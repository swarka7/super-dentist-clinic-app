using Microsoft.Data.Sqlite;
using SuperDentist.Core;
using SuperDentist.Infrastructure.Data;
using SuperDentist.Infrastructure.Repositories;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SuperDentist.Tests
{
    public sealed class SqliteMigrationTests
    {
        [Fact]
        public async Task MigrateAsync_WhenDatabaseIsEmpty_UpgradesToLatestSchema()
        {
            using var database = await SqliteTestDatabase.CreateAsync();

            Assert.Equal(SqliteSchema.LatestVersion, await GetCurrentSchemaVersionAsync(database));
            Assert.True(await ColumnExistsAsync(database, "Doctors", "CreatedAtUtc"));
            Assert.True(await ColumnExistsAsync(database, "Patients", "UpdatedAtUtc"));
            Assert.True(await ForeignKeysEnabledAsync(database));
        }

        [Fact]
        public async Task MigrateAsync_WhenDatabaseIsBaseline_UpgradesWithoutDataLoss()
        {
            using var database = await SqliteTestDatabase.CreateAtMigrationVersionAsync(SqliteSchema.BaselineVersion);
            await database.ExecuteAsync(@"
INSERT INTO Doctors (Id, FirstName, LastName, Phone, Address, Email, Specialization, Salary)
VALUES ('D001', 'Legacy', 'Doctor', '0500000000', 'Legacy St', 'legacy.doctor@example.com', 'General', 9000);

INSERT INTO Treatments (Number, Type, Price, Tools)
VALUES ('TR001', 'Legacy Treatment', 250, 'Legacy Tools');

INSERT INTO Patients (Id, FirstName, LastName, Address, Phone, Email, Age, TreatmentStatus, DoctorId)
VALUES ('P001', 'Legacy', 'Patient', 'Patient St', '0500000001', 'legacy.patient@example.com', 42, 'Yes', 'D001');

INSERT INTO Appointments (PatientId, DoctorId, Date, Time, TreatmentNumber)
VALUES ('P001', 'D001', '2030-05-01', '09:00', 'TR001');

INSERT INTO PatientTreatments (PatientId, TreatmentNumber, IsCompleted, IsPaid, StartDate)
VALUES ('P001', 'TR001', 'No', 'No', '2030-04-01');
");

            MigrationResult result = await database.MigrateToLatestAsync();

            Assert.Equal(SqliteSchema.LatestVersion, result.CurrentVersion);
            Assert.Contains(SqliteSchema.IntegrityVersion, result.AppliedVersions);
            Assert.Equal(1L, await CountAsync(database, "Doctors"));
            Assert.Equal(1L, await CountAsync(database, "Patients"));
            Assert.Equal(1L, await CountAsync(database, "Appointments"));
            Assert.Equal("Legacy", await database.ScalarAsync("SELECT FirstName FROM Patients WHERE Id = 'P001';"));
            Assert.False(string.IsNullOrWhiteSpace(Convert.ToString(await database.ScalarAsync("SELECT CreatedAtUtc FROM Patients WHERE Id = 'P001';"))));
        }

        [Fact]
        public async Task ForeignKeys_WhenReferenceIsInvalid_RejectInsert()
        {
            using var database = await SqliteTestDatabase.CreateAsync();

            await Assert.ThrowsAsync<SqliteException>(() => database.ExecuteAsync(@"
INSERT INTO Appointments (PatientId, DoctorId, Date, Time, TreatmentNumber)
VALUES ('missing-patient', 'missing-doctor', '2030-06-01', '10:00', 'missing-treatment');
"));
        }

        [Fact]
        public async Task Delete_WhenRecordIsReferenced_IsRestricted()
        {
            using var database = await SqliteTestDatabase.CreateAsync();
            await database.SeedAppointmentReferencesAsync(("P100", "D100", "T100"));
            await database.ExecuteAsync(@"
INSERT INTO Appointments (PatientId, DoctorId, Date, Time, TreatmentNumber)
VALUES ('P100', 'D100', '2030-07-01', '11:00', 'T100');
");

            await Assert.ThrowsAsync<SqliteException>(() => database.ExecuteAsync("DELETE FROM Doctors WHERE Id = 'D100';"));
            await Assert.ThrowsAsync<SqliteException>(() => database.ExecuteAsync("DELETE FROM Patients WHERE Id = 'P100';"));
            await Assert.ThrowsAsync<SqliteException>(() => database.ExecuteAsync("DELETE FROM Treatments WHERE Number = 'T100';"));
        }

        [Fact]
        public async Task MigrateAsync_WhenRunMultipleTimes_IsIdempotent()
        {
            using var database = await SqliteTestDatabase.CreateAsync();
            long beforeCount = await CountAsync(database, SqliteSchema.MigrationTableName);

            MigrationResult result = await database.MigrateToLatestAsync();

            Assert.Equal(SqliteSchema.LatestVersion, result.CurrentVersion);
            Assert.Empty(result.AppliedVersions);
            Assert.Equal(beforeCount, await CountAsync(database, SqliteSchema.MigrationTableName));
        }

        [Fact]
        public async Task Repositories_WhenRowsAreInserted_PopulateAuditTimestamps()
        {
            using var database = await SqliteTestDatabase.CreateAsync();
            await database.SeedAppointmentReferencesAsync(("P200", "D200", "T200"));

            var appointmentRepository = new SqliteAppointmentRepository(database.ConnectionFactory);
            await appointmentRepository.AddAsync(new Appointment
            {
                PatientId = "P200",
                DoctorId = "D200",
                Date = "2030-08-01",
                Time = "12:00",
                TreatmentNumber = "T200"
            });

            var patientTreatmentRepository = new SqlitePatientTreatmentRepository(database.ConnectionFactory);
            await patientTreatmentRepository.AddAsync(new PatientTreatment
            {
                PatientId = "P200",
                TreatmentNumber = "T200",
                IsCompleted = "No",
                IsPaid = "No",
                StartDate = "2030-08-01"
            });

            await AssertTimestampsPopulatedAsync(database, "Doctors");
            await AssertTimestampsPopulatedAsync(database, "Patients");
            await AssertTimestampsPopulatedAsync(database, "Treatments");
            await AssertTimestampsPopulatedAsync(database, "Appointments");
            await AssertTimestampsPopulatedAsync(database, "PatientTreatments");
        }

        private static async Task<int> GetCurrentSchemaVersionAsync(SqliteTestDatabase database)
        {
            object? value = await database.ScalarAsync($"SELECT MAX(Version) FROM {SqliteSchema.MigrationTableName};");
            return Convert.ToInt32(value);
        }

        private static async Task<long> CountAsync(SqliteTestDatabase database, string tableName)
        {
            object? value = await database.ScalarAsync($"SELECT COUNT(*) FROM {tableName};");
            return Convert.ToInt64(value);
        }

        private static async Task<bool> ForeignKeysEnabledAsync(SqliteTestDatabase database)
        {
            object? value = await database.ScalarAsync("PRAGMA foreign_keys;");
            return Convert.ToInt64(value) == 1;
        }

        private static async Task<bool> ColumnExistsAsync(SqliteTestDatabase database, string tableName, string columnName)
        {
            await using var connection = await database.OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({tableName});";

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static async Task AssertTimestampsPopulatedAsync(SqliteTestDatabase database, string tableName)
        {
            object? missing = await database.ScalarAsync($@"
SELECT COUNT(*) FROM {tableName}
WHERE CreatedAtUtc IS NULL
   OR CreatedAtUtc = ''
   OR UpdatedAtUtc IS NULL
   OR UpdatedAtUtc = '';");
            Assert.Equal(0L, Convert.ToInt64(missing));
        }
    }
}