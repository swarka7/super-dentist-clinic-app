using SuperDentist.Core;
using SuperDentist.Core.Repositories;
using SuperDentist.Core.Results;
using SuperDentist.Core.Services;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Application.Services
{
    public sealed class TreatmentService : ITreatmentService
    {
        private readonly ITreatmentRepository _repository;
        private readonly IAuditService _auditService;
        private readonly IApplicationTransaction _transaction;

        public TreatmentService(
            ITreatmentRepository repository,
            IAuditService auditService,
            IApplicationTransaction transaction)
        {
            _repository = repository;
            _auditService = auditService;
            _transaction = transaction;
        }

        public Task<IReadOnlyList<Treatment>> GetAllAsync(CancellationToken cancellationToken = default) =>
            _repository.GetAllAsync(cancellationToken);

        public Task<Treatment?> GetByNumberAsync(string number, CancellationToken cancellationToken = default) =>
            _repository.GetByNumberAsync(number, cancellationToken);

        public Task<OperationResult> AddAsync(Treatment treatment, CancellationToken cancellationToken = default)
        {
            return _transaction.ExecuteAsync(async transactionCancellationToken =>
            {
                if (await _repository.ExistsAsync(treatment.Number, transactionCancellationToken).ConfigureAwait(false))
                {
                    return OperationResult.Fail("A treatment with this number already exists.");
                }

                await _repository.AddAsync(treatment, transactionCancellationToken).ConfigureAwait(false);
                Treatment persisted = await _repository.GetByNumberAsync(
                    treatment.Number,
                    transactionCancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Created treatment could not be reloaded.");
                await _auditService.RecordAsync(
                    AuditEntityTypes.Treatment,
                    treatment.Number,
                    AuditOperation.Created,
                    null,
                    AuditSnapshots.Treatment(persisted),
                    cancellationToken: transactionCancellationToken).ConfigureAwait(false);
                return OperationResult.Ok();
            }, cancellationToken);
        }

        public Task<OperationResult> UpdateAsync(Treatment treatment, CancellationToken cancellationToken = default)
        {
            return _transaction.ExecuteAsync(async transactionCancellationToken =>
            {
                Treatment? existing = await _repository.GetByNumberAsync(treatment.Number, transactionCancellationToken).ConfigureAwait(false);
                if (existing == null)
                {
                    return OperationResult.Fail("Treatment not found.");
                }

                await _repository.UpdateAsync(treatment, transactionCancellationToken).ConfigureAwait(false);
                Treatment persisted = await _repository.GetByNumberAsync(
                    treatment.Number,
                    transactionCancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Updated treatment could not be reloaded.");
                await _auditService.RecordAsync(
                    AuditEntityTypes.Treatment,
                    treatment.Number,
                    AuditOperation.Updated,
                    AuditSnapshots.Treatment(existing),
                    AuditSnapshots.Treatment(persisted),
                    cancellationToken: transactionCancellationToken).ConfigureAwait(false);
                return OperationResult.Ok();
            }, cancellationToken);
        }

        public Task<OperationResult> DeleteAsync(string number, CancellationToken cancellationToken = default)
        {
            return _transaction.ExecuteAsync(async transactionCancellationToken =>
            {
                Treatment? existing = await _repository.GetByNumberAsync(number, transactionCancellationToken).ConfigureAwait(false);
                if (existing == null)
                {
                    return OperationResult.Fail("Treatment not found.");
                }

                await _repository.DeleteAsync(number, transactionCancellationToken).ConfigureAwait(false);
                await _auditService.RecordAsync(
                    AuditEntityTypes.Treatment,
                    number,
                    AuditOperation.Deleted,
                    AuditSnapshots.Treatment(existing),
                    null,
                    cancellationToken: transactionCancellationToken).ConfigureAwait(false);
                return OperationResult.Ok();
            }, cancellationToken);
        }
    }
}