using SuperDentist.Application.Queries;
using SuperDentist.Core;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Application.Services
{
    public interface IClinicQueryService
    {
        Task<PagedResult<Doctor>> GetDoctorsAsync(
            DoctorListQuery query,
            CancellationToken cancellationToken = default);

        Task<PagedResult<Patient>> GetPatientsAsync(
            PatientListQuery query,
            CancellationToken cancellationToken = default);

        Task<PagedResult<Appointment>> GetAppointmentsAsync(
            AppointmentListQuery query,
            CancellationToken cancellationToken = default);

        Task<PagedResult<Treatment>> GetTreatmentsAsync(
            TreatmentListQuery query,
            CancellationToken cancellationToken = default);
    }
}
