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
    [Route("api/patients")]
    public sealed class PatientsController : ControllerBase
    {
        private readonly IClinicQueryService _queryService;
        private readonly IPatientService _patientService;

        public PatientsController(IClinicQueryService queryService, IPatientService patientService)
        {
            _queryService = queryService;
            _patientService = patientService;
        }

        /// <summary>Returns a bounded page of patients with optional text and doctor filters.</summary>
        [HttpGet]
        [ApiOperation("List patients with bounded search, doctor filtering, and pagination.")]
        [ProducesResponseType(typeof(PagedResponse<PatientResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResponse<PatientResponse>>> GetAll(
            [FromQuery] PatientQueryParameters parameters,
            CancellationToken cancellationToken)
        {
            var query = new PatientListQuery(
                parameters.Search,
                parameters.DoctorId,
                parameters.Limit,
                parameters.Offset);
            PagedResult<Patient> result = await _queryService
                .GetPatientsAsync(query, cancellationToken)
                .ConfigureAwait(false);
            return Ok(result.ToResponse());
        }

        /// <summary>Returns one patient by clinic identifier.</summary>
        [HttpGet("{id}")]
        [ApiOperation("Get one patient by clinic identifier.")]
        [ProducesResponseType(typeof(PatientResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PatientResponse>> GetById(
            string id,
            CancellationToken cancellationToken)
        {
            Patient? patient = await _patientService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            return patient == null ? NotFound() : Ok(patient.ToResponse());
        }
    }
}
