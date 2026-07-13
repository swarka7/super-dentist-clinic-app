using System;

namespace SuperDentist.Application.Queries
{
    public sealed record DoctorListQuery(
        string? Search = null,
        int Limit = 50,
        int Offset = 0);

    public sealed record PatientListQuery(
        string? Search = null,
        string? DoctorId = null,
        int Limit = 50,
        int Offset = 0);

    public sealed record AppointmentListQuery(
        string? Search = null,
        string? DoctorId = null,
        string? PatientId = null,
        DateOnly? FromDate = null,
        DateOnly? ToDate = null,
        int Limit = 50,
        int Offset = 0);

    public sealed record TreatmentListQuery(
        string? Search = null,
        int Limit = 50,
        int Offset = 0);
}
