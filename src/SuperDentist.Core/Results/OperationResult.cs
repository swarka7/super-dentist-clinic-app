namespace SuperDentist.Core.Results
{
    public sealed record OperationResult(bool Success, string? ErrorMessage)
    {
        public static OperationResult Ok() => new(true, null);
        public static OperationResult Fail(string message) => new(false, message);
    }

    public sealed record OperationResult<T>(bool Success, string? ErrorMessage, T? Value)
    {
        public static OperationResult<T> Ok(T value) => new(true, null, value);
        public static OperationResult<T> Fail(string message) => new(false, message, default);
    }
}
