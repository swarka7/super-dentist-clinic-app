using SuperDentist.Application.Queries;
using SuperDentist.Core;
using SuperDentist.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Application.Services
{
    public sealed class DashboardQueryService : IDashboardQueryService
    {
        private const int MaximumDashboardListSize = 50;

        private readonly IDoctorService _doctorService;
        private readonly IPatientService _patientService;
        private readonly IAppointmentService _appointmentService;
        private readonly ITreatmentService _treatmentService;
        private readonly IPatientTreatmentService _patientTreatmentService;
        private readonly IAuditService _auditService;
        private readonly TimeProvider _timeProvider;

        public DashboardQueryService(
            IDoctorService doctorService,
            IPatientService patientService,
            IAppointmentService appointmentService,
            ITreatmentService treatmentService,
            IPatientTreatmentService patientTreatmentService,
            IAuditService auditService,
            TimeProvider timeProvider)
        {
            _doctorService = doctorService;
            _patientService = patientService;
            _appointmentService = appointmentService;
            _treatmentService = treatmentService;
            _patientTreatmentService = patientTreatmentService;
            _auditService = auditService;
            _timeProvider = timeProvider;
        }

        public async Task<DashboardSummary> GetSummaryAsync(
            DashboardQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            int upcomingLimit = ClampLimit(query.UpcomingAppointmentLimit);
            int auditLimit = ClampLimit(query.RecentAuditLimit);
            int breakdownLimit = ClampLimit(query.BreakdownLimit);

            Task<IReadOnlyList<Doctor>> doctorsTask = _doctorService.GetAllAsync(cancellationToken);
            Task<IReadOnlyList<Patient>> patientsTask = _patientService.GetAllAsync(cancellationToken);
            Task<IReadOnlyList<Appointment>> appointmentsTask = _appointmentService.GetAllAsync(cancellationToken);
            Task<IReadOnlyList<Treatment>> treatmentsTask = _treatmentService.GetAllAsync(cancellationToken);
            Task<IReadOnlyList<PatientTreatment>> patientTreatmentsTask =
                _patientTreatmentService.GetAllAsync(cancellationToken);
            Task<IReadOnlyList<AuditEntry>> auditTask = _auditService.SearchAsync(
                new AuditQuery { Limit = auditLimit },
                cancellationToken);

            await Task.WhenAll(
                doctorsTask,
                patientsTask,
                appointmentsTask,
                treatmentsTask,
                patientTreatmentsTask,
                auditTask).ConfigureAwait(false);

            IReadOnlyList<Doctor> doctors = await doctorsTask.ConfigureAwait(false);
            IReadOnlyList<Patient> patients = await patientsTask.ConfigureAwait(false);
            IReadOnlyList<Appointment> appointments = await appointmentsTask.ConfigureAwait(false);
            IReadOnlyList<Treatment> treatments = await treatmentsTask.ConfigureAwait(false);
            IReadOnlyList<PatientTreatment> patientTreatments = await patientTreatmentsTask.ConfigureAwait(false);
            IReadOnlyList<AuditEntry> auditEntries = await auditTask.ConfigureAwait(false);

            DateOnly today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
            var datedAppointments = appointments
                .Select(appointment => new
                {
                    Appointment = appointment,
                    HasDate = ClinicQueryService.TryParseDate(appointment.Date, out DateOnly date),
                    Date = date
                })
                .Where(item => item.HasDate)
                .ToList();

            ILookup<string, Doctor> doctorsById = doctors.ToLookup(
                doctor => doctor.Id,
                StringComparer.OrdinalIgnoreCase);
            ILookup<string, Patient> patientsById = patients.ToLookup(
                patient => patient.Id,
                StringComparer.OrdinalIgnoreCase);
            ILookup<string, Treatment> treatmentsByNumber = treatments.ToLookup(
                treatment => treatment.Number,
                StringComparer.OrdinalIgnoreCase);
            ILookup<string, Appointment> appointmentsByDoctorId = appointments.ToLookup(
                appointment => appointment.DoctorId,
                StringComparer.OrdinalIgnoreCase);
            ILookup<string, PatientTreatment> patientTreatmentsByNumber = patientTreatments.ToLookup(
                item => item.TreatmentNumber,
                StringComparer.OrdinalIgnoreCase);

            int completedCount = patientTreatments.Count(item => IsYes(item.IsCompleted));
            int outstandingCount = patientTreatments.Count - completedCount;
            decimal outstandingValue = patientTreatments
                .Where(item => !IsYes(item.IsPaid))
                .Sum(item => treatmentsByNumber[item.TreatmentNumber].FirstOrDefault()?.Price ?? 0);

            IReadOnlyList<DoctorAppointmentSummary> appointmentsByDoctor = doctors
                .Select(doctor => new DoctorAppointmentSummary(
                    doctor.Id,
                    FullName(doctor.FirstName, doctor.LastName),
                    appointmentsByDoctorId[doctor.Id].Count()))
                .OrderByDescending(summary => summary.AppointmentCount)
                .ThenBy(summary => summary.DoctorName, StringComparer.OrdinalIgnoreCase)
                .Take(breakdownLimit)
                .ToList();

            IReadOnlyList<TreatmentUsageSummary> treatmentUsage = treatments
                .Select(treatment =>
                {
                    IReadOnlyList<PatientTreatment> usage =
                        patientTreatmentsByNumber[treatment.Number].ToList();
                    decimal unitPrice = treatment.Price;
                    return new TreatmentUsageSummary(
                        treatment.Number,
                        treatment.Type,
                        unitPrice,
                        usage.Count,
                        unitPrice * usage.Count,
                        unitPrice * usage.Count(item => !IsYes(item.IsPaid)));
                })
                .OrderByDescending(summary => summary.UsageCount)
                .ThenByDescending(summary => summary.TotalValue)
                .ThenBy(summary => summary.TreatmentNumber, StringComparer.Ordinal)
                .Take(breakdownLimit)
                .ToList();

            IReadOnlyList<UpcomingAppointmentSummary> upcomingAppointments = datedAppointments
                .Where(item => item.Date > today)
                .OrderBy(item => item.Date)
                .ThenBy(item => item.Appointment.Time, StringComparer.Ordinal)
                .ThenBy(item => item.Appointment.PatientId, StringComparer.Ordinal)
                .Take(upcomingLimit)
                .Select(item =>
                {
                    Appointment appointment = item.Appointment;
                    Patient? patient = patientsById[appointment.PatientId].FirstOrDefault();
                    Doctor? doctor = doctorsById[appointment.DoctorId].FirstOrDefault();
                    Treatment? treatment = treatmentsByNumber[appointment.TreatmentNumber].FirstOrDefault();
                    return new UpcomingAppointmentSummary(
                        appointment.PatientId,
                        patient == null ? string.Empty : FullName(patient.FirstName, patient.LastName),
                        appointment.DoctorId,
                        doctor == null ? string.Empty : FullName(doctor.FirstName, doctor.LastName),
                        item.Date,
                        appointment.Time,
                        appointment.TreatmentNumber,
                        treatment?.Type ?? string.Empty);
                })
                .ToList();

            IReadOnlyList<RecentAuditSummary> recentAudit = auditEntries
                .Take(auditLimit)
                .Select(entry => new RecentAuditSummary(
                    entry.Id,
                    entry.EntityType,
                    entry.EntityId,
                    entry.Operation,
                    entry.Actor,
                    entry.TimestampUtc,
                    entry.CorrelationId))
                .ToList();

            return new DashboardSummary(
                _timeProvider.GetUtcNow().UtcDateTime,
                patients.Count,
                doctors.Count,
                datedAppointments.Count(item => item.Date == today),
                datedAppointments.Count(item => item.Date > today),
                completedCount,
                outstandingCount,
                outstandingValue,
                appointmentsByDoctor,
                treatmentUsage,
                upcomingAppointments,
                recentAudit);
        }

        private static int ClampLimit(int limit) => Math.Clamp(limit, 1, MaximumDashboardListSize);

        private static bool IsYes(string? value) =>
            string.Equals(value?.Trim(), "Yes", StringComparison.OrdinalIgnoreCase);

        private static string FullName(string firstName, string lastName) =>
            string.Join(' ', new[] { firstName, lastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
