using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Core.Repositories
{
    public interface ITreatmentRepository
    {
        Task<IReadOnlyList<Treatment>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Treatment?> GetByNumberAsync(string number, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string number, CancellationToken cancellationToken = default);
        Task AddAsync(Treatment treatment, CancellationToken cancellationToken = default);
        Task UpdateAsync(Treatment treatment, CancellationToken cancellationToken = default);
        Task DeleteAsync(string number, CancellationToken cancellationToken = default);
    }
}


