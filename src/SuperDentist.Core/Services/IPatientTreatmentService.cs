using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SuperDentist.Core.Results;

namespace SuperDentist.Core.Services
{
    public interface IPatientTreatmentService
    {
        Task<IReadOnlyList<PatientTreatment>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<PatientTreatment?> GetByPatientIdAsync(string patientId, CancellationToken cancellationToken = default);
        Task<OperationResult> AddAsync(PatientTreatment patientTreatment, CancellationToken cancellationToken = default);
        Task<OperationResult> UpdateAsync(PatientTreatment patientTreatment, CancellationToken cancellationToken = default);
        Task<OperationResult> DeleteAsync(string patientId, CancellationToken cancellationToken = default);
    }
}
