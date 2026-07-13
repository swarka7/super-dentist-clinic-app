using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SuperDentist.Core.Options;
using SuperDentist.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Tests
{
    internal sealed class SqliteTestDatabase : IDisposable
    {
        private SqliteTestDatabase(string databasePath)
        {
            DatabasePath = databasePath;
            ConnectionFactory = new SqliteConnectionFactory(Options.Create(new DatabaseOptions { Path = databasePath }));
            Migrator = new SqliteDatabaseMigrator(ConnectionFactory, NullLogger<SqliteDatabaseMigrator>.Instance);
        }

        public string DatabasePath { get; }
        public SqliteConnectionFactory ConnectionFactory { get; }
        public SqliteDatabaseMigrator Migrator { get; }

        public static async Task<SqliteTestDatabase> CreateAsync(CancellationToken cancellationToken = default)
        {
            var database = CreateUninitialized();
            await database.MigrateToLatestAsync(cancellationToken).ConfigureAwait(false);
            return database;
        }

        public static async Task<SqliteTestDatabase> CreateAtMigrationVersionAsync(int version, CancellationToken cancellationToken = default)
        {
            var database = CreateUninitialized();
            await database.Migrator.MigrateAsync(version, cancellationToken).ConfigureAwait(false);
            return database;
        }

        public Task<MigrationResult> MigrateToLatestAsync(CancellationToken cancellationToken = default)
        {
            return Migrator.MigrateAsync(cancellationToken);
        }

        public Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            return ConnectionFactory.OpenConnectionAsync(cancellationToken);
        }

        public async Task ExecuteAsync(string sql, CancellationToken cancellationToken = default, params (string Name, object? Value)[] parameters)
        {
            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var (name, value) in parameters)
            {
                command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            }

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<object?> ScalarAsync(string sql, CancellationToken cancellationToken = default, params (string Name, object? Value)[] parameters)
        {
            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var (name, value) in parameters)
            {
                command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            }

            return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task SeedAppointmentReferencesAsync(params (string PatientId, string DoctorId, string TreatmentNumber)[] references)
        {
            foreach (string doctorId in references.Select(r => r.DoctorId).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct())
            {
                await ExecuteAsync(
                    @"INSERT OR IGNORE INTO Doctors (Id, FirstName, LastName, Phone, Address, Email, Specialization, Salary)
                      VALUES (@Id, 'Test', 'Doctor', '0500000000', '1 Test St', 'doctor@example.com', 'General', 8000);",
                    default,
                    ("@Id", doctorId)).ConfigureAwait(false);
            }

            foreach (string treatmentNumber in references.Select(r => r.TreatmentNumber).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct())
            {
                await ExecuteAsync(
                    @"INSERT OR IGNORE INTO Treatments (Number, Type, Price, Tools)
                      VALUES (@Number, 'Test Treatment', 100, 'Test Tools');",
                    default,
                    ("@Number", treatmentNumber)).ConfigureAwait(false);
            }

            foreach ((string patientId, string doctorId, _) in references)
            {
                await ExecuteAsync(
                    @"INSERT OR IGNORE INTO Patients (Id, FirstName, LastName, Address, Phone, Email, Age, TreatmentStatus, DoctorId)
                      VALUES (@Id, 'Test', 'Patient', '2 Test St', '0500000001', 'patient@example.com', 30, 'No', @DoctorId);",
                    default,
                    ("@Id", patientId),
                    ("@DoctorId", doctorId)).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            DeleteIfExists(DatabasePath);
            DeleteIfExists(DatabasePath + "-shm");
            DeleteIfExists(DatabasePath + "-wal");
            DeleteIfExists(DatabasePath + "-journal");
        }

        private static SqliteTestDatabase CreateUninitialized()
        {
            string databasePath = Path.Combine(Path.GetTempPath(), $"superdentist-test-{Guid.NewGuid():N}.db");
            return new SqliteTestDatabase(databasePath);
        }

        private static void DeleteIfExists(string path)
        {
            const int attempts = 5;

            for (int attempt = 1; attempt <= attempts; attempt++)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }

                    return;
                }
                catch (IOException) when (attempt < attempts)
                {
                    SqliteConnection.ClearAllPools();
                    Thread.Sleep(50);
                }
                catch (UnauthorizedAccessException) when (attempt < attempts)
                {
                    SqliteConnection.ClearAllPools();
                    Thread.Sleep(50);
                }
            }
        }
    }
}