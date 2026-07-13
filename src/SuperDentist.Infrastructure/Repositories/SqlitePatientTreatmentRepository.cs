using Microsoft.Data.Sqlite;
using SuperDentist.Core;
using SuperDentist.Core.Repositories;
using SuperDentist.Infrastructure.Data;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Infrastructure.Repositories
{
    public sealed class SqlitePatientTreatmentRepository : SqliteRepositoryBase, IPatientTreatmentRepository
    {
        public SqlitePatientTreatmentRepository(ISqliteConnectionFactory connectionFactory) : base(connectionFactory)
        {
        }

        public async Task<IReadOnlyList<PatientTreatment>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var items = new List<PatientTreatment>();
            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = @"SELECT PatientId, TreatmentNumber, IsCompleted, IsPaid, StartDate, CreatedAtUtc, UpdatedAtUtc
                                    FROM PatientTreatments ORDER BY StartDate DESC;";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                items.Add(MapPatientTreatment(reader));
            }

            return items;
        }

        public async Task<PatientTreatment?> GetByPatientIdAsync(string patientId, CancellationToken cancellationToken = default)
        {
            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = @"SELECT PatientId, TreatmentNumber, IsCompleted, IsPaid, StartDate, CreatedAtUtc, UpdatedAtUtc
                                    FROM PatientTreatments WHERE PatientId = @PatientId;";
            command.Parameters.AddWithValue("@PatientId", patientId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? MapPatientTreatment(reader) : null;
        }

        public async Task<bool> ExistsAsync(string patientId, CancellationToken cancellationToken = default)
        {
            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM PatientTreatments WHERE PatientId = @PatientId LIMIT 1;";
            command.Parameters.AddWithValue("@PatientId", patientId);
            object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result != null;
        }

        public async Task AddAsync(PatientTreatment patientTreatment, CancellationToken cancellationToken = default)
        {
            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = @"INSERT INTO PatientTreatments (PatientId, TreatmentNumber, IsCompleted, IsPaid, StartDate, CreatedAtUtc, UpdatedAtUtc)
                                    VALUES (@PatientId, @TreatmentNumber, @IsCompleted, @IsPaid, @StartDate, @CreatedAtUtc, @UpdatedAtUtc);";
            AddParameters(command, patientTreatment, includeCreatedAt: true);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateAsync(PatientTreatment patientTreatment, CancellationToken cancellationToken = default)
        {
            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = @"UPDATE PatientTreatments
                                    SET TreatmentNumber = @TreatmentNumber,
                                        IsCompleted = @IsCompleted,
                                        IsPaid = @IsPaid,
                                        StartDate = @StartDate,
                                        UpdatedAtUtc = @UpdatedAtUtc
                                    WHERE PatientId = @PatientId;";
            AddParameters(command, patientTreatment, includeCreatedAt: false);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string patientId, CancellationToken cancellationToken = default)
        {
            await using var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM PatientTreatments WHERE PatientId = @PatientId;";
            command.Parameters.AddWithValue("@PatientId", patientId);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private static PatientTreatment MapPatientTreatment(SqliteDataReader reader)
        {
            return new PatientTreatment
            {
                PatientId = reader.GetString(0),
                TreatmentNumber = reader.GetString(1),
                IsCompleted = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                IsPaid = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                StartDate = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                CreatedAtUtc = ReadUtcDateTime(reader, 5),
                UpdatedAtUtc = ReadUtcDateTime(reader, 6)
            };
        }

        private static void AddParameters(SqliteCommand command, PatientTreatment patientTreatment, bool includeCreatedAt)
        {
            string now = UtcNowText();
            command.Parameters.AddWithValue("@PatientId", patientTreatment.PatientId);
            command.Parameters.AddWithValue("@TreatmentNumber", patientTreatment.TreatmentNumber ?? string.Empty);
            command.Parameters.AddWithValue("@IsCompleted", patientTreatment.IsCompleted ?? string.Empty);
            command.Parameters.AddWithValue("@IsPaid", patientTreatment.IsPaid ?? string.Empty);
            command.Parameters.AddWithValue("@StartDate", patientTreatment.StartDate ?? string.Empty);
            if (includeCreatedAt)
            {
                command.Parameters.AddWithValue("@CreatedAtUtc", now);
            }

            command.Parameters.AddWithValue("@UpdatedAtUtc", now);
        }
    }
}