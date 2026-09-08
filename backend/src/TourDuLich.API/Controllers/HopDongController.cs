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
        var maBookingDb = FixedLengthHelper.PadTo20(maBooking);
        var isStaff = User.IsInRole("Sale") || User.IsInRole("Admin");

        if (!isStaff && maUser is null)
            return Unauthorized();

        var query = _context.HopDongs
            .Where(contract => contract.MaBooking == maBookingDb);

        if (!isStaff)
        {
            var maUserDb = FixedLengthHelper.PadTo20(maUser!);
            query = query.Where(contract => contract.MaBookingNavigation.MaUser == maUserDb);
        }

        var hopDong = await query
            .Select(contract => new
            {
                maHopDong = FixedLengthHelper.TrimSafe(contract.MaHopDong),
                maBooking = FixedLengthHelper.TrimSafe(contract.MaBooking),
                soHopDong = contract.SoHopDong,
                ngayKy = contract.NgayKy,
                dieuKhoanCamKet = contract.DieuKhoanCamKet,
                fileHopDongUrl = contract.FileHopDongUrl,
                nguoiDaiDien = FixedLengthHelper.TrimSafe(contract.NguoiDaiDien),
                trangThai = FixedLengthHelper.TrimSafe(contract.TrangThai),
                hoTenKhach = contract.HoTenKhach,
                loaiGiayTo = contract.LoaiGiayTo,
                soGiayTo = contract.SoGiayTo
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
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> Create(HopDongCreateDto request)
    {
        if (string.IsNullOrWhiteSpace(request.MaBooking))
        {
            return BadRequest(new { message = "Mã booking không được để trống." });
        }

        var maBookingDb = FixedLengthHelper.PadTo20(request.MaBooking);

        var bookingData = await _context.DatDichVus
            .Where(booking => booking.MaBooking == maBookingDb)
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
                message = "Không tìm thấy booking."
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
        var khachHang = bookingData.Booking.MaKhachHang is null
            ? null
            : await _context.KhachHangs
                .Include(item => item.GiayTos)
                .FirstOrDefaultAsync(item =>
                    item.MaKhachHang == bookingData.Booking.MaKhachHang);
        var giayToMoiNhat = khachHang?.GiayTos
            .OrderByDescending(item => item.MaGiayTo)
            .FirstOrDefault();
        var hoTenKhach = khachHang is null
            ? null
            : $"{khachHang.Ho} {khachHang.Ten}".Trim();
        if (hoTenKhach?.Length > 70)
            hoTenKhach = hoTenKhach[..70];

        var hopDong = new HopDong
        {
            MaHopDong = maHopDongDb,
            MaBooking = maBookingDb,
            SoHopDong = soHopDong,
            NgayKy = null,
            DieuKhoanCamKet = dieuKhoanCamKet,
            FileHopDongUrl = null,
            NguoiDaiDien = null,
            TrangThai = FixedLengthHelper.PadTo20("DuThao"),
            HoTenKhach = hoTenKhach,
            LoaiGiayTo = giayToMoiNhat?.LoaiGiayTo,
            SoGiayTo = giayToMoiNhat?.SoTrenGiayTo
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
            trangThai = FixedLengthHelper.TrimSafe(hopDong.TrangThai),
            hoTenKhach = hopDong.HoTenKhach,
            loaiGiayTo = hopDong.LoaiGiayTo,
            soGiayTo = hopDong.SoGiayTo
        });
    }

    [HttpPut("{maHopDong}/ky")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> Sign(string maHopDong)
    {
        var maHopDongDb = FixedLengthHelper.PadTo20(maHopDong);

        var hopDong = await _context.HopDongs
            .FirstOrDefaultAsync(contract => contract.MaHopDong == maHopDongDb);

        if (hopDong is null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy hợp đồng."
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
        hopDong.NgayKy = DateOnly.FromDateTime(DateTime.UtcNow);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            maHopDong = FixedLengthHelper.TrimSafe(hopDong.MaHopDong),
            maBooking = FixedLengthHelper.TrimSafe(hopDong.MaBooking),
            trangThai = FixedLengthHelper.TrimSafe(hopDong.TrangThai),
            ngayKy = hopDong.NgayKy
        });
    }

    private string? GetCurrentMaUser()
    {
        return User.FindFirst("MaUser")?.Value;
    }
}
