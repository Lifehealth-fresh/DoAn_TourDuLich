using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TourDuLich.Application.Helpers;

namespace TourDuLich.API.Hubs;

[Authorize]
public sealed class HoTroHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var maUser = Context.User?.FindFirst("MaUser")?.Value;
        if (!string.IsNullOrWhiteSpace(maUser))
        {
            var key = maUser.Trim();
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{key}");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{FixedLengthHelper.PadTo20(key).Trim()}");
        }
        if (Context.User?.IsInRole("Admin") == true || Context.User?.IsInRole("Sale") == true)
            await Groups.AddToGroupAsync(Context.ConnectionId, "staff");
        await base.OnConnectedAsync();
    }
}
