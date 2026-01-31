using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Core.Repositories
{
    public interface IPatientRepository
    {
        Task<IReadOnlyList<Patient>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Patient?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
        Task AddAsync(Patient patient, CancellationToken cancellationToken = default);
        Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default);
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}


