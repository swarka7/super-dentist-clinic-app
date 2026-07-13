using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using SuperDentist.Core.Options;
using SuperDentist.Infrastructure.Data;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Tests
{
    internal sealed class SqliteTestDatabase : IDisposable
    {
        private const string SchemaSql = @"
CREATE TABLE IF NOT EXISTS Doctors (
    Id TEXT PRIMARY KEY,
    FirstName TEXT NOT NULL,
    LastName TEXT NOT NULL,
    Phone TEXT,
    Address TEXT,
    Email TEXT,
    Specialization TEXT,
    Salary INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS Patients (
    Id TEXT PRIMARY KEY,
    FirstName TEXT NOT NULL,
    LastName TEXT NOT NULL,
    Address TEXT,
    Phone TEXT,
    Email TEXT,
    Age INTEGER NOT NULL DEFAULT 0,
    TreatmentStatus TEXT,
    DoctorId TEXT
);

CREATE TABLE IF NOT EXISTS Treatments (
    Number TEXT PRIMARY KEY,
    Type TEXT,
    Price INTEGER NOT NULL DEFAULT 0,
    Tools TEXT
);

CREATE TABLE IF NOT EXISTS Appointments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PatientId TEXT NOT NULL,
    DoctorId TEXT NOT NULL,
    Date TEXT NOT NULL,
    Time TEXT NOT NULL,
    TreatmentNumber TEXT
);

CREATE TABLE IF NOT EXISTS PatientTreatments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PatientId TEXT NOT NULL,
    TreatmentNumber TEXT NOT NULL,
    IsCompleted TEXT,
    IsPaid TEXT,
    StartDate TEXT
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_Appointments_PatientId ON Appointments (PatientId);
CREATE UNIQUE INDEX IF NOT EXISTS IX_Appointments_DoctorSlot ON Appointments (DoctorId, Date, Time);
CREATE INDEX IF NOT EXISTS IX_PatientTreatments_PatientId ON PatientTreatments (PatientId);
";

        private SqliteTestDatabase(string databasePath)
        {
            DatabasePath = databasePath;
            ConnectionFactory = new SqliteConnectionFactory(Options.Create(new DatabaseOptions { Path = databasePath }));
        }

        public string DatabasePath { get; }
        public SqliteConnectionFactory ConnectionFactory { get; }

        public static async Task<SqliteTestDatabase> CreateAsync(CancellationToken cancellationToken = default)
        {
            string databasePath = Path.Combine(Path.GetTempPath(), $"superdentist-test-{Guid.NewGuid():N}.db");
            var database = new SqliteTestDatabase(databasePath);
            await database.CreateSchemaAsync(cancellationToken).ConfigureAwait(false);
            return database;
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            DeleteIfExists(DatabasePath);
            DeleteIfExists(DatabasePath + "-shm");
            DeleteIfExists(DatabasePath + "-wal");
            DeleteIfExists(DatabasePath + "-journal");
        }

        private async Task CreateSchemaAsync(CancellationToken cancellationToken)
        {
            string? directory = Path.GetDirectoryName(DatabasePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = SchemaSql;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
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
