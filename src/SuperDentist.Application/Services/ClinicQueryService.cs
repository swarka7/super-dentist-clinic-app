using SuperDentist.Application.Queries;
using SuperDentist.Core;
using SuperDentist.Core.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Application.Services
{
    public sealed class ClinicQueryService : IClinicQueryService
    {
        private const int MaximumPageSize = 200;

        private readonly IDoctorService _doctorService;
        private readonly IPatientService _patientService;
        private readonly IAppointmentService _appointmentService;
        private readonly ITreatmentService _treatmentService;

        public ClinicQueryService(
            IDoctorService doctorService,
            IPatientService patientService,
            IAppointmentService appointmentService,
            ITreatmentService treatmentService)
        {
            _doctorService = doctorService;
            _patientService = patientService;
            _appointmentService = appointmentService;
            _treatmentService = treatmentService;
        }

        public async Task<PagedResult<Doctor>> GetDoctorsAsync(
            DoctorListQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            IReadOnlyList<Doctor> doctors = await _doctorService
                .GetAllAsync(cancellationToken)
                .ConfigureAwait(false);
            IEnumerable<Doctor> filtered = doctors;

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string search = query.Search.Trim();
                filtered = filtered.Where(doctor =>
                    Contains(doctor.Id, search)
                    || Contains(doctor.FirstName, search)
                    || Contains(doctor.LastName, search)
                    || Contains(doctor.Email, search)
                    || Contains(doctor.Phone, search)
                    || Contains(doctor.Specialization, search));
            }

            return Page(filtered, query.Limit, query.Offset);
        }

        public async Task<PagedResult<Patient>> GetPatientsAsync(
            PatientListQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            IReadOnlyList<Patient> patients = await _patientService
                .GetAllAsync(cancellationToken)
                .ConfigureAwait(false);
            IEnumerable<Patient> filtered = patients;

            if (!string.IsNullOrWhiteSpace(query.DoctorId))
            {
                string doctorId = query.DoctorId.Trim();
                filtered = filtered.Where(patient => EqualsId(patient.DoctorId, doctorId));
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string search = query.Search.Trim();
                filtered = filtered.Where(patient =>
                    Contains(patient.Id, search)
                    || Contains(patient.FirstName, search)
                    || Contains(patient.LastName, search)
                    || Contains(patient.Email, search)
                    || Contains(patient.Phone, search)
                    || Contains(patient.TreatmentStatus, search));
            }

            return Page(filtered, query.Limit, query.Offset);
        }

        public async Task<PagedResult<Appointment>> GetAppointmentsAsync(
            AppointmentListQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            IReadOnlyList<Appointment> appointments = await _appointmentService
                .GetAllAsync(cancellationToken)
                .ConfigureAwait(false);
            IEnumerable<Appointment> filtered = appointments;

            if (!string.IsNullOrWhiteSpace(query.DoctorId))
            {
                string doctorId = query.DoctorId.Trim();
                filtered = filtered.Where(appointment => EqualsId(appointment.DoctorId, doctorId));
            }

            if (!string.IsNullOrWhiteSpace(query.PatientId))
            {
                string patientId = query.PatientId.Trim();
                filtered = filtered.Where(appointment => EqualsId(appointment.PatientId, patientId));
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string search = query.Search.Trim();
                filtered = filtered.Where(appointment =>
                    Contains(appointment.PatientId, search)
                    || Contains(appointment.DoctorId, search)
                    || Contains(appointment.TreatmentNumber, search)
                    || Contains(appointment.Date, search)
                    || Contains(appointment.Time, search));
            }

            if (query.FromDate.HasValue || query.ToDate.HasValue)
            {
                filtered = filtered.Where(appointment =>
                    TryParseDate(appointment.Date, out DateOnly date)
                    && (!query.FromDate.HasValue || date >= query.FromDate.Value)
                    && (!query.ToDate.HasValue || date <= query.ToDate.Value));
            }

            filtered = filtered
                .OrderBy(appointment => appointment.Date, StringComparer.Ordinal)
                .ThenBy(appointment => appointment.Time, StringComparer.Ordinal)
                .ThenBy(appointment => appointment.PatientId, StringComparer.Ordinal);

            return Page(filtered, query.Limit, query.Offset);
        }

        public async Task<PagedResult<Treatment>> GetTreatmentsAsync(
            TreatmentListQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            IReadOnlyList<Treatment> treatments = await _treatmentService
                .GetAllAsync(cancellationToken)
                .ConfigureAwait(false);
            IEnumerable<Treatment> filtered = treatments;

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                string search = query.Search.Trim();
                filtered = filtered.Where(treatment =>
                    Contains(treatment.Number, search)
                    || Contains(treatment.Type, search)
                    || Contains(treatment.Tools, search));
            }

            return Page(filtered, query.Limit, query.Offset);
        }

        internal static bool TryParseDate(string value, out DateOnly date)
        {
            return DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date);
        }

        private static PagedResult<T> Page<T>(IEnumerable<T> source, int requestedLimit, int requestedOffset)
        {
            int limit = Math.Clamp(requestedLimit, 1, MaximumPageSize);
            int offset = Math.Max(requestedOffset, 0);
            IReadOnlyList<T> allItems = source.ToList();
            IReadOnlyList<T> page = allItems.Skip(offset).Take(limit).ToList();
            return new PagedResult<T>(page, allItems.Count, limit, offset);
        }

        private static bool Contains(string? value, string search)
        {
            return !string.IsNullOrEmpty(value)
                && value.Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        private static bool EqualsId(string? value, string expected)
        {
            return string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
