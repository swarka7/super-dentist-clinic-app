using Microsoft.AspNetCore.Mvc;
using SuperDentist.Api.Contracts;
using SuperDentist.Api.Mapping;
using SuperDentist.Api.OpenApi;
using SuperDentist.Application.Queries;
using SuperDentist.Application.Services;

namespace SuperDentist.Api.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public sealed class DashboardController : ControllerBase
    {
        private readonly IDashboardQueryService _dashboardService;

        public DashboardController(IDashboardQueryService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>Returns bounded operational metrics and recent clinic activity.</summary>
        [HttpGet("summary")]
        [ApiOperation("Get bounded operational metrics and recent clinic activity.")]
        [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DashboardResponse>> GetSummary(
            [FromQuery] DashboardQueryParameters parameters,
            CancellationToken cancellationToken)
        {
            var query = new DashboardQuery(
                parameters.UpcomingAppointmentLimit,
                parameters.RecentAuditLimit,
                parameters.BreakdownLimit);
            DashboardSummary result = await _dashboardService
                .GetSummaryAsync(query, cancellationToken)
                .ConfigureAwait(false);
            return Ok(result.ToResponse());
        }
    }
}
