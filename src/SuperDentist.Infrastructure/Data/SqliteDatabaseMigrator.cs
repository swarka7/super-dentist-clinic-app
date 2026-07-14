using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Infrastructure.Data
{
    public sealed class SqliteDatabaseMigrator
    {
        private static readonly IReadOnlyList<SqliteMigration> Migrations = new[]
        {
            new SqliteMigration(SqliteSchema.BaselineVersion, "Baseline current schema", SqliteSchema.BaselineSchemaSql),
            new SqliteMigration(SqliteSchema.IntegrityVersion, "Foreign keys and audit timestamps", SqliteSchema.IntegrityUpgradeSql),
            new SqliteMigration(SqliteSchema.AuditVersion, "Application audit trail", SqliteSchema.AuditTrailSql)
        };

        private static readonly string[] BaselineTables =
        {
            "Doctors",
            "Patients",
            "Treatments",
            "Appointments",
            "PatientTreatments"
        };

        private readonly ISqliteConnectionFactory _connectionFactory;
        private readonly ILogger<SqliteDatabaseMigrator> _logger;

        public SqliteDatabaseMigrator(ISqliteConnectionFactory connectionFactory, ILogger<SqliteDatabaseMigrator> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public Task<MigrationResult> MigrateAsync(CancellationToken cancellationToken = default)
        {
            return MigrateAsync(SqliteSchema.LatestVersion, cancellationToken);
        }

        internal async Task<MigrationResult> MigrateAsync(int targetVersion, CancellationToken cancellationToken = default)
        {
            if (targetVersion < SqliteSchema.BaselineVersion || targetVersion > SqliteSchema.LatestVersion)
            {
                throw new ArgumentOutOfRangeException(nameof(targetVersion), targetVersion, "Unsupported SQLite schema version.");
            }

            string? directory = Path.GetDirectoryName(_connectionFactory.DatabasePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await EnsureMigrationHistoryAsync(connection, cancellationToken).ConfigureAwait(false);

            var appliedVersions = await GetAppliedVersionsAsync(connection, cancellationToken).ConfigureAwait(false);
            var newlyAppliedVersions = new List<int>();

            foreach (SqliteMigration migration in Migrations.Where(m => m.Version <= targetVersion).OrderBy(m => m.Version))
            {
                if (appliedVersions.Contains(migration.Version))
                {
                    continue;
                }

                bool appliedNow = await ApplyMigrationAsync(
                    connection,
                    migration,
                    cancellationToken).ConfigureAwait(false);
                appliedVersions.Add(migration.Version);
                if (appliedNow)
                {
                    newlyAppliedVersions.Add(migration.Version);
                }
            }

            int currentVersion = appliedVersions.Count == 0 ? 0 : appliedVersions.Max();
            return new MigrationResult(currentVersion, newlyAppliedVersions);
        }

        private async Task<bool> ApplyMigrationAsync(
            SqliteConnection connection,
            SqliteMigration migration,
            CancellationToken cancellationToken)
        {
            using var transaction = connection.BeginTransaction(deferred: false);
            try
            {
                if (await IsMigrationAppliedAsync(
                    connection,
                    transaction,
                    migration.Version,
                    cancellationToken).ConfigureAwait(false))
                {
                    transaction.Commit();
                    return false;
                }

                await ExecuteNonQueryAsync(connection, transaction, migration.Sql, cancellationToken).ConfigureAwait(false);

                if (migration.Version >= SqliteSchema.IntegrityVersion)
                {
                    await VerifyForeignKeysAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
                }

                await RecordMigrationAsync(connection, transaction, migration.Version, migration.Name, cancellationToken).ConfigureAwait(false);
                transaction.Commit();
                _logger.LogInformation("Applied SQLite migration {Version}: {Name}", migration.Version, migration.Name);
                return true;
            }
            catch (Exception ex)
            {
                TryRollback(transaction);
                _logger.LogError(ex, "SQLite migration {Version} ({Name}) failed", migration.Version, migration.Name);
                throw new InvalidOperationException($"SQLite migration {migration.Version} ({migration.Name}) failed.", ex);
            }
        }

        private async Task EnsureMigrationHistoryAsync(
            SqliteConnection connection,
            CancellationToken cancellationToken)
        {
            using var transaction = connection.BeginTransaction(deferred: false);
            bool adoptedBaseline = false;

            try
            {
                bool migrationTableExists = await TableExistsAsync(
                    connection,
                    transaction,
                    SqliteSchema.MigrationTableName,
                    cancellationToken).ConfigureAwait(false);

                if (!migrationTableExists)
                {
                    await CreateMigrationTableAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
                }

                bool hasAppliedMigrations = await HasAppliedMigrationsAsync(
                    connection,
                    transaction,
                    cancellationToken).ConfigureAwait(false);
                if (!hasAppliedMigrations)
                {
                    bool hasBaselineTables = await AnyBaselineTableExistsAsync(
                        connection,
                        transaction,
                        cancellationToken).ConfigureAwait(false);
                    if (hasBaselineTables)
                    {
                        await EnsureBaselineTablesExistAsync(
                            connection,
                            transaction,
                            cancellationToken).ConfigureAwait(false);
                        await RecordMigrationAsync(
                            connection,
                            transaction,
                            SqliteSchema.BaselineVersion,
                            "Existing baseline schema",
                            cancellationToken).ConfigureAwait(false);
                        adoptedBaseline = true;
                    }
                }

                transaction.Commit();
            }
            catch
            {
                TryRollback(transaction);
                throw;
            }

            if (adoptedBaseline)
            {
                _logger.LogInformation(
                    "Adopted existing SQLite database as schema baseline version {Version}",
                    SqliteSchema.BaselineVersion);
            }
        }

        private static async Task CreateMigrationTableAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"
CREATE TABLE {SqliteSchema.MigrationTableName} (
    Version INTEGER PRIMARY KEY,
    Name TEXT NOT NULL,
    AppliedAtUtc TEXT NOT NULL
);";
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task<bool> HasAppliedMigrationsAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"SELECT 1 FROM {SqliteSchema.MigrationTableName} LIMIT 1;";
            return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) != null;
        }

        private static async Task<bool> IsMigrationAppliedAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            int version,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"SELECT 1 FROM {SqliteSchema.MigrationTableName} WHERE Version = @Version LIMIT 1;";
            command.Parameters.AddWithValue("@Version", version);
            return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) != null;
        }

        private static async Task<HashSet<int>> GetAppliedVersionsAsync(SqliteConnection connection, CancellationToken cancellationToken)
        {
            var versions = new HashSet<int>();
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT Version FROM {SqliteSchema.MigrationTableName} ORDER BY Version;";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                versions.Add(reader.GetInt32(0));
            }

            return versions;
        }

        private static async Task RecordMigrationAsync(SqliteConnection connection, SqliteTransaction transaction, int version, string name, CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"
INSERT INTO {SqliteSchema.MigrationTableName} (Version, Name, AppliedAtUtc)
VALUES (@Version, @Name, @AppliedAtUtc);";
            command.Parameters.AddWithValue("@Version", version);
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@AppliedAtUtc", DateTime.UtcNow.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task ExecuteNonQueryAsync(SqliteConnection connection, SqliteTransaction transaction, string sql, CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task<bool> TableExistsAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string tableName,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = @Name LIMIT 1;";
            command.Parameters.AddWithValue("@Name", tableName);
            object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result != null;
        }

        private static async Task<bool> AnyBaselineTableExistsAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            CancellationToken cancellationToken)
        {
            foreach (string tableName in BaselineTables)
            {
                if (await TableExistsAsync(
                    connection,
                    transaction,
                    tableName,
                    cancellationToken).ConfigureAwait(false))
                {
                    return true;
                }
            }

            return false;
        }

        private static async Task EnsureBaselineTablesExistAsync(
            SqliteConnection connection,
            SqliteTransaction transaction,
            CancellationToken cancellationToken)
        {
            var missingTables = new List<string>();
            foreach (string tableName in BaselineTables)
            {
                if (!await TableExistsAsync(
                    connection,
                    transaction,
                    tableName,
                    cancellationToken).ConfigureAwait(false))
                {
                    missingTables.Add(tableName);
                }
            }

            if (missingTables.Count > 0)
            {
                throw new InvalidOperationException($"Existing SQLite database is missing baseline table(s): {string.Join(", ", missingTables)}.");
            }
        }

        private static async Task VerifyForeignKeysAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "PRAGMA foreign_key_check;";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                string table = reader.GetString(0);
                long rowId = reader.GetInt64(1);
                throw new InvalidOperationException($"SQLite foreign key check failed for table {table}, rowid {rowId}.");
            }
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

        private sealed record SqliteMigration(int Version, string Name, string Sql);
    }

    public sealed class MigrationResult
    {
        public MigrationResult(int currentVersion, IReadOnlyList<int> appliedVersions)
        {
            CurrentVersion = currentVersion;
            AppliedVersions = appliedVersions;
        }

        public int CurrentVersion { get; }
        public IReadOnlyList<int> AppliedVersions { get; }
    }
}