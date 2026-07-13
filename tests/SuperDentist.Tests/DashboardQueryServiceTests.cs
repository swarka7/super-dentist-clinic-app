using SuperDentist.Application.Queries;
using SuperDentist.Application.Services;
using SuperDentist.Core;
using SuperDentist.Core.Results;
using SuperDentist.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SuperDentist.Tests
{
    public sealed class DashboardQueryServiceTests
    {
        [Fact]
        public async Task GetSummaryAsync_CalculatesOperationalMetricsWithoutSqlite()
        {
            var auditService = new StubAuditService(new AuditEntry
            {
                Id = 7,
                EntityType = AuditEntityTypes.Appointment,
                EntityId = "P2",
                Operation = AuditOperation.Created,
                Actor = "DashboardActor",
                TimestampUtc = new DateTime(2030, 1, 15, 8, 0, 0, DateTimeKind.Utc),
                CorrelationId = "dashboard-correlation"
            });
            var service = new DashboardQueryService(
                new StubDoctorService(
                    new Doctor { Id = "D1", FirstName = "Ada", LastName = "Dentist" },
                    new Doctor { Id = "D2", FirstName = "Grace", LastName = "Clinician" }),
                new StubPatientService(
                    new Patient { Id = "P1", FirstName = "Pat", LastName = "One", DoctorId = "D1" },
                    new Patient { Id = "P2", FirstName = "Pat", LastName = "Two", DoctorId = "D1" }),
                new StubAppointmentService(
                    new Appointment
                    {
                        PatientId = "P1",
                        DoctorId = "D1",
                        TreatmentNumber = "T1",
                        Date = "2030-01-15",
                        Time = "09:00"
                    },
                    new Appointment
                    {
                        PatientId = "P2",
                        DoctorId = "D1",
                        TreatmentNumber = "T2",
                        Date = "2030-01-16",
                        Time = "10:00"
                    }),
                new StubTreatmentService(
                    new Treatment { Number = "T1", Type = "Cleaning", Price = 100 },
                    new Treatment { Number = "T2", Type = "Crown", Price = 250 }),
                new StubPatientTreatmentService(
                    new PatientTreatment
                    {
                        PatientId = "P1",
                        TreatmentNumber = "T1",
                        IsCompleted = "Yes",
                        IsPaid = "Yes"
                    },
                    new PatientTreatment
                    {
                        PatientId = "P2",
                        TreatmentNumber = "T2",
                        IsCompleted = "No",
                        IsPaid = "No"
                    }),
                auditService,
                new UtcTimeProvider(new DateTimeOffset(2030, 1, 15, 12, 0, 0, TimeSpan.Zero)));

            DashboardSummary summary = await service.GetSummaryAsync(new DashboardQuery(
                UpcomingAppointmentLimit: int.MaxValue,
                RecentAuditLimit: int.MaxValue,
                BreakdownLimit: int.MaxValue));

            Assert.Equal(2, summary.TotalPatients);
            Assert.Equal(2, summary.ActiveDoctorCount);
            Assert.Equal(1, summary.TodayAppointmentCount);
            Assert.Equal(1, summary.UpcomingAppointmentCount);
            Assert.Equal(1, summary.CompletedPatientTreatmentCount);
            Assert.Equal(1, summary.OutstandingPatientTreatmentCount);
            Assert.Equal(250m, summary.OutstandingTreatmentValue);
            Assert.Equal(2, summary.AppointmentsByDoctor[0].AppointmentCount);
            Assert.Equal("D1", summary.AppointmentsByDoctor[0].DoctorId);
            Assert.Equal(0, summary.AppointmentsByDoctor[1].AppointmentCount);
            Assert.Equal("Pat Two", Assert.Single(summary.UpcomingAppointments).PatientName);
            Assert.Equal("Crown", summary.UpcomingAppointments[0].TreatmentType);
            Assert.Equal("DashboardActor", Assert.Single(summary.RecentAuditActivity).Actor);
            Assert.Equal(50, auditService.LastQuery?.Limit);
            Assert.Equal(DateTimeKind.Utc, summary.GeneratedAtUtc.Kind);
        }

        private sealed class StubDoctorService : IDoctorService
        {
            private readonly IReadOnlyList<Doctor> _items;

            public StubDoctorService(params Doctor[] items) => _items = items;

            public Task<IReadOnlyList<Doctor>> GetAllAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(_items);

            public Task<Doctor?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
                Task.FromResult(_items.FirstOrDefault(item => item.Id == id));

            public Task<OperationResult> AddAsync(Doctor doctor, CancellationToken cancellationToken = default) =>
                throw new NotSupportedException();

            public Task<OperationResult> UpdateAsync(Doctor doctor, CancellationToken cancellationToken = default) =>
                throw new NotSupportedException();

            public Task<OperationResult> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
                throw new NotSupportedException();
        }

        private sealed class StubPatientService : IPatientService
        {
            private readonly IReadOnlyList<Patient> _items;

            public StubPatientService(params Patient[] items) => _items = items;

            public Task<IReadOnlyList<Patient>> GetAllAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(_items);

            public Task<Patient?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
                Task.FromResult(_items.FirstOrDefault(item => item.Id == id));

            public Task<OperationResult> AddAsync(Patient patient, CancellationToken cancellationToken = default) =>
                throw new NotSupportedException();

            public Task<OperationResult> UpdateAsync(Patient patient, CancellationToken cancellationToken = default) =>
                throw new NotSupportedException();

            public Task<OperationResult> DeleteAsync(string id, CancellationToken cancellationToken = default) =>
                throw new NotSupportedException();
        }

        private sealed class StubAppointmentService : IAppointmentService
        {
            private readonly IReadOnlyList<Appointment> _items;

            public StubAppointmentService(params Appointment[] items) => _items = items;

            public Task<IReadOnlyList<Appointment>> GetAllAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(_items);

            public Task<Appointment?> GetByPatientIdAsync(
                string patientId,
                CancellationToken cancellationToken = default) =>
                Task.FromResult(_items.FirstOrDefault(item => item.PatientId == patientId));

            public Task<IReadOnlyList<Appointment>> GetByDateAsync(
                string date,
                CancellationToken cancellationToken = default) =>
                Task.FromResult<IReadOnlyList<Appointment>>(_items.Where(item => item.Date == date).ToList());

            public Task<OperationResult> AddAsync(
                Appointment appointment,
                CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<OperationResult> UpdateAsync(
                Appointment appointment,
                CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<OperationResult> DeleteAsync(
                string patientId,
                CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }

        private sealed class StubTreatmentService : ITreatmentService
        {
            private readonly IReadOnlyList<Treatment> _items;

            public StubTreatmentService(params Treatment[] items) => _items = items;

            public Task<IReadOnlyList<Treatment>> GetAllAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(_items);

            public Task<Treatment?> GetByNumberAsync(
                string number,
                CancellationToken cancellationToken = default) =>
                Task.FromResult(_items.FirstOrDefault(item => item.Number == number));

            public Task<OperationResult> AddAsync(
                Treatment treatment,
                CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<OperationResult> UpdateAsync(
                Treatment treatment,
                CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<OperationResult> DeleteAsync(
                string number,
                CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }

        private sealed class StubPatientTreatmentService : IPatientTreatmentService
        {
            private readonly IReadOnlyList<PatientTreatment> _items;

            public StubPatientTreatmentService(params PatientTreatment[] items) => _items = items;

            public Task<IReadOnlyList<PatientTreatment>> GetAllAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(_items);

            public Task<PatientTreatment?> GetByPatientIdAsync(
                string patientId,
                CancellationToken cancellationToken = default) =>
                Task.FromResult(_items.FirstOrDefault(item => item.PatientId == patientId));

            public Task<OperationResult> AddAsync(
                PatientTreatment patientTreatment,
                CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<OperationResult> UpdateAsync(
                PatientTreatment patientTreatment,
                CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<OperationResult> DeleteAsync(
                string patientId,
                CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }

        private sealed class StubAuditService : IAuditService
        {
            private readonly IReadOnlyList<AuditEntry> _entries;

            public StubAuditService(params AuditEntry[] entries) => _entries = entries;

            public AuditQuery? LastQuery { get; private set; }

            public Task RecordAsync(
                string entityType,
                string entityId,
                AuditOperation operation,
                IReadOnlyDictionary<string, object?>? oldValues,
                IReadOnlyDictionary<string, object?>? newValues,
                string? correlationId = null,
                CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<IReadOnlyList<AuditEntry>> SearchAsync(
                AuditQuery query,
                CancellationToken cancellationToken = default)
            {
                LastQuery = query;
                return Task.FromResult(_entries);
            }
        }

        private sealed class UtcTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _utcNow;

            public UtcTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

            public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

            public override DateTimeOffset GetUtcNow() => _utcNow;
        }
    }
}
