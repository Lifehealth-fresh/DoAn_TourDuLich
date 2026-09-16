using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TourDuLich.API.Hubs;

[Authorize]
public sealed class HoTroHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var maUser = Context.User?.FindFirst("MaUser")?.Value;
        if (!string.IsNullOrWhiteSpace(maUser))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{maUser.Trim()}");
        if (Context.User?.IsInRole("Admin") == true || Context.User?.IsInRole("Sale") == true)
            await Groups.AddToGroupAsync(Context.ConnectionId, "staff");
        await base.OnConnectedAsync();
    }
}
