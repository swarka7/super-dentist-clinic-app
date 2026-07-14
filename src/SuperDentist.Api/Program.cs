using Microsoft.OpenApi.Models;
using SuperDentist.Api.Health;
using SuperDentist.Api.Infrastructure;
using SuperDentist.Api.OpenApi;
using SuperDentist.Application;
using SuperDentist.Core.Options;
using SuperDentist.Core.Services;
using SuperDentist.Infrastructure;
using System.Text.Json.Serialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));
builder.Services.AddSuperDentistInfrastructure();
builder.Services.AddSuperDentistApplication();

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks().AddCheck<SqliteHealthCheck>("sqlite");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Super Dentist Reporting API",
        Version = "v1",
        Description = "Read-only clinic operations and audit reporting endpoints."
    });
    options.OperationFilter<ApiOperationFilter>();
});

const string DevelopmentCorsPolicy = "ReactDevelopmentClient";
if (builder.Environment.IsDevelopment())
{
    string[] allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
        ?? Array.Empty<string>();

    if (allowedOrigins.Length == 0)
    {
        throw new InvalidOperationException("At least one development CORS origin must be configured.");
    }

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(DevelopmentCorsPolicy, policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .WithMethods(HttpMethods.Get)
                .AllowAnyHeader();
        });
    });
}

WebApplication app = builder.Build();

await InitializeDatabaseAsync(app.Services, app.Logger).ConfigureAwait(false);

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(DevelopmentCorsPolicy);
}

app.MapControllers();
app.Run();

static async Task InitializeDatabaseAsync(IServiceProvider services, ILogger logger)
{
    await using AsyncServiceScope scope = services.CreateAsyncScope();
    IDatabaseInitializer initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();

    try
    {
        InitializationResult result = await initializer.InitializeAsync().ConfigureAwait(false);
        logger.LogInformation(
            "API database initialized at {DatabasePath}; new database: {IsNewDatabase}",
            result.DatabasePath,
            result.IsNewDatabase);
    }
    catch (Exception exception)
    {
        logger.LogCritical(exception, "API startup stopped because database initialization failed");
        throw;
    }
}

public partial class Program
{
}
