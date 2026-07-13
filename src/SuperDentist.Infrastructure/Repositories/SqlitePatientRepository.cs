using Microsoft.Data.Sqlite;
using SuperDentist.Core;
using SuperDentist.Core.Repositories;
using SuperDentist.Infrastructure.Data;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Infrastructure.Repositories
{
    public sealed class SqlitePatientRepository : SqliteRepositoryBase, IPatientRepository
    {
        public SqlitePatientRepository(SqliteConnectionFactory connectionFactory) : base(connectionFactory)
        {
        }

        public async Task<IReadOnlyList<Patient>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var patients = new List<Patient>();
            await using var scope = await OpenScopeAsync(cancellationToken).ConfigureAwait(false);
            await using var command = scope.CreateCommand();
            command.CommandText = @"SELECT Id, FirstName, LastName, Address, Phone, Email, Age, TreatmentStatus, DoctorId, CreatedAtUtc, UpdatedAtUtc
                                    FROM Patients ORDER BY LastName, FirstName;";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                patients.Add(MapPatient(reader));
            }

            return patients;
        }

        public async Task<Patient?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            await using var scope = await OpenScopeAsync(cancellationToken).ConfigureAwait(false);
            await using var command = scope.CreateCommand();
            command.CommandText = @"SELECT Id, FirstName, LastName, Address, Phone, Email, Age, TreatmentStatus, DoctorId, CreatedAtUtc, UpdatedAtUtc
                                    FROM Patients WHERE Id = @Id;";
            command.Parameters.AddWithValue("@Id", id);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? MapPatient(reader) : null;
        }

        public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
        {
            await using var scope = await OpenScopeAsync(cancellationToken).ConfigureAwait(false);
            await using var command = scope.CreateCommand();
            command.CommandText = "SELECT 1 FROM Patients WHERE Id = @Id LIMIT 1;";
            command.Parameters.AddWithValue("@Id", id);
            object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result != null;
        }

        public async Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
        {
            await using var scope = await OpenScopeAsync(cancellationToken).ConfigureAwait(false);
            await using var command = scope.CreateCommand();
            command.CommandText = @"INSERT INTO Patients (Id, FirstName, LastName, Address, Phone, Email, Age, TreatmentStatus, DoctorId, CreatedAtUtc, UpdatedAtUtc)
                                    VALUES (@Id, @FirstName, @LastName, @Address, @Phone, @Email, @Age, @TreatmentStatus, @DoctorId, @CreatedAtUtc, @UpdatedAtUtc);";
            AddPatientParameters(command, patient, includeCreatedAt: true);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
        {
            await using var scope = await OpenScopeAsync(cancellationToken).ConfigureAwait(false);
            await using var command = scope.CreateCommand();
            command.CommandText = @"UPDATE Patients
                                    SET FirstName = @FirstName,
                                        LastName = @LastName,
                                        Address = @Address,
                                        Phone = @Phone,
                                        Email = @Email,
                                        Age = @Age,
                                        TreatmentStatus = @TreatmentStatus,
                                        DoctorId = @DoctorId,
                                        UpdatedAtUtc = @UpdatedAtUtc
                                    WHERE Id = @Id;";
            AddPatientParameters(command, patient, includeCreatedAt: false);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            await using var scope = await OpenScopeAsync(cancellationToken).ConfigureAwait(false);
            await using var command = scope.CreateCommand();
            command.CommandText = "DELETE FROM Patients WHERE Id = @Id;";
            command.Parameters.AddWithValue("@Id", id);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private static Patient MapPatient(SqliteDataReader reader)
        {
            return new Patient
            {
                Id = reader.GetString(0),
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                Address = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Phone = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Email = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                Age = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                TreatmentStatus = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                DoctorId = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                CreatedAtUtc = ReadUtcDateTime(reader, 9),
                UpdatedAtUtc = ReadUtcDateTime(reader, 10)
            };
        }

        private static void AddPatientParameters(SqliteCommand command, Patient patient, bool includeCreatedAt)
        {
            string now = UtcNowText();
            command.Parameters.AddWithValue("@Id", patient.Id);
            command.Parameters.AddWithValue("@FirstName", patient.FirstName);
            command.Parameters.AddWithValue("@LastName", patient.LastName);
            command.Parameters.AddWithValue("@Address", patient.Address ?? string.Empty);
            command.Parameters.AddWithValue("@Phone", patient.Phone ?? string.Empty);
            command.Parameters.AddWithValue("@Email", patient.Email ?? string.Empty);
            command.Parameters.AddWithValue("@Age", patient.Age);
            command.Parameters.AddWithValue("@TreatmentStatus", patient.TreatmentStatus ?? string.Empty);
            command.Parameters.AddWithValue("@DoctorId", DbNullableText(patient.DoctorId));
            if (includeCreatedAt)
            {
                command.Parameters.AddWithValue("@CreatedAtUtc", now);
            }

            command.Parameters.AddWithValue("@UpdatedAtUtc", now);
        }
    }
}