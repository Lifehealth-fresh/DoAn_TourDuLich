using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourDuLich.Application.Helpers;
using TourDuLich.Application.Services;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TinhThanhController : ControllerBase
{
    private readonly IDestinationResolver _destinations;
    public TinhThanhController(IDestinationResolver destinations) => _destinations = destinations;

    [HttpGet]
    public async Task<ActionResult> Search([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var rows = await _destinations.SearchAsync(q, cancellationToken);
        return Ok(rows.Select(item => new
        {
            maTinh = FixedLengthHelper.TrimSafe(item.MaTinh),
            tenTinh = item.TenTinh,
            maKhuVuc = FixedLengthHelper.TrimSafe(item.MaKhuVuc),
            tenKhuVuc = item.MaKhuVucNavigation?.TenKhuVuc
        }));
    }
}
