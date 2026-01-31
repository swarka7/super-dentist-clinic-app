using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Core.Repositories
{
    public interface IPatientTreatmentRepository
    {
        Task<IReadOnlyList<PatientTreatment>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<PatientTreatment?> GetByPatientIdAsync(string patientId, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string patientId, CancellationToken cancellationToken = default);
        Task AddAsync(PatientTreatment record, CancellationToken cancellationToken = default);
        Task UpdateAsync(PatientTreatment record, CancellationToken cancellationToken = default);
        Task DeleteAsync(string patientId, CancellationToken cancellationToken = default);
    }
}
