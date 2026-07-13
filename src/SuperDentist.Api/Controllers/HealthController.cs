using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SuperDentist.Api.Contracts;
using SuperDentist.Api.OpenApi;

namespace SuperDentist.Api.Controllers
{
    [ApiController]
    [Route("health")]
    public sealed class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthCheckService;

        public HealthController(HealthCheckService healthCheckService)
        {
            _healthCheckService = healthCheckService;
        }

        /// <summary>Reports API and SQLite availability.</summary>
        [HttpGet]
        [ApiOperation("Report API and SQLite availability.")]
        [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<HealthResponse>> Get(CancellationToken cancellationToken)
        {
            HealthReport report = await _healthCheckService
                .CheckHealthAsync(cancellationToken)
                .ConfigureAwait(false);
            IReadOnlyList<HealthCheckResponse> checks = report.Entries
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => new HealthCheckResponse(
                    entry.Key,
                    entry.Value.Status.ToString(),
                    entry.Value.Duration.TotalMilliseconds))
                .ToList();
            var response = new HealthResponse(report.Status.ToString(), checks);

            return report.Status == HealthStatus.Healthy
                ? Ok(response)
                : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
        }
    }
}
