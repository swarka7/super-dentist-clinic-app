using Microsoft.Data.Sqlite;
using SuperDentist.Core;
using SuperDentist.Core.Repositories;
using SuperDentist.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Infrastructure.Repositories
{
    public sealed class SqliteAuditRepository : SqliteRepositoryBase, IAuditRepository
    {
        public SqliteAuditRepository(SqliteConnectionFactory connectionFactory) : base(connectionFactory)
        {
        }

        public async Task AddAsync(AuditEntry entry, CancellationToken cancellationToken = default)
        {
            await using SqliteConnectionScope scope =
                await OpenScopeAsync(cancellationToken).ConfigureAwait(false);
            await using SqliteCommand command = scope.CreateCommand();
            command.CommandText = @"
INSERT INTO AuditEntries
    (EntityType, EntityId, Operation, Actor, TimestampUtc, OldValues, NewValues, CorrelationId)
VALUES
    (@EntityType, @EntityId, @Operation, @Actor, @TimestampUtc, @OldValues, @NewValues, @CorrelationId);";
            command.Parameters.AddWithValue("@EntityType", entry.EntityType);
            command.Parameters.AddWithValue("@EntityId", entry.EntityId);
            command.Parameters.AddWithValue("@Operation", entry.Operation.ToString());
            command.Parameters.AddWithValue("@Actor", entry.Actor);
            command.Parameters.AddWithValue("@TimestampUtc", entry.TimestampUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@OldValues", (object?)entry.OldValues ?? DBNull.Value);
            command.Parameters.AddWithValue("@NewValues", (object?)entry.NewValues ?? DBNull.Value);
            command.Parameters.AddWithValue("@CorrelationId", entry.CorrelationId);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<AuditEntry>> SearchAsync(
            AuditQuery query,
            CancellationToken cancellationToken = default)
        {
            await using SqliteConnectionScope scope =
                await OpenScopeAsync(cancellationToken).ConfigureAwait(false);
            await using SqliteCommand command = scope.CreateCommand();

            var sql = new StringBuilder(@"
SELECT Id, EntityType, EntityId, Operation, Actor, TimestampUtc, OldValues, NewValues, CorrelationId
FROM AuditEntries
WHERE 1 = 1");

            AddTextFilter(sql, command, "EntityType", query.EntityType);
            AddEntityIdFilter(sql, command, query.EntityId);
            AddTextFilter(sql, command, "Actor", query.Actor);

            if (query.Operation.HasValue)
            {
                sql.Append(" AND Operation = @Operation");
                command.Parameters.AddWithValue("@Operation", query.Operation.Value.ToString());
            }

            if (query.FromUtc.HasValue)
            {
                sql.Append(" AND TimestampUtc >= @FromUtc");
                command.Parameters.AddWithValue("@FromUtc", ToUtcText(query.FromUtc.Value));
            }

            if (query.ToUtc.HasValue)
            {
                sql.Append(" AND TimestampUtc <= @ToUtc");
                command.Parameters.AddWithValue("@ToUtc", ToUtcText(query.ToUtc.Value));
            }

            sql.Append(" ORDER BY TimestampUtc DESC, Id DESC LIMIT @Limit;");
            command.Parameters.AddWithValue("@Limit", Math.Clamp(query.Limit, 1, 500));
            command.CommandText = sql.ToString();

            var entries = new List<AuditEntry>();
            await using SqliteDataReader reader =
                await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                entries.Add(MapEntry(reader));
            }

            return entries;
        }

        private static void AddTextFilter(
            StringBuilder sql,
            SqliteCommand command,
            string column,
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            string parameterName = "@" + column;
            sql.Append(" AND ").Append(column).Append(" = ").Append(parameterName);
            command.Parameters.AddWithValue(parameterName, value.Trim());
        }

        private static void AddEntityIdFilter(
            StringBuilder sql,
            SqliteCommand command,
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            sql.Append(" AND EntityId LIKE @EntityId ESCAPE '\\'");
            command.Parameters.AddWithValue("@EntityId", "%" + EscapeLike(value.Trim()) + "%");
        }

        private static string EscapeLike(string value)
        {
            return value.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal);
        }

        private static string ToUtcText(DateTime value)
        {
            DateTime utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };

            return utc.ToString("O", CultureInfo.InvariantCulture);
        }

        private static AuditEntry MapEntry(SqliteDataReader reader)
        {
            string operationText = reader.GetString(3);
            if (!Enum.TryParse(operationText, ignoreCase: false, out AuditOperation operation))
            {
                throw new InvalidOperationException($"Unknown audit operation '{operationText}'.");
            }

            return new AuditEntry
            {
                Id = reader.GetInt64(0),
                EntityType = reader.GetString(1),
                EntityId = reader.GetString(2),
                Operation = operation,
                Actor = reader.GetString(4),
                TimestampUtc = ReadUtcDateTime(reader, 5),
                OldValues = reader.IsDBNull(6) ? null : reader.GetString(6),
                NewValues = reader.IsDBNull(7) ? null : reader.GetString(7),
                CorrelationId = reader.GetString(8)
            };
        }
    }
}
