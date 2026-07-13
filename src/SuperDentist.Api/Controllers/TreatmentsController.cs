using Microsoft.AspNetCore.Mvc;
using SuperDentist.Api.Contracts;
using SuperDentist.Api.Mapping;
using SuperDentist.Api.OpenApi;
using SuperDentist.Application.Queries;
using SuperDentist.Application.Services;
using SuperDentist.Core;

namespace SuperDentist.Api.Controllers
{
    [ApiController]
    [Route("api/treatments")]
    public sealed class TreatmentsController : ControllerBase
    {
        private readonly IClinicQueryService _queryService;

        public TreatmentsController(IClinicQueryService queryService)
        {
            _queryService = queryService;
        }

        /// <summary>Returns a bounded, searchable page of treatment catalog entries.</summary>
        [HttpGet]
        [ApiOperation("List treatment catalog entries with bounded search and pagination.")]
        [ProducesResponseType(typeof(PagedResponse<TreatmentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResponse<TreatmentResponse>>> GetAll(
            [FromQuery] TreatmentQueryParameters parameters,
            CancellationToken cancellationToken)
        {
            var query = new TreatmentListQuery(parameters.Search, parameters.Limit, parameters.Offset);
            PagedResult<Treatment> result = await _queryService
                .GetTreatmentsAsync(query, cancellationToken)
                .ConfigureAwait(false);
            return Ok(result.ToResponse());
        }
    }
}
