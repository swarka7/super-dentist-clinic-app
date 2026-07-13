using Microsoft.Extensions.Diagnostics.HealthChecks;
using SuperDentist.Infrastructure.Data;

namespace SuperDentist.Api.Health
{
    internal sealed class SqliteHealthCheck : IHealthCheck
    {
        private readonly ISqliteConnectionFactory _connectionFactory;

        public SqliteHealthCheck(ISqliteConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var connection = await _connectionFactory
                    .OpenConnectionAsync(cancellationToken)
                    .ConfigureAwait(false);
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1;";
                object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                return Convert.ToInt32(result) == 1
                    ? HealthCheckResult.Healthy("SQLite is available.")
                    : HealthCheckResult.Unhealthy("SQLite returned an unexpected result.");
            }
            catch (Exception exception)
            {
                return HealthCheckResult.Unhealthy("SQLite is unavailable.", exception);
            }
        }
    }
}
