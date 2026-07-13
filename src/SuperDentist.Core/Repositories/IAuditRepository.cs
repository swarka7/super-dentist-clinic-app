using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Core.Repositories
{
    public interface IAuditRepository
    {
        Task AddAsync(AuditEntry entry, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AuditEntry>> SearchAsync(AuditQuery query, CancellationToken cancellationToken = default);
    }
}
