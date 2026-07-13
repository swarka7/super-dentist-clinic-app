using SuperDentist.Core;
using SuperDentist.Core.Repositories;
using SuperDentist.Core.Results;
using SuperDentist.Core.Services;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Application.Services
{
    public sealed class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _repository;
        private readonly IAuditService _auditService;
        private readonly IApplicationTransaction _transaction;

        public AppointmentService(
            IAppointmentRepository repository,
            IAuditService auditService,
            IApplicationTransaction transaction)
        {
            _repository = repository;
            _auditService = auditService;
            _transaction = transaction;
        }

        public Task<IReadOnlyList<Appointment>> GetAllAsync(CancellationToken cancellationToken = default) =>
            _repository.GetAllAsync(cancellationToken);

        public Task<Appointment?> GetByPatientIdAsync(string patientId, CancellationToken cancellationToken = default) =>
            _repository.GetByPatientIdAsync(patientId, cancellationToken);

        public Task<IReadOnlyList<Appointment>> GetByDateAsync(string date, CancellationToken cancellationToken = default) =>
            _repository.GetByDateAsync(date, cancellationToken);

        public Task<OperationResult> AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
        {
            return _transaction.ExecuteAsync(async transactionCancellationToken =>
            {
                if (await _repository.ExistsAsync(appointment.PatientId, transactionCancellationToken).ConfigureAwait(false))
                {
                    return OperationResult.Fail("This patient already has an appointment.");
                }

                if (await _repository.SlotExistsAsync(
                    appointment.DoctorId,
                    appointment.Date,
                    appointment.Time,
                    transactionCancellationToken).ConfigureAwait(false))
                {
                    return OperationResult.Fail("This time slot is already booked for the selected doctor.");
                }

                await _repository.AddAsync(appointment, transactionCancellationToken).ConfigureAwait(false);
                Appointment persisted = await _repository.GetByPatientIdAsync(
                    appointment.PatientId,
                    transactionCancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Created appointment could not be reloaded.");
                await _auditService.RecordAsync(
                    AuditEntityTypes.Appointment,
                    appointment.PatientId,
                    AuditOperation.Created,
                    null,
                    AuditSnapshots.Appointment(persisted),
                    cancellationToken: transactionCancellationToken).ConfigureAwait(false);
                return OperationResult.Ok();
            }, cancellationToken);
        }

        public Task<OperationResult> UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default)
        {
            return _transaction.ExecuteAsync(async transactionCancellationToken =>
            {
                Appointment? existing = await _repository.GetByPatientIdAsync(
                    appointment.PatientId,
                    transactionCancellationToken).ConfigureAwait(false);
                if (existing == null)
                {
                    return OperationResult.Fail("Appointment not found.");
                }

                if (await _repository.SlotExistsAsync(
                    appointment.DoctorId,
                    appointment.Date,
                    appointment.Time,
                    transactionCancellationToken).ConfigureAwait(false)
                    && (existing.DoctorId != appointment.DoctorId
                        || existing.Date != appointment.Date
                        || existing.Time != appointment.Time))
                {
                    return OperationResult.Fail("This time slot is already booked for the selected doctor.");
                }

                await _repository.UpdateAsync(appointment, transactionCancellationToken).ConfigureAwait(false);
                Appointment persisted = await _repository.GetByPatientIdAsync(
                    appointment.PatientId,
                    transactionCancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Updated appointment could not be reloaded.");
                await _auditService.RecordAsync(
                    AuditEntityTypes.Appointment,
                    appointment.PatientId,
                    AuditOperation.Updated,
                    AuditSnapshots.Appointment(existing),
                    AuditSnapshots.Appointment(persisted),
                    cancellationToken: transactionCancellationToken).ConfigureAwait(false);
                return OperationResult.Ok();
            }, cancellationToken);
        }

        public Task<OperationResult> DeleteAsync(string patientId, CancellationToken cancellationToken = default)
        {
            return _transaction.ExecuteAsync(async transactionCancellationToken =>
            {
                Appointment? existing = await _repository.GetByPatientIdAsync(
                    patientId,
                    transactionCancellationToken).ConfigureAwait(false);
                if (existing == null)
                {
                    return OperationResult.Fail("Appointment not found.");
                }

                await _repository.DeleteAsync(patientId, transactionCancellationToken).ConfigureAwait(false);
                await _auditService.RecordAsync(
                    AuditEntityTypes.Appointment,
                    patientId,
                    AuditOperation.Deleted,
                    AuditSnapshots.Appointment(existing),
                    null,
                    cancellationToken: transactionCancellationToken).ConfigureAwait(false);
                return OperationResult.Ok();
            }, cancellationToken);
        }
    }
}