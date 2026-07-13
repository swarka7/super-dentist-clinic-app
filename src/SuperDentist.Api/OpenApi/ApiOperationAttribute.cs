namespace SuperDentist.Api.OpenApi
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ApiOperationAttribute : Attribute
    {
        public ApiOperationAttribute(string summary)
        {
            Summary = summary;
        }

        public string Summary { get; }
    }
}
