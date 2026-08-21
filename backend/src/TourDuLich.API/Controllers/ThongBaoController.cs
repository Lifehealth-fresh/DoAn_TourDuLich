using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ThongBaoController : ControllerBase
{
    private readonly AppDbContext _context;

    public ThongBaoController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/ThongBao/cua-toi?chuaDoc={true|false}
    [HttpGet("cua-toi")]
    public async Task<ActionResult> GetMine([FromQuery] bool? chuaDoc)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);

        var query = _context.ThongBaos
            .AsNoTracking()
            .Where(item => item.MaUser == maUserDb);

        if (chuaDoc.HasValue)
        {
            query = query.Where(item => item.DaDoc == !chuaDoc.Value);
        }

        var items = await query
            .OrderByDescending(item => item.NgayGui)
            .Select(item => new
            {
                maThongBao = FixedLengthHelper.TrimSafe(item.MaThongBao),
                tieuDe = item.TieuDe,
                noiDung = item.NoiDung,
                daDoc = item.DaDoc,
                ngayGui = item.NgayGui
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET /api/ThongBao/cua-toi/dem-chua-doc
    [HttpGet("cua-toi/dem-chua-doc")]
    public async Task<ActionResult> CountUnread()
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);

        var soLuong = await _context.ThongBaos
            .CountAsync(item =>
                item.MaUser == maUserDb &&
                item.DaDoc != true);

        return Ok(new { soLuong });
    }

    // PUT /api/ThongBao/{maThongBao}/danh-dau-da-doc
    [HttpPut("{maThongBao}/danh-dau-da-doc")]
    public async Task<IActionResult> MarkAsRead(string maThongBao)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maThongBaoDb = FixedLengthHelper.PadTo20(maThongBao);

        var thongBao = await _context.ThongBaos
            .FirstOrDefaultAsync(item =>
                item.MaThongBao == maThongBaoDb &&
                item.MaUser == maUserDb);

        if (thongBao is null)
        {
            return NotFound(new { message = "Không tìm thấy thông báo của bạn." });
        }

        thongBao.DaDoc = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // PUT /api/ThongBao/danh-dau-tat-ca-da-doc
    [HttpPut("danh-dau-tat-ca-da-doc")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);

        await _context.ThongBaos
            .Where(item =>
                item.MaUser == maUserDb &&
                item.DaDoc != true)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                item => item.DaDoc,
                true));

        return NoContent();
    }

    private string? GetCurrentMaUser()
    {
        return User.FindFirst("MaUser")?.Value;
    }
}
