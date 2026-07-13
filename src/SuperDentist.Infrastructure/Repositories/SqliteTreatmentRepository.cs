using Microsoft.Data.Sqlite;
using SuperDentist.Core;
using SuperDentist.Core.Repositories;
using SuperDentist.Infrastructure.Data;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Infrastructure.Repositories
{
    public sealed class SqliteTreatmentRepository : SqliteRepositoryBase, ITreatmentRepository
    {
        public SqliteTreatmentRepository(SqliteConnectionFactory connectionFactory) : base(connectionFactory)
        {
        }

        public async Task<IReadOnlyList<Treatment>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var treatments = new List<Treatment>();
            await using var scope = await OpenScopeAsync(cancellationToken).ConfigureAwait(false);
            await using var command = scope.CreateCommand();
            command.CommandText = "SELECT Number, Type, Price, Tools, CreatedAtUtc, UpdatedAtUtc FROM Treatments ORDER BY Number;";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                treatments.Add(MapTreatment(reader));
            }

            return treatments;
        }

        public async Task<Treatment?> GetByNumberAsync(string number, CancellationToken cancellationToken = default)
        {
            await using var scope = await OpenScopeAsync(cancellationToken).ConfigureAwait(false);
            await using var command = scope.CreateCommand();
            command.CommandText = "SELECT Number, Type, Price, Tools, CreatedAtUtc, UpdatedAtUtc FROM Treatments WHERE Number = @Number;";
            command.Parameters.AddWithValue("@Number", number);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? MapTreatment(reader) : null;
        }

        public async Task<bool> ExistsAsync(string number, CancellationToken cancellationToken = default)
        {
            await using var scope = await OpenScopeAsync(cancellationToken).ConfigureAwait(false);
            await using var command = scope.CreateCommand();
            command.CommandText = "SELECT 1 FROM Treatments WHERE Number = @Number LIMIT 1;";
            command.Parameters.AddWithValue("@Number", number);
            object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result != null;
        }

        public async Task AddAsync(Treatment treatment, CancellationToken cancellationToken = default)
        {
            await using var scope = await OpenScopeAsync(cancellationToken).ConfigureAwait(false);
            await using var command = scope.CreateCommand();
            command.CommandText = @"INSERT INTO Treatments (Number, Type, Price, Tools, CreatedAtUtc, UpdatedAtUtc)
                                    VALUES (@Number, @Type, @Price, @Tools, @CreatedAtUtc, @UpdatedAtUtc);";
            AddTreatmentParameters(command, treatment, includeCreatedAt: true);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task UpdateAsync(Treatment treatment, CancellationToken cancellationToken = default)
        {
            await using var scope = await OpenScopeAsync(cancellationToken).ConfigureAwait(false);
            await using var command = scope.CreateCommand();
            command.CommandText = @"UPDATE Treatments
                                    SET Type = @Type,
                                        Price = @Price,
                                        Tools = @Tools,
                                        UpdatedAtUtc = @UpdatedAtUtc
                                    WHERE Number = @Number;";
            AddTreatmentParameters(command, treatment, includeCreatedAt: false);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string number, CancellationToken cancellationToken = default)
        {
            await using var scope = await OpenScopeAsync(cancellationToken).ConfigureAwait(false);
            await using var command = scope.CreateCommand();
            command.CommandText = "DELETE FROM Treatments WHERE Number = @Number;";
            command.Parameters.AddWithValue("@Number", number);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private static Treatment MapTreatment(SqliteDataReader reader)
        {
            return new Treatment
            {
                Number = reader.GetString(0),
                Type = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Price = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                Tools = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                CreatedAtUtc = ReadUtcDateTime(reader, 4),
                UpdatedAtUtc = ReadUtcDateTime(reader, 5)
            };
        }

        private static void AddTreatmentParameters(SqliteCommand command, Treatment treatment, bool includeCreatedAt)
        {
            string now = UtcNowText();
            command.Parameters.AddWithValue("@Number", treatment.Number);
            command.Parameters.AddWithValue("@Type", treatment.Type ?? string.Empty);
            command.Parameters.AddWithValue("@Price", treatment.Price);
            command.Parameters.AddWithValue("@Tools", treatment.Tools ?? string.Empty);
            if (includeCreatedAt)
            {
                command.Parameters.AddWithValue("@CreatedAtUtc", now);
            }

            command.Parameters.AddWithValue("@UpdatedAtUtc", now);
        }
    }
}