using System;

namespace SuperDentist.Core
{
    public sealed class AuditQuery
    {
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? Actor { get; set; }
        public AuditOperation? Operation { get; set; }
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
        public int Limit { get; set; } = 200;
    }
}
