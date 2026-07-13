using Microsoft.Extensions.Options;
using SuperDentist.Application.Services;
using SuperDentist.Core;
using SuperDentist.Core.Options;
using SuperDentist.Core.Repositories;
using SuperDentist.Infrastructure.Data;
using SuperDentist.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SuperDentist.Tests
{
    public sealed class AuditTrailTests
    {
        [Fact]
        public async Task Create_WhenSuccessful_PersistsAuditWithActorAndUtcTimestampAcrossConnections()
        {
            using var database = await SqliteTestDatabase.CreateAsync();
            var auditService = new AuditService(
                database.AuditRepository,
                new LocalCurrentActorProvider(),
                TimeProvider.System);
            var service = new DoctorService(
                new SqliteDoctorRepository(database.ConnectionFactory),
                auditService,
                database.Transaction);

            var result = await service.AddAsync(CreateDoctor("AUDIT-D1", "Original"));

            Assert.True(result.Success, result.ErrorMessage);

            var reopenedFactory = new SqliteConnectionFactory(
                Options.Create(new DatabaseOptions { Path = database.DatabasePath }));
            var reopenedRepository = new SqliteAuditRepository(reopenedFactory);
            IReadOnlyList<AuditEntry> entries = await reopenedRepository.SearchAsync(new AuditQuery());

            AuditEntry entry = Assert.Single(entries);
            Assert.Equal(AuditEntityTypes.Doctor, entry.EntityType);
            Assert.Equal("AUDIT-D1", entry.EntityId);
            Assert.Equal(AuditOperation.Created, entry.Operation);
            Assert.Equal("LocalUser", entry.Actor);
            Assert.Equal(DateTimeKind.Utc, entry.TimestampUtc.Kind);
            Assert.Null(entry.OldValues);
            Assert.Contains("\"FirstName\":\"Original\"", entry.NewValues);
        }

        [Fact]
        public async Task Update_WhenSuccessful_RecordsCorrectBeforeAndAfterValues()
        {
            using var database = await SqliteTestDatabase.CreateAsync();
            var service = CreateDoctorService(database);
            await service.AddAsync(CreateDoctor("AUDIT-D2", "Before"));

            Doctor updated = CreateDoctor("AUDIT-D2", "After");
            updated.Salary = 12000;
            updated.Phone = null!;
            var result = await service.UpdateAsync(updated);

            Assert.True(result.Success, result.ErrorMessage);
            IReadOnlyList<AuditEntry> entries = await database.AuditRepository.SearchAsync(new AuditQuery
            {
                EntityType = AuditEntityTypes.Doctor,
                EntityId = "AUDIT-D2",
                Operation = AuditOperation.Updated
            });

            AuditEntry entry = Assert.Single(entries);
            using JsonDocument oldValues = JsonDocument.Parse(entry.OldValues!);
            using JsonDocument newValues = JsonDocument.Parse(entry.NewValues!);
            Assert.Equal("Before", oldValues.RootElement.GetProperty("FirstName").GetString());
            Assert.Equal(8000, oldValues.RootElement.GetProperty("Salary").GetInt32());
            Assert.Equal("After", newValues.RootElement.GetProperty("FirstName").GetString());
            Assert.Equal(12000, newValues.RootElement.GetProperty("Salary").GetInt32());
            Assert.Equal(string.Empty, newValues.RootElement.GetProperty("Phone").GetString());
        }

        [Fact]
        public async Task Create_WhenBusinessValidationFails_DoesNotAddSuccessAuditEntry()
        {
            using var database = await SqliteTestDatabase.CreateAsync();
            var service = CreateDoctorService(database);
            Assert.True((await service.AddAsync(CreateDoctor("AUDIT-D3", "First"))).Success);

            var duplicate = await service.AddAsync(CreateDoctor("AUDIT-D3", "Duplicate"));
            var missing = await service.DeleteAsync("AUDIT-MISSING");

            Assert.False(duplicate.Success);
            Assert.False(missing.Success);
            IReadOnlyList<AuditEntry> entries = await database.AuditRepository.SearchAsync(new AuditQuery
            {
                EntityType = AuditEntityTypes.Doctor
            });
            AuditEntry entry = Assert.Single(entries);
            Assert.Equal(AuditOperation.Created, entry.Operation);
        }

        [Fact]
        public async Task Delete_WhenSuccessful_RecordsOldValuesAndNullNewValues()
        {
            using var database = await SqliteTestDatabase.CreateAsync();
            var service = CreateDoctorService(database);
            Assert.True((await service.AddAsync(CreateDoctor("AUDIT-D7", "Delete"))).Success);

            var result = await service.DeleteAsync("AUDIT-D7");

            Assert.True(result.Success, result.ErrorMessage);
            IReadOnlyList<AuditEntry> entries = await database.AuditRepository.SearchAsync(new AuditQuery
            {
                EntityType = AuditEntityTypes.Doctor,
                EntityId = "AUDIT-D7",
                Operation = AuditOperation.Deleted
            });
            AuditEntry entry = Assert.Single(entries);
            Assert.NotNull(entry.OldValues);
            Assert.Null(entry.NewValues);
            Assert.Null(await new SqliteDoctorRepository(database.ConnectionFactory)
                .GetByIdAsync("AUDIT-D7"));
        }
        [Fact]
        public async Task Create_WhenAuditPersistenceFails_RollsBackBusinessChange()
        {
            using var database = await SqliteTestDatabase.CreateAsync();
            var doctorRepository = new SqliteDoctorRepository(database.ConnectionFactory);
            var auditService = new AuditService(
                new ThrowingAuditRepository(),
                new FixedActorProvider("FailureTest"),
                TimeProvider.System);
            var service = new DoctorService(
                doctorRepository,
                auditService,
                database.Transaction);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.AddAsync(CreateDoctor("AUDIT-D4", "Rollback")));

            Assert.Null(await doctorRepository.GetByIdAsync("AUDIT-D4"));

            var recoveryService = new DoctorService(
                doctorRepository,
                database.CreateAuditService("RecoveryActor"),
                database.Transaction);
            Assert.True((await recoveryService.AddAsync(CreateDoctor("AUDIT-D5", "Recovery"))).Success);
        }

        [Fact]
        public async Task Create_WhenAuditSerializationFails_RollsBackBusinessChange()
        {
            using var database = await SqliteTestDatabase.CreateAsync();
            var doctorRepository = new SqliteDoctorRepository(database.ConnectionFactory);
            var auditService = database.CreateAuditService("SerializationTest");

            await Assert.ThrowsAsync<NotSupportedException>(() =>
                database.Transaction.ExecuteAsync(async cancellationToken =>
                {
                    Doctor doctor = CreateDoctor("AUDIT-D6", "Serialization");
                    await doctorRepository.AddAsync(doctor, cancellationToken);
                    await auditService.RecordAsync(
                        AuditEntityTypes.Doctor,
                        doctor.Id,
                        AuditOperation.Created,
                        null,
                        new Dictionary<string, object?>
                        {
                            ["Unsupported"] = new Action(() => { })
                        },
                        cancellationToken: cancellationToken);
                    return true;
                }));

            Assert.Null(await doctorRepository.GetByIdAsync("AUDIT-D6"));
        }

        [Fact]
        public async Task Search_AppliesFiltersLimitsAndNewestFirstOrdering()
        {
            using var database = await SqliteTestDatabase.CreateAsync();
            await database.AuditRepository.AddAsync(CreateEntry(
                AuditEntityTypes.Doctor, "D-100", AuditOperation.Created, "ActorA", 1));
            await database.AuditRepository.AddAsync(CreateEntry(
                AuditEntityTypes.Patient, "P-100", AuditOperation.Created, "ActorB", 2));
            await database.AuditRepository.AddAsync(CreateEntry(
                AuditEntityTypes.Doctor, "D-100", AuditOperation.Updated, "ActorA", 3));
            await database.AuditRepository.AddAsync(CreateEntry(
                AuditEntityTypes.Patient, "P-200", AuditOperation.Created, "ActorC", 3));

            IReadOnlyList<AuditEntry> filtered = await database.AuditRepository.SearchAsync(new AuditQuery
            {
                EntityType = AuditEntityTypes.Doctor,
                EntityId = "100",
                Actor = "ActorA",
                Operation = AuditOperation.Updated,
                FromUtc = new DateTime(2040, 1, 1, 0, 0, 2, DateTimeKind.Utc),
                ToUtc = new DateTime(2040, 1, 1, 0, 0, 4, DateTimeKind.Utc),
                Limit = 10
            });

            AuditEntry match = Assert.Single(filtered);
            Assert.Equal(AuditOperation.Updated, match.Operation);

            IReadOnlyList<AuditEntry> newest = await database.AuditRepository.SearchAsync(new AuditQuery
            {
                Limit = 2
            });
            Assert.Equal(2, newest.Count);
            Assert.Equal("P-200", newest[0].EntityId);
            Assert.Equal(AuditOperation.Updated, newest[1].Operation);

            IReadOnlyList<AuditEntry> escapedSearch = await database.AuditRepository.SearchAsync(new AuditQuery
            {
                EntityId = "%_\\",
                Limit = 10
            });
            Assert.Empty(escapedSearch);
        }

        [Fact]
        public async Task AuditEntries_WhenMutationIsAttempted_AreAppendOnly()
        {
            using var database = await SqliteTestDatabase.CreateAsync();
            await database.AuditRepository.AddAsync(CreateEntry(
                AuditEntityTypes.Doctor, "D-LOCKED", AuditOperation.Created, "ActorA", 1));

            await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(
                () => database.ExecuteAsync("UPDATE AuditEntries SET Actor = 'Changed';"));
            await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(
                () => database.ExecuteAsync("DELETE FROM AuditEntries;"));

            Assert.Equal(
                1L,
                Convert.ToInt64(await database.ScalarAsync("SELECT COUNT(*) FROM AuditEntries;")));
        }
        private static DoctorService CreateDoctorService(SqliteTestDatabase database)
        {
            return new DoctorService(
                new SqliteDoctorRepository(database.ConnectionFactory),
                database.CreateAuditService("AuditTestActor"),
                database.Transaction);
        }

        private static Doctor CreateDoctor(string id, string firstName)
        {
            return new Doctor
            {
                Id = id,
                FirstName = firstName,
                LastName = "Tester",
                Phone = "0500000000",
                Address = "1 Audit St",
                Email = "audit@example.com",
                Specialization = "General",
                Salary = 8000
            };
        }

        private static AuditEntry CreateEntry(
            string entityType,
            string entityId,
            AuditOperation operation,
            string actor,
            int second)
        {
            return new AuditEntry
            {
                EntityType = entityType,
                EntityId = entityId,
                Operation = operation,
                Actor = actor,
                TimestampUtc = new DateTime(2040, 1, 1, 0, 0, second, DateTimeKind.Utc),
                NewValues = "{}",
                CorrelationId = Guid.NewGuid().ToString("N")
            };
        }
    }
}
