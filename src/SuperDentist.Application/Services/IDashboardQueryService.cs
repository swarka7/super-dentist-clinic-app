using SuperDentist.Application.Queries;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Application.Services
{
    public interface IDashboardQueryService
    {
        Task<DashboardSummary> GetSummaryAsync(
            DashboardQuery query,
            CancellationToken cancellationToken = default);
    }
}
