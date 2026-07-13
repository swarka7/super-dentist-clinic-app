using SuperDentist.Core;
using System;
using System.Collections.Generic;

namespace SuperDentist.Application.Queries
{
    public sealed record DashboardQuery(
        int UpcomingAppointmentLimit = 10,
        int RecentAuditLimit = 10,
        int BreakdownLimit = 20);

    public sealed record DashboardSummary(
        DateTime GeneratedAtUtc,
        int TotalPatients,
        int ActiveDoctorCount,
        int TodayAppointmentCount,
        int UpcomingAppointmentCount,
        int CompletedPatientTreatmentCount,
        int OutstandingPatientTreatmentCount,
        decimal OutstandingTreatmentValue,
        IReadOnlyList<DoctorAppointmentSummary> AppointmentsByDoctor,
        IReadOnlyList<TreatmentUsageSummary> TreatmentUsage,
        IReadOnlyList<UpcomingAppointmentSummary> UpcomingAppointments,
        IReadOnlyList<RecentAuditSummary> RecentAuditActivity);

    public sealed record DoctorAppointmentSummary(
        string DoctorId,
        string DoctorName,
        int AppointmentCount);

    public sealed record TreatmentUsageSummary(
        string TreatmentNumber,
        string TreatmentType,
        decimal UnitPrice,
        int UsageCount,
        decimal TotalValue,
        decimal OutstandingValue);

    public sealed record UpcomingAppointmentSummary(
        string PatientId,
        string PatientName,
        string DoctorId,
        string DoctorName,
        DateOnly Date,
        string Time,
        string TreatmentNumber,
        string TreatmentType);

    public sealed record RecentAuditSummary(
        long Id,
        string EntityType,
        string EntityId,
        AuditOperation Operation,
        string Actor,
        DateTime TimestampUtc,
        string CorrelationId);
}
