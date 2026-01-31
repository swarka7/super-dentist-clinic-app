using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Core.Repositories
{
    public interface IAppointmentRepository
    {
        Task<IReadOnlyList<Appointment>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Appointment?> GetByPatientIdAsync(string patientId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Appointment>> GetByDateAsync(string date, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string patientId, CancellationToken cancellationToken = default);
        Task<bool> SlotExistsAsync(string doctorId, string date, string time, CancellationToken cancellationToken = default);
        Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);
        Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default);
        Task DeleteAsync(string patientId, CancellationToken cancellationToken = default);
    }
}
