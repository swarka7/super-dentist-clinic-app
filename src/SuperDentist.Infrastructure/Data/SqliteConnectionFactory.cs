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
        private readonly AsyncLocal<SqliteTransactionContext?> _currentTransaction = new();

        public SqliteConnectionFactory(IOptions<DatabaseOptions> options)
        {
            DatabasePath = ResolveDatabasePath(options.Value);

            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = DatabasePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            }.ToString();
        }

        public string DatabasePath { get; }

        public async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = new SqliteConnection(_connectionString);
            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                await EnableForeignKeysAsync(connection, cancellationToken).ConfigureAwait(false);
                return connection;
            }
            catch
            {
                await connection.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        internal async Task<SqliteConnectionScope> OpenScopeAsync(
            CancellationToken cancellationToken = default)
        {
            SqliteTransactionContext? context = _currentTransaction.Value;
            if (context is { IsActive: true } && context.Transaction.Connection != null)
            {
                await context.CommandGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                if (context.IsActive && context.Transaction.Connection != null)
                {
                    return new SqliteConnectionScope(
                        context.Transaction.Connection,
                        context.Transaction,
                        ownsConnection: false,
                        context.CommandGate);
                }

                context.CommandGate.Release();
            }

            SqliteConnection connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            return new SqliteConnectionScope(connection, transaction: null, ownsConnection: true);
        }

        internal async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(operation);

            SqliteTransactionContext? ambientContext = _currentTransaction.Value;
            if (ambientContext is { IsActive: true })
            {
                return await operation(cancellationToken).ConfigureAwait(false);
            }

            _currentTransaction.Value = null;

            await using SqliteConnection connection =
                await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            using SqliteTransaction transaction = connection.BeginTransaction();
            var context = new SqliteTransactionContext(transaction);
            _currentTransaction.Value = context;

            try
            {
                T result = await operation(cancellationToken).ConfigureAwait(false);
                await context.CommandGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                try
                {
                    transaction.Commit();
                }
                finally
                {
                    context.CommandGate.Release();
                }

                return result;
            }
            catch
            {
                await context.CommandGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                try
                {
                    TryRollback(transaction);
                }
                finally
                {
                    context.CommandGate.Release();
                }

                throw;
            }
            finally
            {
                context.Deactivate();
                _currentTransaction.Value = null;
            }
        }

        private static async Task EnableForeignKeysAsync(
            SqliteConnection connection,
            CancellationToken cancellationToken)
        {
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "PRAGMA foreign_keys = ON;";
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private static void TryRollback(SqliteTransaction transaction)
        {
            try
            {
                transaction.Rollback();
            }
            catch (InvalidOperationException)
            {
            }
            catch (SqliteException)
            {
            }
        }

        private static string ResolveDatabasePath(DatabaseOptions options)
        {
            string? overridePath = Environment.GetEnvironmentVariable("SUPERDENTIST_DB_PATH");
            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                return Path.GetFullPath(overridePath);
            }

            if (!string.IsNullOrWhiteSpace(options.Path))
            {
                return Path.GetFullPath(Environment.ExpandEnvironmentVariables(options.Path));
            }

            string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(root, "SuperDentist", "superdentist.db");
        }
    }
}