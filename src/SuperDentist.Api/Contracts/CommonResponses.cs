namespace SuperDentist.Api.Contracts
{
    public sealed record PagedResponse<T>(
        IReadOnlyList<T> Items,
        int TotalCount,
        int Limit,
        int Offset);

    public sealed record BoundedResponse<T>(
        IReadOnlyList<T> Items,
        int Count,
        int Limit);

    public sealed record HealthResponse(
        string Status,
        IReadOnlyList<HealthCheckResponse> Checks);

    public sealed record HealthCheckResponse(
        string Name,
        string Status,
        double DurationMilliseconds);
}
