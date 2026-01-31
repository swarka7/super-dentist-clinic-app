using SuperDentist.Core;
using SuperDentist.Core.Repositories;
using SuperDentist.Core.Results;
using SuperDentist.Core.Services;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Infrastructure.Services
{
    public sealed class DoctorService : IDoctorService
    {
        private readonly IDoctorRepository _repository;

        public DoctorService(IDoctorRepository repository)
        {
            _repository = repository;
        }

        public Task<IReadOnlyList<Doctor>> GetAllAsync(CancellationToken cancellationToken = default) =>
            _repository.GetAllAsync(cancellationToken);

        public Task<Doctor?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            _repository.GetByIdAsync(id, cancellationToken);

        public async Task<OperationResult> AddAsync(Doctor doctor, CancellationToken cancellationToken = default)
        {
            if (await _repository.ExistsAsync(doctor.Id, cancellationToken).ConfigureAwait(false))
            {
                return OperationResult.Fail("A doctor with this ID already exists.");
            }

            await _repository.AddAsync(doctor, cancellationToken).ConfigureAwait(false);
            return OperationResult.Ok();
        }

        public async Task<OperationResult> UpdateAsync(Doctor doctor, CancellationToken cancellationToken = default)
        {
            if (!await _repository.ExistsAsync(doctor.Id, cancellationToken).ConfigureAwait(false))
            {
                return OperationResult.Fail("Doctor not found.");
            }

            await _repository.UpdateAsync(doctor, cancellationToken).ConfigureAwait(false);
            return OperationResult.Ok();
        }

        public async Task<OperationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            if (!await _repository.ExistsAsync(id, cancellationToken).ConfigureAwait(false))
            {
                return OperationResult.Fail("Doctor not found.");
            }

            await _repository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
            return OperationResult.Ok();
        }
    }
}
