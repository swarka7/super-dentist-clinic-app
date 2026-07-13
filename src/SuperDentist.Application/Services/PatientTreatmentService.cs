using SuperDentist.Core;
using SuperDentist.Core.Repositories;
using SuperDentist.Core.Results;
using SuperDentist.Core.Services;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Application.Services
{
    public sealed class PatientTreatmentService : IPatientTreatmentService
    {
        private readonly IPatientTreatmentRepository _repository;

        public PatientTreatmentService(IPatientTreatmentRepository repository)
        {
            _repository = repository;
        }

        public Task<IReadOnlyList<PatientTreatment>> GetAllAsync(CancellationToken cancellationToken = default) =>
            _repository.GetAllAsync(cancellationToken);

        public Task<PatientTreatment?> GetByPatientIdAsync(string patientId, CancellationToken cancellationToken = default) =>
            _repository.GetByPatientIdAsync(patientId, cancellationToken);

        public async Task<OperationResult> AddAsync(PatientTreatment patientTreatment, CancellationToken cancellationToken = default)
        {
            if (await _repository.ExistsAsync(patientTreatment.PatientId, cancellationToken).ConfigureAwait(false))
            {
                return OperationResult.Fail("This patient already has a treatment record.");
            }

            await _repository.AddAsync(patientTreatment, cancellationToken).ConfigureAwait(false);
            return OperationResult.Ok();
        }

        public async Task<OperationResult> UpdateAsync(PatientTreatment patientTreatment, CancellationToken cancellationToken = default)
        {
            if (!await _repository.ExistsAsync(patientTreatment.PatientId, cancellationToken).ConfigureAwait(false))
            {
                return OperationResult.Fail("Patient treatment record not found.");
            }

            await _repository.UpdateAsync(patientTreatment, cancellationToken).ConfigureAwait(false);
            return OperationResult.Ok();
        }

        public async Task<OperationResult> DeleteAsync(string patientId, CancellationToken cancellationToken = default)
        {
            if (!await _repository.ExistsAsync(patientId, cancellationToken).ConfigureAwait(false))
            {
                return OperationResult.Fail("Patient treatment record not found.");
            }

            await _repository.DeleteAsync(patientId, cancellationToken).ConfigureAwait(false);
            return OperationResult.Ok();
        }
    }
}
