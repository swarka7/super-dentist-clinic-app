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
        private readonly IAuditService _auditService;
        private readonly IApplicationTransaction _transaction;

        public PatientTreatmentService(
            IPatientTreatmentRepository repository,
            IAuditService auditService,
            IApplicationTransaction transaction)
        {
            _repository = repository;
            _auditService = auditService;
            _transaction = transaction;
        }

        public Task<IReadOnlyList<PatientTreatment>> GetAllAsync(CancellationToken cancellationToken = default) =>
            _repository.GetAllAsync(cancellationToken);

        public Task<PatientTreatment?> GetByPatientIdAsync(string patientId, CancellationToken cancellationToken = default) =>
            _repository.GetByPatientIdAsync(patientId, cancellationToken);

        public Task<OperationResult> AddAsync(PatientTreatment patientTreatment, CancellationToken cancellationToken = default)
        {
            return _transaction.ExecuteAsync(async transactionCancellationToken =>
            {
                if (await _repository.ExistsAsync(patientTreatment.PatientId, transactionCancellationToken).ConfigureAwait(false))
                {
                    return OperationResult.Fail("This patient already has a treatment record.");
                }

                await _repository.AddAsync(patientTreatment, transactionCancellationToken).ConfigureAwait(false);
                PatientTreatment persisted = await _repository.GetByPatientIdAsync(
                    patientTreatment.PatientId,
                    transactionCancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Created patient treatment could not be reloaded.");
                await _auditService.RecordAsync(
                    AuditEntityTypes.PatientTreatment,
                    patientTreatment.PatientId,
                    AuditOperation.Created,
                    null,
                    AuditSnapshots.PatientTreatment(persisted),
                    cancellationToken: transactionCancellationToken).ConfigureAwait(false);
                return OperationResult.Ok();
            }, cancellationToken);
        }

        public Task<OperationResult> UpdateAsync(PatientTreatment patientTreatment, CancellationToken cancellationToken = default)
        {
            return _transaction.ExecuteAsync(async transactionCancellationToken =>
            {
                PatientTreatment? existing = await _repository.GetByPatientIdAsync(
                    patientTreatment.PatientId,
                    transactionCancellationToken).ConfigureAwait(false);
                if (existing == null)
                {
                    return OperationResult.Fail("Patient treatment record not found.");
                }

                await _repository.UpdateAsync(patientTreatment, transactionCancellationToken).ConfigureAwait(false);
                PatientTreatment persisted = await _repository.GetByPatientIdAsync(
                    patientTreatment.PatientId,
                    transactionCancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Updated patient treatment could not be reloaded.");
                await _auditService.RecordAsync(
                    AuditEntityTypes.PatientTreatment,
                    patientTreatment.PatientId,
                    AuditOperation.Updated,
                    AuditSnapshots.PatientTreatment(existing),
                    AuditSnapshots.PatientTreatment(persisted),
                    cancellationToken: transactionCancellationToken).ConfigureAwait(false);
                return OperationResult.Ok();
            }, cancellationToken);
        }

        public Task<OperationResult> DeleteAsync(string patientId, CancellationToken cancellationToken = default)
        {
            return _transaction.ExecuteAsync(async transactionCancellationToken =>
            {
                PatientTreatment? existing = await _repository.GetByPatientIdAsync(
                    patientId,
                    transactionCancellationToken).ConfigureAwait(false);
                if (existing == null)
                {
                    return OperationResult.Fail("Patient treatment record not found.");
                }

                await _repository.DeleteAsync(patientId, transactionCancellationToken).ConfigureAwait(false);
                await _auditService.RecordAsync(
                    AuditEntityTypes.PatientTreatment,
                    patientId,
                    AuditOperation.Deleted,
                    AuditSnapshots.PatientTreatment(existing),
                    null,
                    cancellationToken: transactionCancellationToken).ConfigureAwait(false);
                return OperationResult.Ok();
            }, cancellationToken);
        }
    }
}