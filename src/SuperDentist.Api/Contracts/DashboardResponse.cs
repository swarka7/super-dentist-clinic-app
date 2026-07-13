using SuperDentist.Core;

namespace SuperDentist.Api.Contracts
{
    public sealed record DashboardResponse(
        DateTime GeneratedAtUtc,
        int TotalPatients,
        int ActiveDoctorCount,
        int TodayAppointmentCount,
        int UpcomingAppointmentCount,
        int CompletedPatientTreatmentCount,
        int OutstandingPatientTreatmentCount,
        decimal OutstandingTreatmentValue,
        IReadOnlyList<DoctorAppointmentResponse> AppointmentsByDoctor,
        IReadOnlyList<TreatmentUsageResponse> TreatmentUsage,
        IReadOnlyList<UpcomingAppointmentResponse> UpcomingAppointments,
        IReadOnlyList<RecentAuditResponse> RecentAuditActivity);

    public sealed record DoctorAppointmentResponse(
        string DoctorId,
        string DoctorName,
        int AppointmentCount);

    public sealed record TreatmentUsageResponse(
        string TreatmentNumber,
        string TreatmentType,
        decimal UnitPrice,
        int UsageCount,
        decimal TotalValue,
        decimal OutstandingValue);

    public sealed record UpcomingAppointmentResponse(
        string PatientId,
        string PatientName,
        string DoctorId,
        string DoctorName,
        DateOnly Date,
        string Time,
        string TreatmentNumber,
        string TreatmentType);

    public sealed record RecentAuditResponse(
        long Id,
        string EntityType,
        string EntityId,
        AuditOperation Operation,
        string Actor,
        DateTime TimestampUtc,
        string CorrelationId);
}
