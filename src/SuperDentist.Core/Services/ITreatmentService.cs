using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SuperDentist.Core.Results;

namespace SuperDentist.Core.Services
{
    public interface ITreatmentService
    {
        Task<IReadOnlyList<Treatment>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Treatment?> GetByNumberAsync(string number, CancellationToken cancellationToken = default);
        Task<OperationResult> AddAsync(Treatment treatment, CancellationToken cancellationToken = default);
        Task<OperationResult> UpdateAsync(Treatment treatment, CancellationToken cancellationToken = default);
        Task<OperationResult> DeleteAsync(string number, CancellationToken cancellationToken = default);
    }
}
