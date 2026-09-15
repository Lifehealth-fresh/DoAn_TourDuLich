using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourDuLich.Application.Helpers;
using TourDuLich.Application.Services;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/HoTro")]
[Authorize]
public class HoTroController : ControllerBase
{
    private readonly IDesignChatService _chat;

    public HoTroController(IDesignChatService chat) => _chat = chat;

    [HttpPost("chat")]
    public async Task<ActionResult> Chat([FromBody] DesignChatRequest request, CancellationToken cancellationToken)
    {
        var maUser = User.FindFirst("MaUser")?.Value;
        if (string.IsNullOrWhiteSpace(maUser)) return Unauthorized();
        try
        {
            return Ok(await _chat.StartOrContinueAsync(FixedLengthHelper.PadTo20(maUser), request, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }
}
