using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourDuLich.API.DTOs;
using TourDuLich.Application.Helpers;
using TourDuLich.Application.Services;
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
        "XemLichTrinh",
        "DatTour",
        "ThanhToan",
        "DanhGiaTour",
        "DanhGiaHdv",
        "DanhGiaSanPham",
        "TuChoiGoiY",
        "HoanThanh"
    };

    // This endpoint is for client-side telemetry only. Business events are
    // recorded by their owning backend transaction via IHanhViLogger.
    private static readonly HashSet<string> HanhDongUiHopLe = new(StringComparer.OrdinalIgnoreCase)
    {
        "Xem",
        "TimKiem"
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
            !HanhDongUiHopLe.Contains(request.HanhDong.Trim()))
        {
            return BadRequest(new
            {
                message = "Endpoint này chỉ nhận hành vi UI: Xem hoặc TimKiem. Hành vi nghiệp vụ được backend tự ghi."
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
    public async Task<ActionResult> GetMine([FromQuery] string? hanhDong, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
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
                    message = "Hành động không hợp lệ. Chỉ chấp nhận các hành động đã khai báo trong hệ thống."
                });
            }

            var hanhDongDb = FixedLengthHelper.PadTo20(hanhDong.Trim());
            query = query.Where(item => item.HanhDong == hanhDongDb);
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(item => item.ThoiGian).ThenBy(item => item.MaHanhDong)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(item => new
            {
                maHanhDong = FixedLengthHelper.TrimSafe(item.MaHanhDong),
                maTour = FixedLengthHelper.TrimSafe(item.MaTour),
                tenTour = item.MaTourNavigation!.TenTour,
                hanhDong = FixedLengthHelper.TrimSafe(item.HanhDong),
                thoiGian = item.ThoiGian
            })
            .ToListAsync();

        return Ok(new { items, page, pageSize, totalCount });
    }

    private string? GetCurrentMaUser()
    {
        return User.FindFirst("MaUser")?.Value;
    }

    private async Task<string> GenerateMaHanhDongAsync()
    {
        return await HanhViLogger.GenerateMaHanhDongAsync(_context);
    }
}
