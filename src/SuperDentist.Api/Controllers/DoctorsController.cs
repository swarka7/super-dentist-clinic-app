using Microsoft.AspNetCore.Mvc;
using SuperDentist.Api.Contracts;
using SuperDentist.Api.Mapping;
using SuperDentist.Api.OpenApi;
using SuperDentist.Application.Queries;
using SuperDentist.Application.Services;
using SuperDentist.Core;
using SuperDentist.Core.Services;

namespace SuperDentist.Api.Controllers
{
    [ApiController]
    [Route("api/doctors")]
    public sealed class DoctorsController : ControllerBase
    {
        private readonly IClinicQueryService _queryService;
        private readonly IDoctorService _doctorService;

        public DoctorsController(IClinicQueryService queryService, IDoctorService doctorService)
        {
            _queryService = queryService;
            _doctorService = doctorService;
        }

        /// <summary>Returns a bounded, searchable page of doctors.</summary>
        [HttpGet]
        [ApiOperation("List doctors with bounded search and pagination.")]
        [ProducesResponseType(typeof(PagedResponse<DoctorResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResponse<DoctorResponse>>> GetAll(
            [FromQuery] DoctorQueryParameters parameters,
            CancellationToken cancellationToken)
        {
            var query = new DoctorListQuery(parameters.Search, parameters.Limit, parameters.Offset);
            PagedResult<Doctor> result = await _queryService
                .GetDoctorsAsync(query, cancellationToken)
                .ConfigureAwait(false);
            return Ok(result.ToResponse());
        }

        /// <summary>Returns one doctor by clinic identifier.</summary>
        [HttpGet("{id}")]
        [ApiOperation("Get one doctor by clinic identifier.")]
        [ProducesResponseType(typeof(DoctorResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DoctorResponse>> GetById(
            string id,
            CancellationToken cancellationToken)
        {
            Doctor? doctor = await _doctorService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            return doctor == null ? NotFound() : Ok(doctor.ToResponse());
        }
    }
}
