using Microsoft.AspNetCore.Mvc;
using SuperDentist.Api.Contracts;
using SuperDentist.Api.Mapping;
using SuperDentist.Api.OpenApi;
using SuperDentist.Core;
using SuperDentist.Core.Services;

namespace SuperDentist.Api.Controllers
{
    [ApiController]
    [Route("api/audit")]
    public sealed class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;

        public AuditController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        /// <summary>Returns newest-first audit entries using combinable bounded filters.</summary>
        [HttpGet]
        [ApiOperation("List newest audit entries using combinable bounded filters.")]
        [ProducesResponseType(typeof(BoundedResponse<AuditResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BoundedResponse<AuditResponse>>> GetAll(
            [FromQuery] AuditQueryParameters parameters,
            CancellationToken cancellationToken)
        {
            if (parameters.FromUtc > parameters.ToUtc)
            {
                ModelState.AddModelError(
                    nameof(parameters.FromUtc),
                    "FromUtc must be on or before ToUtc.");
                return ValidationProblem(ModelState);
            }

            if (parameters.Operation.HasValue
                && !Enum.IsDefined(typeof(AuditOperation), parameters.Operation.Value))
            {
                ModelState.AddModelError(nameof(parameters.Operation), "Operation is not supported.");
                return ValidationProblem(ModelState);
            }

            var query = new AuditQuery
            {
                EntityType = parameters.EntityType,
                EntityId = parameters.EntityId,
                Actor = parameters.Actor,
                Operation = parameters.Operation,
                FromUtc = parameters.FromUtc?.UtcDateTime,
                ToUtc = parameters.ToUtc?.UtcDateTime,
                Limit = parameters.Limit
            };

            IReadOnlyList<AuditEntry> entries = await _auditService
                .SearchAsync(query, cancellationToken)
                .ConfigureAwait(false);
            IReadOnlyList<AuditResponse> response = entries.Select(entry => entry.ToResponse()).ToList();
            return Ok(new BoundedResponse<AuditResponse>(response, response.Count, parameters.Limit));
        }
    }
}
