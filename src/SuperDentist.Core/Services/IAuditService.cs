using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Core.Services
{
    public interface IAuditService
    {
        Task RecordAsync(
            string entityType,
            string entityId,
            AuditOperation operation,
            IReadOnlyDictionary<string, object?>? oldValues,
            IReadOnlyDictionary<string, object?>? newValues,
            string? correlationId = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AuditEntry>> SearchAsync(AuditQuery query, CancellationToken cancellationToken = default);
    }
}
