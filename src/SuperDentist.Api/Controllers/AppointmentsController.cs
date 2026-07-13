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
    [Route("api/appointments")]
    public sealed class AppointmentsController : ControllerBase
    {
        private readonly IClinicQueryService _queryService;

        public AppointmentsController(IClinicQueryService queryService)
        {
            _queryService = queryService;
        }

        /// <summary>Returns bounded appointments filtered by clinic identifiers or date range.</summary>
        [HttpGet]
        [ApiOperation("List appointments with bounded clinic and date filters.")]
        [ProducesResponseType(typeof(PagedResponse<AppointmentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResponse<AppointmentResponse>>> GetAll(
            [FromQuery] AppointmentQueryParameters parameters,
            CancellationToken cancellationToken)
        {
            if (parameters.FromDate > parameters.ToDate)
            {
                ModelState.AddModelError(
                    nameof(parameters.FromDate),
                    "FromDate must be on or before ToDate.");
                return ValidationProblem(ModelState);
            }

            var query = new AppointmentListQuery(
                parameters.Search,
                parameters.DoctorId,
                parameters.PatientId,
                parameters.FromDate,
                parameters.ToDate,
                parameters.Limit,
                parameters.Offset);
            PagedResult<Appointment> result = await _queryService
                .GetAppointmentsAsync(query, cancellationToken)
                .ConfigureAwait(false);
            return Ok(result.ToResponse());
        }
    }
}
