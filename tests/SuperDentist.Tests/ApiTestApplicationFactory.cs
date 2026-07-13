using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SuperDentist.Core;
using SuperDentist.Core.Repositories;
using SuperDentist.Core.Services;
using SuperDentist.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Tests
{
    internal sealed class ApiTestApplicationFactory : WebApplicationFactory<Program>
    {
        private static readonly DateOnly TestDate = new(2035, 5, 20);
        private static readonly DateTimeOffset TestUtcNow =
            new(2035, 5, 20, 12, 0, 0, TimeSpan.Zero);

        private readonly Action<IServiceCollection>? _configureServices;
        private readonly bool _useProductionInitializer;

        public ApiTestApplicationFactory(Action<IServiceCollection>? configureServices = null)
            : this(useProductionInitializer: false, configureServices)
        {
        }

        public ApiTestApplicationFactory(bool useProductionInitializer)
            : this(useProductionInitializer, configureServices: null)
        {
        }

        private ApiTestApplicationFactory(
            bool useProductionInitializer,
            Action<IServiceCollection>? configureServices)
        {
            _useProductionInitializer = useProductionInitializer;
            _configureServices = configureServices;
            DatabasePath = Path.Combine(
                Path.GetTempPath(),
                $"superdentist-api-test-{Guid.NewGuid():N}.db");
        }

        public string DatabasePath { get; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:Path"] = DatabasePath,
                    ["Cors:AllowedOrigins:0"] = "http://localhost:5173"
                });
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(new FixedApiTimeProvider(TestUtcNow));

                if (!_useProductionInitializer)
                {
                    services.RemoveAll<IDatabaseInitializer>();
                    services.AddSingleton<IDatabaseInitializer, MigrationOnlyDatabaseInitializer>();
                }

                _configureServices?.Invoke(services);
            });
        }

        public async Task SeedClinicDataAsync()
        {
            IDoctorRepository doctors = Services.GetRequiredService<IDoctorRepository>();
            ITreatmentRepository treatments = Services.GetRequiredService<ITreatmentRepository>();
            IPatientRepository patients = Services.GetRequiredService<IPatientRepository>();
            IAppointmentRepository appointments = Services.GetRequiredService<IAppointmentRepository>();
            IPatientTreatmentRepository patientTreatments =
                Services.GetRequiredService<IPatientTreatmentRepository>();
            IAuditRepository audit = Services.GetRequiredService<IAuditRepository>();

            await doctors.AddAsync(new Doctor
            {
                Id = "API-D1",
                FirstName = "Ada",
                LastName = "Dentist",
                Email = "ada@example.com",
                Phone = "0500000001",
                Address = "1 API Street",
                Specialization = "General",
                Salary = 10000
            });
            await doctors.AddAsync(new Doctor
            {
                Id = "API-D2",
                FirstName = "Grace",
                LastName = "Clinician",
                Email = "grace@example.com",
                Phone = "0500000002",
                Address = "2 API Street",
                Specialization = "Orthodontics",
                Salary = 12000
            });

            await treatments.AddAsync(new Treatment
            {
                Number = "API-T1",
                Type = "Cleaning",
                Price = 100,
                Tools = "Scaler"
            });
            await treatments.AddAsync(new Treatment
            {
                Number = "API-T2",
                Type = "Crown",
                Price = 250,
                Tools = "Crown Kit"
            });

            await patients.AddAsync(new Patient
            {
                Id = "API-P1",
                FirstName = "Patient",
                LastName = "One",
                DoctorId = "API-D1",
                Email = "patient.one@example.com",
                Phone = "0500000011",
                Address = "11 API Street",
                Age = 30,
                TreatmentStatus = "Yes"
            });
            await patients.AddAsync(new Patient
            {
                Id = "API-P2",
                FirstName = "Patient",
                LastName = "Two",
                DoctorId = "API-D2",
                Email = "patient.two@example.com",
                Phone = "0500000012",
                Address = "12 API Street",
                Age = 40,
                TreatmentStatus = "No"
            });

            string today = TestDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string tomorrow = TestDate.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            await appointments.AddAsync(new Appointment
            {
                PatientId = "API-P1",
                DoctorId = "API-D1",
                TreatmentNumber = "API-T1",
                Date = today,
                Time = "09:00"
            });
            await appointments.AddAsync(new Appointment
            {
                PatientId = "API-P2",
                DoctorId = "API-D2",
                TreatmentNumber = "API-T2",
                Date = tomorrow,
                Time = "10:00"
            });

            await patientTreatments.AddAsync(new PatientTreatment
            {
                PatientId = "API-P1",
                TreatmentNumber = "API-T1",
                IsCompleted = "Yes",
                IsPaid = "Yes",
                StartDate = today
            });
            await patientTreatments.AddAsync(new PatientTreatment
            {
                PatientId = "API-P2",
                TreatmentNumber = "API-T2",
                IsCompleted = "No",
                IsPaid = "No",
                StartDate = today
            });

            await audit.AddAsync(CreateAuditEntry(
                AuditEntityTypes.Doctor,
                "API-D1",
                AuditOperation.Created,
                "ApiTester",
                TestUtcNow.UtcDateTime.AddMinutes(-1)));
            await audit.AddAsync(CreateAuditEntry(
                AuditEntityTypes.Doctor,
                "API-D1",
                AuditOperation.Updated,
                "ApiTester",
                TestUtcNow.UtcDateTime));
            await audit.AddAsync(CreateAuditEntry(
                AuditEntityTypes.Patient,
                "API-P2",
                AuditOperation.Created,
                "OtherActor",
                TestUtcNow.UtcDateTime.AddMinutes(-2)));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
            {
                return;
            }

            SqliteConnection.ClearAllPools();
            DeleteIfExists(DatabasePath);
            DeleteIfExists(DatabasePath + "-shm");
            DeleteIfExists(DatabasePath + "-wal");
            DeleteIfExists(DatabasePath + "-journal");
        }

        private static AuditEntry CreateAuditEntry(
            string entityType,
            string entityId,
            AuditOperation operation,
            string actor,
            DateTime timestampUtc)
        {
            return new AuditEntry
            {
                EntityType = entityType,
                EntityId = entityId,
                Operation = operation,
                Actor = actor,
                TimestampUtc = DateTime.SpecifyKind(timestampUtc, DateTimeKind.Utc),
                NewValues = "{}",
                CorrelationId = Guid.NewGuid().ToString("N")
            };
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

        private sealed class FixedApiTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _utcNow;

            public FixedApiTimeProvider(DateTimeOffset utcNow)
            {
                _utcNow = utcNow;
            }

            public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

            public override DateTimeOffset GetUtcNow() => _utcNow;
        }

        private sealed class MigrationOnlyDatabaseInitializer : IDatabaseInitializer
        {
            private readonly ISqliteConnectionFactory _connectionFactory;
            private readonly SqliteDatabaseMigrator _migrator;

            public MigrationOnlyDatabaseInitializer(
                ISqliteConnectionFactory connectionFactory,
                SqliteDatabaseMigrator migrator)
            {
                _connectionFactory = connectionFactory;
                _migrator = migrator;
            }

            public async Task<InitializationResult> InitializeAsync(
                CancellationToken cancellationToken = default)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_connectionFactory.DatabasePath)!);
                await _migrator.MigrateAsync(cancellationToken).ConfigureAwait(false);
                return new InitializationResult(true, _connectionFactory.DatabasePath);
            }
        }
    }
}
