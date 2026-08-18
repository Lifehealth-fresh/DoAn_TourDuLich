using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TourDuLich.API.Swagger;

public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAllowAnonymous =
            context.MethodInfo.GetCustomAttributes(true)
                .OfType<AllowAnonymousAttribute>()
                .Any()
            || context.MethodInfo.DeclaringType?
                .GetCustomAttributes(true)
                .OfType<AllowAnonymousAttribute>()
                .Any() == true;

        var hasAuthorize =
            context.MethodInfo.GetCustomAttributes(true)
                .OfType<AuthorizeAttribute>()
                .Any()
            || context.MethodInfo.DeclaringType?
                .GetCustomAttributes(true)
                .OfType<AuthorizeAttribute>()
                .Any() == true;

        if (hasAllowAnonymous || !hasAuthorize)
        {
            return;
        }

        operation.Responses ??= [];
        operation.Responses.TryAdd("401", new OpenApiResponse
        {
            Description = "Unauthorized"
        });

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
            }
        ];
    }
}