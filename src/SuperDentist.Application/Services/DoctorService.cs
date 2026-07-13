using SuperDentist.Core;
using SuperDentist.Core.Repositories;
using SuperDentist.Core.Results;
using SuperDentist.Core.Services;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Application.Services
{
    public sealed class DoctorService : IDoctorService
    {
        private readonly IDoctorRepository _repository;
        private readonly IAuditService _auditService;
        private readonly IApplicationTransaction _transaction;

        public DoctorService(
            IDoctorRepository repository,
            IAuditService auditService,
            IApplicationTransaction transaction)
        {
            _repository = repository;
            _auditService = auditService;
            _transaction = transaction;
        }

        public Task<IReadOnlyList<Doctor>> GetAllAsync(CancellationToken cancellationToken = default) =>
            _repository.GetAllAsync(cancellationToken);

        public Task<Doctor?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            _repository.GetByIdAsync(id, cancellationToken);

        public Task<OperationResult> AddAsync(Doctor doctor, CancellationToken cancellationToken = default)
        {
            return _transaction.ExecuteAsync(async transactionCancellationToken =>
            {
                if (await _repository.ExistsAsync(doctor.Id, transactionCancellationToken).ConfigureAwait(false))
                {
                    return OperationResult.Fail("A doctor with this ID already exists.");
                }

                await _repository.AddAsync(doctor, transactionCancellationToken).ConfigureAwait(false);
                Doctor persisted = await _repository.GetByIdAsync(
                    doctor.Id,
                    transactionCancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Created doctor could not be reloaded.");
                await _auditService.RecordAsync(
                    AuditEntityTypes.Doctor,
                    doctor.Id,
                    AuditOperation.Created,
                    null,
                    AuditSnapshots.Doctor(persisted),
                    cancellationToken: transactionCancellationToken).ConfigureAwait(false);
                return OperationResult.Ok();
            }, cancellationToken);
        }

        public Task<OperationResult> UpdateAsync(Doctor doctor, CancellationToken cancellationToken = default)
        {
            return _transaction.ExecuteAsync(async transactionCancellationToken =>
            {
                Doctor? existing = await _repository.GetByIdAsync(doctor.Id, transactionCancellationToken).ConfigureAwait(false);
                if (existing == null)
                {
                    return OperationResult.Fail("Doctor not found.");
                }

                await _repository.UpdateAsync(doctor, transactionCancellationToken).ConfigureAwait(false);
                Doctor persisted = await _repository.GetByIdAsync(
                    doctor.Id,
                    transactionCancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Updated doctor could not be reloaded.");
                await _auditService.RecordAsync(
                    AuditEntityTypes.Doctor,
                    doctor.Id,
                    AuditOperation.Updated,
                    AuditSnapshots.Doctor(existing),
                    AuditSnapshots.Doctor(persisted),
                    cancellationToken: transactionCancellationToken).ConfigureAwait(false);
                return OperationResult.Ok();
            }, cancellationToken);
        }

        public Task<OperationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            return _transaction.ExecuteAsync(async transactionCancellationToken =>
            {
                Doctor? existing = await _repository.GetByIdAsync(id, transactionCancellationToken).ConfigureAwait(false);
                if (existing == null)
                {
                    return OperationResult.Fail("Doctor not found.");
                }

                await _repository.DeleteAsync(id, transactionCancellationToken).ConfigureAwait(false);
                await _auditService.RecordAsync(
                    AuditEntityTypes.Doctor,
                    id,
                    AuditOperation.Deleted,
                    AuditSnapshots.Doctor(existing),
                    null,
                    cancellationToken: transactionCancellationToken).ConfigureAwait(false);
                return OperationResult.Ok();
            }, cancellationToken);
        }
    }
}