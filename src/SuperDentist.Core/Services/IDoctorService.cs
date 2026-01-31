using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SuperDentist.Core.Results;

namespace SuperDentist.Core.Services
{
    public interface IDoctorService
    {
        Task<IReadOnlyList<Doctor>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Doctor?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<OperationResult> AddAsync(Doctor doctor, CancellationToken cancellationToken = default);
        Task<OperationResult> UpdateAsync(Doctor doctor, CancellationToken cancellationToken = default);
        Task<OperationResult> DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}
