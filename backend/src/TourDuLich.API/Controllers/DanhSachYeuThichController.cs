using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourDuLich.Application.Helpers;
using TourDuLich.API.DTOs;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DanhSachYeuThichController : ControllerBase
{
    private readonly AppDbContext _context;

    public DanhSachYeuThichController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("cua-toi")]
    public async Task<ActionResult> GetMine()
    {
        var maUser = GetCurrentMaUser();
        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);

        var items = await _context.DanhSachYeuThiches
            .AsNoTracking()
            .Where(item => item.MaUser == maUserDb)
            .OrderByDescending(item => item.NgayThem)
            .Select(item => new
            {
                maWish = FixedLengthHelper.TrimSafe(item.MaWish),
                maTour = FixedLengthHelper.TrimSafe(item.MaTour),
                tenTour = item.MaTourNavigation!.TenTour,
                giaTour = item.MaTourNavigation.GiaTour,
                trangThai = FixedLengthHelper.TrimSafe(item.MaTourNavigation.TrangThai),
                ngayThem = item.NgayThem
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult> Add(DanhSachYeuThichCreateDto request)
    {
        var maUser = GetCurrentMaUser();
        if (maUser is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.MaTour))
        {
            return BadRequest(new { message = "Mã tour không được để trống." });
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maTourDb = FixedLengthHelper.PadTo20(request.MaTour);

        var tourTonTai = await _context.Tours
            .AnyAsync(item => item.MaTour == maTourDb);

        if (!tourTonTai)
        {
            return BadRequest(new { message = "Tour không tồn tại." });
        }

        var daYeuThich = await _context.DanhSachYeuThiches
            .AnyAsync(item =>
                item.MaUser == maUserDb &&
                item.MaTour == maTourDb);

        if (daYeuThich)
        {
            return Conflict(new { message = "Tour này đã có trong danh sách yêu thích." });
        }

        var maWishDb = await GenerateMaWishAsync();

        var wish = new DanhSachYeuThich
        {
            MaWish = maWishDb,
            MaUser = maUserDb,
            MaTour = maTourDb,
            NgayThem = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        _context.DanhSachYeuThiches.Add(wish);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            maWish = FixedLengthHelper.TrimSafe(wish.MaWish),
            maTour = FixedLengthHelper.TrimSafe(wish.MaTour),
            ngayThem = wish.NgayThem
        });
    }

    [HttpDelete("{maTour}")]
    public async Task<IActionResult> Remove(string maTour)
    {
        var maUser = GetCurrentMaUser();
        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maTourDb = FixedLengthHelper.PadTo20(maTour);

        var wish = await _context.DanhSachYeuThiches
            .FirstOrDefaultAsync(item =>
                item.MaUser == maUserDb &&
                item.MaTour == maTourDb);

        if (wish is null)
        {
            return NotFound(new { message = "Tour không có trong danh sách yêu thích." });
        }

        _context.DanhSachYeuThiches.Remove(wish);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private string? GetCurrentMaUser()
    {
        return User.FindFirst("MaUser")?.Value;
    }

    private async Task<string> GenerateMaWishAsync()
    {
        string maWishDb;

        do
        {
            var maWish = $"WL{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            maWishDb = FixedLengthHelper.PadTo20(maWish);
        }
        while (await _context.DanhSachYeuThiches
            .AnyAsync(item => item.MaWish == maWishDb));

        return maWishDb;
    }
}

