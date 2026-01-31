using SuperDentist.Core;
using SuperDentist.Core.Repositories;
using SuperDentist.Core.Results;
using SuperDentist.Core.Services;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Infrastructure.Services
{
    public sealed class PatientService : IPatientService
    {
        private readonly IPatientRepository _repository;

        public PatientService(IPatientRepository repository)
        {
            _repository = repository;
        }

        public Task<IReadOnlyList<Patient>> GetAllAsync(CancellationToken cancellationToken = default) =>
            _repository.GetAllAsync(cancellationToken);

        public Task<Patient?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            _repository.GetByIdAsync(id, cancellationToken);

        public async Task<OperationResult> AddAsync(Patient patient, CancellationToken cancellationToken = default)
        {
            if (await _repository.ExistsAsync(patient.Id, cancellationToken).ConfigureAwait(false))
            {
                return OperationResult.Fail("A patient with this ID already exists.");
            }

            await _repository.AddAsync(patient, cancellationToken).ConfigureAwait(false);
            return OperationResult.Ok();
        }

        public async Task<OperationResult> UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
        {
            if (!await _repository.ExistsAsync(patient.Id, cancellationToken).ConfigureAwait(false))
            {
                return OperationResult.Fail("Patient not found.");
            }

            await _repository.UpdateAsync(patient, cancellationToken).ConfigureAwait(false);
            return OperationResult.Ok();
        }

        public async Task<OperationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            if (!await _repository.ExistsAsync(id, cancellationToken).ConfigureAwait(false))
            {
                return OperationResult.Fail("Patient not found.");
            }

            await _repository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            return OperationResult.Ok();
        }
    }
}
