using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using SuperDentist.Core.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Infrastructure.Data
{
    public sealed class SqliteConnectionFactory : ISqliteConnectionFactory
    {
        private readonly string _connectionString;

        public SqliteConnectionFactory(IOptions<DatabaseOptions> options)
        {
            DatabasePath = ResolveDatabasePath(options.Value);
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = DatabasePath,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();
        }

        public string DatabasePath { get; }

        public async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }

        private static string ResolveDatabasePath(DatabaseOptions options)
        {
            string? overridePath = Environment.GetEnvironmentVariable("SUPERDENTIST_DB_PATH");
            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                return overridePath!;
            }

            if (!string.IsNullOrWhiteSpace(options.Path))
            {
                return options.Path!;
            }

            string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(root, "SuperDentist", "superdentist.db");
        }
    }
}
