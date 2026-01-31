using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Core.Repositories
{
    public interface IDoctorRepository
    {
        Task<IReadOnlyList<Doctor>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Doctor?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
        Task AddAsync(Doctor doctor, CancellationToken cancellationToken = default);
        Task UpdateAsync(Doctor doctor, CancellationToken cancellationToken = default);
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}


