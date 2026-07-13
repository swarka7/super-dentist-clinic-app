using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SuperDentist.Api.Contracts;
using SuperDentist.Application.Queries;
using SuperDentist.Application.Services;
using SuperDentist.Core;
using SuperDentist.Core.Repositories;
using SuperDentist.Core.Services;
using SuperDentist.Infrastructure.Data;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SuperDentist.Tests
{
    public sealed class ApiIntegrationTests
    {
        private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

        [Fact]
        public async Task Startup_RegistersApplicationInfrastructureAndSwagger()
        {
            using var factory = new ApiTestApplicationFactory(useProductionInitializer: true);
            using HttpClient client = factory.CreateClient();

            Assert.NotNull(factory.Services.GetService<IDashboardQueryService>());
            Assert.NotNull(factory.Services.GetService<IClinicQueryService>());
            Assert.NotNull(factory.Services.GetService<IDoctorRepository>());
            Assert.IsType<SqliteDatabaseInitializer>(
                factory.Services.GetRequiredService<IDatabaseInitializer>());

            using HttpResponseMessage response = await client.GetAsync("/swagger/v1/swagger.json");
            string swagger = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();
            Assert.Contains("Super Dentist Reporting API", swagger, StringComparison.Ordinal);
            Assert.Contains("List doctors with bounded search and pagination.", swagger, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Health_WhenDatabaseIsAvailable_ReturnsHealthy()
        {
            using var factory = new ApiTestApplicationFactory();
            using HttpClient client = factory.CreateClient();

            HealthResponse? response = await client.GetFromJsonAsync<HealthResponse>("/health", JsonOptions);

            Assert.NotNull(response);
            Assert.Equal("Healthy", response!.Status);
            HealthCheckResponse check = Assert.Single(response.Checks);
            Assert.Equal("sqlite", check.Name);
            Assert.Equal("Healthy", check.Status);
        }

        [Fact]
        public async Task DashboardSummary_ReturnsCalculatedClinicMetrics()
        {
            using var factory = new ApiTestApplicationFactory();
            using HttpClient client = factory.CreateClient();
            await factory.SeedClinicDataAsync();

            DashboardResponse? response = await client.GetFromJsonAsync<DashboardResponse>(
                "/api/dashboard/summary",
                JsonOptions);

            Assert.NotNull(response);
            Assert.Equal(2, response!.TotalPatients);
            Assert.Equal(2, response.ActiveDoctorCount);
            Assert.Equal(1, response.TodayAppointmentCount);
            Assert.Equal(1, response.UpcomingAppointmentCount);
            Assert.Equal(1, response.CompletedPatientTreatmentCount);
            Assert.Equal(1, response.OutstandingPatientTreatmentCount);
            Assert.Equal(250m, response.OutstandingTreatmentValue);
            Assert.Single(response.UpcomingAppointments);
            Assert.NotEmpty(response.RecentAuditActivity);
        }

        [Fact]
        public async Task DoctorList_AppliesSearchAndPaginationWithoutExposingSalary()
        {
            using var factory = new ApiTestApplicationFactory();
            using HttpClient client = factory.CreateClient();
            await factory.SeedClinicDataAsync();

            using HttpResponseMessage response = await client.GetAsync("/api/doctors?search=ada&limit=1&offset=0");
            string json = await response.Content.ReadAsStringAsync();
            PagedResponse<DoctorResponse>? page = JsonSerializer.Deserialize<PagedResponse<DoctorResponse>>(
                json,
                JsonOptions);

            response.EnsureSuccessStatusCode();
            Assert.NotNull(page);
            Assert.Equal(1, page!.TotalCount);
            Assert.Equal("API-D1", Assert.Single(page.Items).Id);
            Assert.DoesNotContain("salary", json, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AppointmentList_AppliesCombinedIdentifierAndDateFilters()
        {
            using var factory = new ApiTestApplicationFactory();
            using HttpClient client = factory.CreateClient();
            await factory.SeedClinicDataAsync();

            PagedResponse<AppointmentResponse>? response =
                await client.GetFromJsonAsync<PagedResponse<AppointmentResponse>>(
                    "/api/appointments?doctorId=API-D2&patientId=API-P2&fromDate=2035-05-21&toDate=2035-05-21",
                    JsonOptions);

            Assert.NotNull(response);
            AppointmentResponse appointment = Assert.Single(response!.Items);
            Assert.Equal("API-P2", appointment.PatientId);
            Assert.Equal("2035-05-21", appointment.Date);
            Assert.Equal(1, response.TotalCount);
        }

        [Fact]
        public async Task MissingDoctor_ReturnsNotFound()
        {
            using var factory = new ApiTestApplicationFactory();
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response = await client.GetAsync("/api/doctors/does-not-exist");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("/api/doctors?limit=201")]
        [InlineData("/api/appointments?fromDate=2030-02-02&toDate=2030-01-01")]
        [InlineData("/api/audit?operation=Unknown")]
        public async Task InvalidQuery_ReturnsValidationProblem(string requestUri)
        {
            using var factory = new ApiTestApplicationFactory();
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response = await client.GetAsync(requestUri);
            string content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            Assert.Contains("errors", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AuditList_AppliesCombinedFiltersAndLimit()
        {
            using var factory = new ApiTestApplicationFactory();
            using HttpClient client = factory.CreateClient();
            await factory.SeedClinicDataAsync();

            BoundedResponse<AuditResponse>? response = await client.GetFromJsonAsync<BoundedResponse<AuditResponse>>(
                "/api/audit?entityType=Doctor&entityId=D1&actor=ApiTester&operation=Updated&limit=1",
                JsonOptions);

            Assert.NotNull(response);
            AuditResponse entry = Assert.Single(response!.Items);
            Assert.Equal("API-D1", entry.EntityId);
            Assert.Equal(AuditOperation.Updated, entry.Operation);
            Assert.Equal("ApiTester", entry.Actor);
            Assert.Equal(1, response.Limit);
        }

        [Fact]
        public async Task UnexpectedFailure_ReturnsGenericProblemWithoutInternalDetails()
        {
            using var factory = new ApiTestApplicationFactory(services =>
            {
                services.RemoveAll<IClinicQueryService>();
                services.AddSingleton<IClinicQueryService, ThrowingClinicQueryService>();
            });
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response = await client.GetAsync("/api/doctors");
            string content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            Assert.Contains("The request could not be completed.", content, StringComparison.Ordinal);
            Assert.DoesNotContain("Sensitive repository failure", content, StringComparison.Ordinal);
            Assert.DoesNotContain(nameof(InvalidOperationException), content, StringComparison.Ordinal);
        }

        private static JsonSerializerOptions CreateJsonOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }

        private sealed class ThrowingClinicQueryService : IClinicQueryService
        {
            public Task<PagedResult<Doctor>> GetDoctorsAsync(
                DoctorListQuery query,
                CancellationToken cancellationToken = default) =>
                throw new InvalidOperationException("Sensitive repository failure and stack context.");

            public Task<PagedResult<Patient>> GetPatientsAsync(
                PatientListQuery query,
                CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<PagedResult<Appointment>> GetAppointmentsAsync(
                AppointmentListQuery query,
                CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<PagedResult<Treatment>> GetTreatmentsAsync(
                TreatmentListQuery query,
                CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }
    }
}
