using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourDuLich.API.DTOs;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HanhViKhachHangController : ControllerBase
{
    private static readonly HashSet<string> HanhDongHopLe = new(StringComparer.OrdinalIgnoreCase)
    {
        "Xem",
        "TimKiem",
        "ThemYeuThich",
        "XemLichTrinh"
    };

    private readonly AppDbContext _context;

    public HanhViKhachHangController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult> Create(HanhViCreateDto request)
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

        if (string.IsNullOrWhiteSpace(request.HanhDong) ||
            !HanhDongHopLe.Contains(request.HanhDong.Trim()))
        {
            return BadRequest(new
            {
                message = "Hành động không hợp lệ. Chỉ chấp nhận: Xem, TimKiem, ThemYeuThich, XemLichTrinh."
            });
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maTourDb = FixedLengthHelper.PadTo20(request.MaTour);

        var tourTonTai = await _context.Tours
            .AnyAsync(item => item.MaTour == maTourDb);

        if (!tourTonTai)
        {
            return BadRequest(new { message = "Tour không tồn tại." });
        }

        var maHanhDongDb = await GenerateMaHanhDongAsync();

        var hanhVi = new HanhViKhachHang
        {
            MaHanhDong = maHanhDongDb,
            MaUser = maUserDb,
            MaTour = maTourDb,
            HanhDong = FixedLengthHelper.PadTo20(request.HanhDong.Trim()),
            ThoiGian = DateTime.UtcNow
        };

        _context.HanhViKhachHangs.Add(hanhVi);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            maHanhDong = FixedLengthHelper.TrimSafe(hanhVi.MaHanhDong),
            maUser = FixedLengthHelper.TrimSafe(hanhVi.MaUser),
            maTour = FixedLengthHelper.TrimSafe(hanhVi.MaTour),
            hanhDong = FixedLengthHelper.TrimSafe(hanhVi.HanhDong),
            thoiGian = hanhVi.ThoiGian
        });
    }

    [HttpGet("cua-toi")]
    public async Task<ActionResult> GetMine([FromQuery] string? hanhDong)
    {
        var maUser = GetCurrentMaUser();
        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);

        var query = _context.HanhViKhachHangs
            .AsNoTracking()
            .Where(item => item.MaUser == maUserDb);

        if (!string.IsNullOrWhiteSpace(hanhDong))
        {
            if (!HanhDongHopLe.Contains(hanhDong.Trim()))
            {
                return BadRequest(new
                {
                    message = "Hành động không hợp lệ. Chỉ chấp nhận: Xem, TimKiem, ThemYeuThich, XemLichTrinh."
                });
            }

            var hanhDongDb = FixedLengthHelper.PadTo20(hanhDong.Trim());
            query = query.Where(item => item.HanhDong == hanhDongDb);
        }

        var items = await query
            .OrderByDescending(item => item.ThoiGian)
            .Take(100)
            .Select(item => new
            {
                maHanhDong = FixedLengthHelper.TrimSafe(item.MaHanhDong),
                maTour = FixedLengthHelper.TrimSafe(item.MaTour),
                tenTour = item.MaTourNavigation!.TenTour,
                hanhDong = FixedLengthHelper.TrimSafe(item.HanhDong),
                thoiGian = item.ThoiGian
            })
            .ToListAsync();

        return Ok(items);
    }

    private string? GetCurrentMaUser()
    {
        return User.FindFirst("MaUser")?.Value;
    }

    private async Task<string> GenerateMaHanhDongAsync()
    {
        string maHanhDongDb;

        do
        {
            var maHanhDong = $"HV{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            maHanhDongDb = FixedLengthHelper.PadTo20(maHanhDong);
        }
        while (await _context.HanhViKhachHangs
            .AnyAsync(item => item.MaHanhDong == maHanhDongDb));

        return maHanhDongDb;
    }
}