using SuperDentist.Core;
using SuperDentist.Core.Repositories;
using SuperDentist.Core.Results;
using SuperDentist.Core.Services;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Infrastructure.Services
{
    public sealed class TreatmentService : ITreatmentService
    {
        private readonly ITreatmentRepository _repository;

        public TreatmentService(ITreatmentRepository repository)
        {
            _repository = repository;
        }

        public Task<IReadOnlyList<Treatment>> GetAllAsync(CancellationToken cancellationToken = default) =>
            _repository.GetAllAsync(cancellationToken);

        public Task<Treatment?> GetByNumberAsync(string number, CancellationToken cancellationToken = default) =>
            _repository.GetByNumberAsync(number, cancellationToken);

        public async Task<OperationResult> AddAsync(Treatment treatment, CancellationToken cancellationToken = default)
        {
            if (await _repository.ExistsAsync(treatment.Number, cancellationToken).ConfigureAwait(false))
            {
                return OperationResult.Fail("A treatment with this number already exists.");
            }

            await _repository.AddAsync(treatment, cancellationToken).ConfigureAwait(false);
            return OperationResult.Ok();
        }

        public async Task<OperationResult> UpdateAsync(Treatment treatment, CancellationToken cancellationToken = default)
        {
            if (!await _repository.ExistsAsync(treatment.Number, cancellationToken).ConfigureAwait(false))
            {
                return OperationResult.Fail("Treatment not found.");
            }

            await _repository.UpdateAsync(treatment, cancellationToken).ConfigureAwait(false);
            return OperationResult.Ok();
        }

        public async Task<OperationResult> DeleteAsync(string number, CancellationToken cancellationToken = default)
        {
            if (!await _repository.ExistsAsync(number, cancellationToken).ConfigureAwait(false))
            {
                return OperationResult.Fail("Treatment not found.");
            }

            await _repository.DeleteAsync(number, cancellationToken).ConfigureAwait(false);
            return OperationResult.Ok();
        }
    }
}
