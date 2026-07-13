using Microsoft.Data.Sqlite;
using SuperDentist.Infrastructure.Data;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Infrastructure.Repositories
{
    public abstract class SqliteRepositoryBase
    {
        private readonly SqliteConnectionFactory _connectionFactory;

        protected SqliteRepositoryBase(SqliteConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private protected Task<SqliteConnectionScope> OpenScopeAsync(CancellationToken cancellationToken)
        {
            return _connectionFactory.OpenScopeAsync(cancellationToken);
        }

        protected static string UtcNowText()
        {
            return DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        }

        protected static DateTime ReadUtcDateTime(SqliteDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return default;
            }

            string value = reader.GetString(ordinal);
            return DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime parsed)
                ? parsed
                : default;
        }

        protected static object DbNullableText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
        }
    }
}