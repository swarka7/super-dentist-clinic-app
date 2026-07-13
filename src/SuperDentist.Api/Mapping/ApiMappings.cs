using SuperDentist.Api.Contracts;
using SuperDentist.Application.Queries;
using SuperDentist.Core;

namespace SuperDentist.Api.Mapping
{
    internal static class ApiMappings
    {
        public static PagedResponse<DoctorResponse> ToResponse(this PagedResult<Doctor> result)
        {
            return new PagedResponse<DoctorResponse>(
                result.Items.Select(ToResponse).ToList(),
                result.TotalCount,
                result.Limit,
                result.Offset);
        }

        public static PagedResponse<PatientResponse> ToResponse(this PagedResult<Patient> result)
        {
            return new PagedResponse<PatientResponse>(
                result.Items.Select(ToResponse).ToList(),
                result.TotalCount,
                result.Limit,
                result.Offset);
        }

        public static PagedResponse<AppointmentResponse> ToResponse(this PagedResult<Appointment> result)
        {
            return new PagedResponse<AppointmentResponse>(
                result.Items.Select(ToResponse).ToList(),
                result.TotalCount,
                result.Limit,
                result.Offset);
        }

        public static PagedResponse<TreatmentResponse> ToResponse(this PagedResult<Treatment> result)
        {
            return new PagedResponse<TreatmentResponse>(
                result.Items.Select(ToResponse).ToList(),
                result.TotalCount,
                result.Limit,
                result.Offset);
        }

        public static DoctorResponse ToResponse(this Doctor doctor)
        {
            return new DoctorResponse(
                doctor.Id,
                doctor.FirstName,
                doctor.LastName,
                doctor.Phone,
                doctor.Address,
                doctor.Email,
                doctor.Specialization,
                doctor.CreatedAtUtc,
                doctor.UpdatedAtUtc);
        }

        public static PatientResponse ToResponse(this Patient patient)
        {
            return new PatientResponse(
                patient.Id,
                patient.FirstName,
                patient.LastName,
                patient.Phone,
                patient.Address,
                patient.Email,
                patient.Age,
                patient.TreatmentStatus,
                patient.DoctorId,
                patient.CreatedAtUtc,
                patient.UpdatedAtUtc);
        }

        public static AppointmentResponse ToResponse(this Appointment appointment)
        {
            return new AppointmentResponse(
                appointment.PatientId,
                appointment.DoctorId,
                appointment.Date,
                appointment.Time,
                appointment.TreatmentNumber,
                appointment.CreatedAtUtc,
                appointment.UpdatedAtUtc);
        }

        public static TreatmentResponse ToResponse(this Treatment treatment)
        {
            return new TreatmentResponse(
                treatment.Number,
                treatment.Type,
                treatment.Price,
                treatment.Tools,
                treatment.CreatedAtUtc,
                treatment.UpdatedAtUtc);
        }

        public static AuditResponse ToResponse(this AuditEntry entry)
        {
            return new AuditResponse(
                entry.Id,
                entry.EntityType,
                entry.EntityId,
                entry.Operation,
                entry.Actor,
                entry.TimestampUtc,
                entry.OldValues,
                entry.NewValues,
                entry.CorrelationId);
        }

        public static DashboardResponse ToResponse(this DashboardSummary summary)
        {
            return new DashboardResponse(
                summary.GeneratedAtUtc,
                summary.TotalPatients,
                summary.ActiveDoctorCount,
                summary.TodayAppointmentCount,
                summary.UpcomingAppointmentCount,
                summary.CompletedPatientTreatmentCount,
                summary.OutstandingPatientTreatmentCount,
                summary.OutstandingTreatmentValue,
                summary.AppointmentsByDoctor
                    .Select(item => new DoctorAppointmentResponse(
                        item.DoctorId,
                        item.DoctorName,
                        item.AppointmentCount))
                    .ToList(),
                summary.TreatmentUsage
                    .Select(item => new TreatmentUsageResponse(
                        item.TreatmentNumber,
                        item.TreatmentType,
                        item.UnitPrice,
                        item.UsageCount,
                        item.TotalValue,
                        item.OutstandingValue))
                    .ToList(),
                summary.UpcomingAppointments
                    .Select(item => new UpcomingAppointmentResponse(
                        item.PatientId,
                        item.PatientName,
                        item.DoctorId,
                        item.DoctorName,
                        item.Date,
                        item.Time,
                        item.TreatmentNumber,
                        item.TreatmentType))
                    .ToList(),
                summary.RecentAuditActivity
                    .Select(item => new RecentAuditResponse(
                        item.Id,
                        item.EntityType,
                        item.EntityId,
                        item.Operation,
                        item.Actor,
                        item.TimestampUtc,
                        item.CorrelationId))
                    .ToList());
        }
    }
}
