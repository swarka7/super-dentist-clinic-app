using SuperDentist.Core;
using SuperDentist.Core.Repositories;
using SuperDentist.Core.Results;
using SuperDentist.Core.Services;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Application.Services
{
    public sealed class PatientService : IPatientService
    {
        private readonly IPatientRepository _repository;
        private readonly IAuditService _auditService;
        private readonly IApplicationTransaction _transaction;

        public PatientService(
            IPatientRepository repository,
            IAuditService auditService,
            IApplicationTransaction transaction)
        {
            _repository = repository;
            _auditService = auditService;
            _transaction = transaction;
        }

        public Task<IReadOnlyList<Patient>> GetAllAsync(CancellationToken cancellationToken = default) =>
            _repository.GetAllAsync(cancellationToken);

        public Task<Patient?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            _repository.GetByIdAsync(id, cancellationToken);

        public Task<OperationResult> AddAsync(Patient patient, CancellationToken cancellationToken = default)
        {
            return _transaction.ExecuteAsync(async transactionCancellationToken =>
            {
                if (await _repository.ExistsAsync(patient.Id, transactionCancellationToken).ConfigureAwait(false))
                {
                    return OperationResult.Fail("A patient with this ID already exists.");
                }

                await _repository.AddAsync(patient, transactionCancellationToken).ConfigureAwait(false);
                Patient persisted = await _repository.GetByIdAsync(
                    patient.Id,
                    transactionCancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Created patient could not be reloaded.");
                await _auditService.RecordAsync(
                    AuditEntityTypes.Patient,
                    patient.Id,
                    AuditOperation.Created,
                    null,
                    AuditSnapshots.Patient(persisted),
                    cancellationToken: transactionCancellationToken).ConfigureAwait(false);
                return OperationResult.Ok();
            }, cancellationToken);
        }

        public Task<OperationResult> UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
        {
            return _transaction.ExecuteAsync(async transactionCancellationToken =>
            {
                Patient? existing = await _repository.GetByIdAsync(patient.Id, transactionCancellationToken).ConfigureAwait(false);
                if (existing == null)
                {
                    return OperationResult.Fail("Patient not found.");
                }

                await _repository.UpdateAsync(patient, transactionCancellationToken).ConfigureAwait(false);
                Patient persisted = await _repository.GetByIdAsync(
                    patient.Id,
                    transactionCancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Updated patient could not be reloaded.");
                await _auditService.RecordAsync(
                    AuditEntityTypes.Patient,
                    patient.Id,
                    AuditOperation.Updated,
                    AuditSnapshots.Patient(existing),
                    AuditSnapshots.Patient(persisted),
                    cancellationToken: transactionCancellationToken).ConfigureAwait(false);
                return OperationResult.Ok();
            }, cancellationToken);
        }

        public Task<OperationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            return _transaction.ExecuteAsync(async transactionCancellationToken =>
            {
                Patient? existing = await _repository.GetByIdAsync(id, transactionCancellationToken).ConfigureAwait(false);
                if (existing == null)
                {
                    return OperationResult.Fail("Patient not found.");
                }

                await _repository.DeleteAsync(id, transactionCancellationToken).ConfigureAwait(false);
                await _auditService.RecordAsync(
                    AuditEntityTypes.Patient,
                    id,
                    AuditOperation.Deleted,
                    AuditSnapshots.Patient(existing),
                    null,
                    cancellationToken: transactionCancellationToken).ConfigureAwait(false);
                return OperationResult.Ok();
            }, cancellationToken);
        }
    }
}