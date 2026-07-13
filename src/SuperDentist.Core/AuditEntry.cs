using System;

namespace SuperDentist.Core
{
    public sealed class AuditEntry
    {
        public long Id { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public AuditOperation Operation { get; set; }
        public string Actor { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
    }
}
