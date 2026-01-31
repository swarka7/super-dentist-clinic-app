using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SuperDentist.Core.Results;

namespace SuperDentist.Core.Services
{
    public interface IPatientService
    {
        Task<IReadOnlyList<Patient>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Patient?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<OperationResult> AddAsync(Patient patient, CancellationToken cancellationToken = default);
        Task<OperationResult> UpdateAsync(Patient patient, CancellationToken cancellationToken = default);
        Task<OperationResult> DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}
