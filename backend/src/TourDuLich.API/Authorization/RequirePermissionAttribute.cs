using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace TourDuLich.API.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    public RequirePermissionAttribute(string chucNang, string hanhDong)
    {
        ChucNang = chucNang;
        HanhDong = hanhDong;
    }

    public string ChucNang { get; }
    public string HanhDong { get; }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
            return;

        var service = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        if (await service.CanAsync(user, ChucNang, HanhDong, context.HttpContext.RequestAborted))
            return;

        context.Result = new ObjectResult(new
        {
            message = PermissionCatalog.DeniedMessage(ChucNang, HanhDong)
        })
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }
}
