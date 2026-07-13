using SuperDentist.Core;
using System.ComponentModel.DataAnnotations;

namespace SuperDentist.Api.Contracts
{
    public abstract class PagedQueryParameters
    {
        [StringLength(100)]
        public string? Search { get; init; }

        [Range(1, 200)]
        public int Limit { get; init; } = 50;

        [Range(0, int.MaxValue)]
        public int Offset { get; init; }
    }

    public sealed class DoctorQueryParameters : PagedQueryParameters
    {
    }

    public sealed class PatientQueryParameters : PagedQueryParameters
    {
        [StringLength(64)]
        public string? DoctorId { get; init; }
    }

    public sealed class AppointmentQueryParameters : PagedQueryParameters
    {
        [StringLength(64)]
        public string? DoctorId { get; init; }

        [StringLength(64)]
        public string? PatientId { get; init; }

        public DateOnly? FromDate { get; init; }

        public DateOnly? ToDate { get; init; }
    }

    public sealed class TreatmentQueryParameters : PagedQueryParameters
    {
    }

    public sealed class AuditQueryParameters
    {
        [StringLength(64)]
        public string? EntityType { get; init; }

        [StringLength(128)]
        public string? EntityId { get; init; }

        [StringLength(100)]
        public string? Actor { get; init; }

        [EnumDataType(typeof(AuditOperation))]
        public AuditOperation? Operation { get; init; }

        public DateTimeOffset? FromUtc { get; init; }

        public DateTimeOffset? ToUtc { get; init; }

        [Range(1, 200)]
        public int Limit { get; init; } = 100;
    }

    public sealed class DashboardQueryParameters
    {
        [Range(1, 50)]
        public int UpcomingAppointmentLimit { get; init; } = 10;

        [Range(1, 50)]
        public int RecentAuditLimit { get; init; } = 10;

        [Range(1, 50)]
        public int BreakdownLimit { get; init; } = 20;
    }
}
