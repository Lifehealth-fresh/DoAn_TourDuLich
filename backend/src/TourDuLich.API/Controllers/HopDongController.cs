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
public class HopDongController : ControllerBase
{
    private readonly AppDbContext _context;

    public HopDongController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("theo-booking/{maBooking}")]
    public async Task<ActionResult> GetByBooking(string maBooking)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maBookingDb = FixedLengthHelper.PadTo20(maBooking);

        var hopDong = await _context.HopDongs
            .Where(contract =>
                contract.MaBooking == maBookingDb &&
                contract.MaBookingNavigation.MaUser == maUserDb)
            .Select(contract => new
            {
                maHopDong = FixedLengthHelper.TrimSafe(contract.MaHopDong),
                maBooking = FixedLengthHelper.TrimSafe(contract.MaBooking),
                soHopDong = contract.SoHopDong,
                ngayKy = contract.NgayKy,
                dieuKhoanCamKet = contract.DieuKhoanCamKet,
                fileHopDongUrl = contract.FileHopDongUrl,
                nguoiDaiDien = FixedLengthHelper.TrimSafe(contract.NguoiDaiDien),
                trangThai = FixedLengthHelper.TrimSafe(contract.TrangThai)
            })
            .FirstOrDefaultAsync();

        if (hopDong is null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy hợp đồng thuộc booking của bạn."
            });
        }

        return Ok(hopDong);
    }

    [HttpPost]
    public async Task<ActionResult> Create(HopDongCreateDto request)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.MaBooking))
        {
            return BadRequest(new { message = "Mã booking không được để trống." });
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maBookingDb = FixedLengthHelper.PadTo20(request.MaBooking);

        var bookingData = await _context.DatDichVus
            .Where(booking =>
                booking.MaBooking == maBookingDb &&
                booking.MaUser == maUserDb)
            .Select(booking => new
            {
                Booking = booking,
                DieuKhoanTour = booking.MaTourNavigation.DieuKhoan
            })
            .FirstOrDefaultAsync();

        if (bookingData is null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy booking thuộc tài khoản của bạn."
            });
        }

        var trangThaiBooking = FixedLengthHelper.TrimSafe(
            bookingData.Booking.TrangThai);

        if (trangThaiBooking != "ChoXacNhan")
        {
            return BadRequest(new
            {
                message = "Booking không ở trạng thái có thể lập hợp đồng"
            });
        }

        var daCoHopDong = await _context.HopDongs
            .AnyAsync(contract => contract.MaBooking == maBookingDb);

        if (daCoHopDong)
        {
            return Conflict(new
            {
                message = "Booking này đã có hợp đồng."
            });
        }

        string maHopDong;
        string maHopDongDb;

        do
        {
            maHopDong = $"HD{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            maHopDongDb = FixedLengthHelper.PadTo20(maHopDong);
        }
        while (await _context.HopDongs
            .AnyAsync(contract => contract.MaHopDong == maHopDongDb));

        var soHopDong = string.IsNullOrWhiteSpace(request.SoHopDong)
            ? $"HD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..50]
            : request.SoHopDong.Trim();

        var dieuKhoanCamKet = string.IsNullOrWhiteSpace(request.DieuKhoanCamKet)
            ? bookingData.DieuKhoanTour
            : request.DieuKhoanCamKet.Trim();

        var hopDong = new HopDong
        {
            MaHopDong = maHopDongDb,
            MaBooking = maBookingDb,
            SoHopDong = soHopDong,
            NgayKy = DateOnly.FromDateTime(DateTime.UtcNow),
            DieuKhoanCamKet = dieuKhoanCamKet,
            FileHopDongUrl = null,
            NguoiDaiDien = null,
            TrangThai = FixedLengthHelper.PadTo20("DuThao")
        };

        _context.HopDongs.Add(hopDong);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            maHopDong = FixedLengthHelper.TrimSafe(hopDong.MaHopDong),
            maBooking = FixedLengthHelper.TrimSafe(hopDong.MaBooking),
            soHopDong = hopDong.SoHopDong,
            ngayKy = hopDong.NgayKy,
            dieuKhoanCamKet = hopDong.DieuKhoanCamKet,
            trangThai = FixedLengthHelper.TrimSafe(hopDong.TrangThai)
        });
    }

    [HttpPut("{maHopDong}/ky")]
    public async Task<ActionResult> Sign(string maHopDong)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maHopDongDb = FixedLengthHelper.PadTo20(maHopDong);

        var hopDong = await _context.HopDongs
            .FirstOrDefaultAsync(contract =>
                contract.MaHopDong == maHopDongDb &&
                contract.MaBookingNavigation.MaUser == maUserDb);

        if (hopDong is null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy hợp đồng thuộc tài khoản của bạn."
            });
        }

        var trangThai = FixedLengthHelper.TrimSafe(hopDong.TrangThai);

        if (trangThai != "DuThao")
        {
            return BadRequest(new
            {
                message = "Chỉ hợp đồng ở trạng thái dự thảo mới có thể ký."
            });
        }

        hopDong.TrangThai = FixedLengthHelper.PadTo20("DaKy");

        await _context.SaveChangesAsync();

        return Ok(new
        {
            maHopDong = FixedLengthHelper.TrimSafe(hopDong.MaHopDong),
            maBooking = FixedLengthHelper.TrimSafe(hopDong.MaBooking),
            trangThai = FixedLengthHelper.TrimSafe(hopDong.TrangThai)
        });
    }

    private string? GetCurrentMaUser()
    {
        return User.FindFirst("MaUser")?.Value;
    }
}