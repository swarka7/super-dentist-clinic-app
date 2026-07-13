using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace SuperDentist.Api.OpenApi
{
    internal sealed class ApiOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            ApiOperationAttribute? description = context.MethodInfo
                .GetCustomAttribute<ApiOperationAttribute>();
            if (description != null)
            {
                operation.Summary = description.Summary;
            }
        }
    }
}
