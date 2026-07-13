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

        public AppointmentService(IAppointmentRepository repository)
        {
            _repository = repository;
        }

        public Task<IReadOnlyList<Appointment>> GetAllAsync(CancellationToken cancellationToken = default) =>
            _repository.GetAllAsync(cancellationToken);

        public Task<Appointment?> GetByPatientIdAsync(string patientId, CancellationToken cancellationToken = default) =>
            _repository.GetByPatientIdAsync(patientId, cancellationToken);

        public Task<IReadOnlyList<Appointment>> GetByDateAsync(string date, CancellationToken cancellationToken = default) =>
            _repository.GetByDateAsync(date, cancellationToken);

        public async Task<OperationResult> AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
        {
            if (await _repository.ExistsAsync(appointment.PatientId, cancellationToken).ConfigureAwait(false))
            {
                return OperationResult.Fail("This patient already has an appointment.");
            }

            if (await _repository.SlotExistsAsync(appointment.DoctorId, appointment.Date, appointment.Time, cancellationToken).ConfigureAwait(false))
            {
                return OperationResult.Fail("This time slot is already booked for the selected doctor.");
            }

            await _repository.AddAsync(appointment, cancellationToken).ConfigureAwait(false);
            return OperationResult.Ok();
        }

        public async Task<OperationResult> UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default)
        {
            if (!await _repository.ExistsAsync(appointment.PatientId, cancellationToken).ConfigureAwait(false))
            {
                return OperationResult.Fail("Appointment not found.");
            }

            if (await _repository.SlotExistsAsync(appointment.DoctorId, appointment.Date, appointment.Time, cancellationToken).ConfigureAwait(false))
            {
                Appointment? existing = await _repository.GetByPatientIdAsync(appointment.PatientId, cancellationToken).ConfigureAwait(false);
                if (existing == null || existing.DoctorId != appointment.DoctorId || existing.Date != appointment.Date || existing.Time != appointment.Time)
                {
                    return OperationResult.Fail("This time slot is already booked for the selected doctor.");
                }
            }

            await _repository.UpdateAsync(appointment, cancellationToken).ConfigureAwait(false);
            return OperationResult.Ok();
        }

        public async Task<OperationResult> DeleteAsync(string patientId, CancellationToken cancellationToken = default)
        {
            if (!await _repository.ExistsAsync(patientId, cancellationToken).ConfigureAwait(false))
            {
                return OperationResult.Fail("Appointment not found.");
            }

            await _repository.DeleteAsync(patientId, cancellationToken).ConfigureAwait(false);
            return OperationResult.Ok();
        }
    }
}
