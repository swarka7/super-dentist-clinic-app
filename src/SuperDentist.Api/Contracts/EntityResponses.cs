using SuperDentist.Core;

namespace SuperDentist.Api.Contracts
{
    public sealed record DoctorResponse(
        string Id,
        string FirstName,
        string LastName,
        string Phone,
        string Address,
        string Email,
        string Specialization,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc);

    public sealed record PatientResponse(
        string Id,
        string FirstName,
        string LastName,
        string Phone,
        string Address,
        string Email,
        int Age,
        string TreatmentStatus,
        string DoctorId,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc);

    public sealed record AppointmentResponse(
        string PatientId,
        string DoctorId,
        string Date,
        string Time,
        string TreatmentNumber,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc);

    public sealed record TreatmentResponse(
        string Number,
        string Type,
        decimal Price,
        string Tools,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc);

    public sealed record AuditResponse(
        long Id,
        string EntityType,
        string EntityId,
        AuditOperation Operation,
        string Actor,
        DateTime TimestampUtc,
        string? OldValues,
        string? NewValues,
        string CorrelationId);
}
