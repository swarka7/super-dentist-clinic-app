using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SuperDentist.Core.Results;

namespace SuperDentist.Core.Services
{
    public interface IAppointmentService
    {
        Task<IReadOnlyList<Appointment>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Appointment?> GetByPatientIdAsync(string patientId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Appointment>> GetByDateAsync(string date, CancellationToken cancellationToken = default);
        Task<OperationResult> AddAsync(Appointment appointment, CancellationToken cancellationToken = default);
        Task<OperationResult> UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default);
        Task<OperationResult> DeleteAsync(string patientId, CancellationToken cancellationToken = default);
    }
}
