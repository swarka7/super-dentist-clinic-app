using SuperDentist.Application.Services;
using SuperDentist.Core;
using SuperDentist.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SuperDentist.Tests
{
    public sealed class DoctorApplicationServiceTests
    {
        [Fact]
        public async Task AddAsync_WhenDoctorAlreadyExists_ReturnsDuplicateMessageWithoutAudit()
        {
            var repository = new FakeDoctorRepository(new Doctor { Id = "999000001" });
            var auditRepository = new RecordingAuditRepository();
            var auditService = new AuditService(
                auditRepository,
                new FixedActorProvider("UnitTestActor"),
                TimeProvider.System);
            var service = new DoctorService(
                repository,
                auditService,
                new ImmediateApplicationTransaction());

            var result = await service.AddAsync(new Doctor { Id = "999000001" });

            Assert.False(result.Success);
            Assert.Equal("A doctor with this ID already exists.", result.ErrorMessage);
            Assert.Empty(repository.AddedDoctors);
            Assert.Empty(auditRepository.Entries);
        }

        [Fact]
        public async Task AddAsync_WhenSuccessful_CreatesDeterministicAuditEntryWithoutSqlite()
        {
            var repository = new FakeDoctorRepository();
            var auditRepository = new RecordingAuditRepository();
            var timestamp = new DateTimeOffset(2035, 4, 5, 6, 7, 8, TimeSpan.Zero);
            var auditService = new AuditService(
                auditRepository,
                new FixedActorProvider("PortfolioTester"),
                new FixedTimeProvider(timestamp));
            var service = new DoctorService(
                repository,
                auditService,
                new ImmediateApplicationTransaction());

            var doctor = new Doctor
            {
                Id = "999000002",
                FirstName = "Ada",
                LastName = "Lovelace",
                Specialization = "General",
                Salary = 9000
            };

            var result = await service.AddAsync(doctor);

            Assert.True(result.Success, result.ErrorMessage);
            AuditEntry entry = Assert.Single(auditRepository.Entries);
            Assert.Equal(AuditOperation.Created, entry.Operation);
            Assert.Equal("PortfolioTester", entry.Actor);
            Assert.Equal(timestamp.UtcDateTime, entry.TimestampUtc);
            Assert.Equal(DateTimeKind.Utc, entry.TimestampUtc.Kind);
            Assert.Null(entry.OldValues);
            Assert.Equal(
                """{"Address":"","Email":"","FirstName":"Ada","Id":"999000002","LastName":"Lovelace","Phone":"","Salary":9000,"Specialization":"General"}""",
                entry.NewValues);
            Assert.False(string.IsNullOrWhiteSpace(entry.CorrelationId));
        }

        [Fact]
        public async Task RecordAsync_SerializesSupportedValueTypesConsistently()
        {
            var repository = new RecordingAuditRepository();
            var auditService = new AuditService(
                repository,
                new FixedActorProvider("TypedActor"),
                TimeProvider.System);
            var values = new Dictionary<string, object?>
            {
                ["When"] = new DateTime(2042, 3, 4, 5, 6, 7, DateTimeKind.Utc),
                ["Optional"] = null,
                ["Flag"] = true,
                ["Operation"] = AuditOperation.Updated,
                ["Amount"] = 12.50m
            };

            await auditService.RecordAsync(
                AuditEntityTypes.Treatment,
                "TYPE-1",
                AuditOperation.Updated,
                values,
                values,
                correlationId: " correlation-1 ");

            AuditEntry entry = Assert.Single(repository.Entries);
            Assert.Equal("correlation-1", entry.CorrelationId);
            using JsonDocument document = JsonDocument.Parse(entry.NewValues!);
            Assert.Equal(12.50m, document.RootElement.GetProperty("Amount").GetDecimal());
            Assert.True(document.RootElement.GetProperty("Flag").GetBoolean());
            Assert.Equal("Updated", document.RootElement.GetProperty("Operation").GetString());
            Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("Optional").ValueKind);
            Assert.Equal(
                DateTimeKind.Utc,
                document.RootElement.GetProperty("When").GetDateTime().Kind);
            Assert.Equal(
                new[] { "Amount", "Flag", "Operation", "Optional", "When" },
                document.RootElement.EnumerateObject().Select(property => property.Name));
        }

        [Fact]
        public async Task SearchAsync_WhenLimitIsUnbounded_UsesSafeMaximumWithoutMutatingCaller()
        {
            var repository = new RecordingAuditRepository();
            var auditService = new AuditService(
                repository,
                new FixedActorProvider("SearchActor"),
                TimeProvider.System);
            var query = new AuditQuery { Limit = int.MaxValue };

            await auditService.SearchAsync(query);

            Assert.Equal(int.MaxValue, query.Limit);
            Assert.NotNull(repository.LastQuery);
            Assert.Equal(500, repository.LastQuery!.Limit);
        }
        private sealed class FakeDoctorRepository : IDoctorRepository
        {
            private readonly List<Doctor> _doctors;

            public FakeDoctorRepository(params Doctor[] doctors)
            {
                _doctors = doctors.ToList();
            }

            public List<Doctor> AddedDoctors { get; } = new();

            public Task<IReadOnlyList<Doctor>> GetAllAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<Doctor>>(_doctors);
            }

            public Task<Doctor?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_doctors.FirstOrDefault(doctor => doctor.Id == id));
            }

            public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_doctors.Any(doctor => doctor.Id == id));
            }

            public Task AddAsync(Doctor doctor, CancellationToken cancellationToken = default)
            {
                AddedDoctors.Add(doctor);
                _doctors.Add(doctor);
                return Task.CompletedTask;
            }

            public Task UpdateAsync(Doctor doctor, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
            {
                _doctors.RemoveAll(doctor => doctor.Id == id);
                return Task.CompletedTask;
            }
        }
    }
}