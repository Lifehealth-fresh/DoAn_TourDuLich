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
public class DatDichVuController : ControllerBase
{
    private readonly AppDbContext _context;

    public DatDichVuController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("cua-toi")]
    public async Task<ActionResult> GetMyBookings()
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);

        var bookings = await _context.DatDichVus
            .Where(booking => booking.MaUser == maUserDb)
            .OrderByDescending(booking => booking.NgayDat)
            .Select(booking => new
            {
                maBooking = FixedLengthHelper.TrimSafe(booking.MaBooking),
                maTour = FixedLengthHelper.TrimSafe(booking.MaTour),
                tenTour = booking.MaTourNavigation.TenTour,
                maKhoiHanh = FixedLengthHelper.TrimSafe(booking.MaKhoiHanh),
                ngayDat = booking.NgayDat,
                slnguoiLon = booking.SlnguoiLon,
                sltreEm = booking.SltreEm,
                tongTien = booking.TongTien,
                tongGiamGia = booking.TongGiamGia,
                thanhTien = booking.ThanhTien,
                trangThai = FixedLengthHelper.TrimSafe(booking.TrangThai)
            })
            .ToListAsync();

        return Ok(bookings);
    }

    [HttpGet("{maBooking}")]
    public async Task<ActionResult> GetMyBooking(string maBooking)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maBookingDb = FixedLengthHelper.PadTo20(maBooking);

        var booking = await _context.DatDichVus
            .Where(item => item.MaBooking == maBookingDb && item.MaUser == maUserDb)
            .Select(item => new
            {
                maBooking = FixedLengthHelper.TrimSafe(item.MaBooking),
                maTour = FixedLengthHelper.TrimSafe(item.MaTour),
                tenTour = item.MaTourNavigation.TenTour,
                maKhoiHanh = FixedLengthHelper.TrimSafe(item.MaKhoiHanh),
                ngayKhoiHanh = item.MaKhoiHanhNavigation!.NgayKhoiHanh,
                ngayKetThuc = item.MaKhoiHanhNavigation.NgayKetThuc,
                diaDiem = item.MaKhoiHanhNavigation.DiaDiem,
                ngayDat = item.NgayDat,
                slnguoiLon = item.SlnguoiLon,
                sltreEm = item.SltreEm,
                tongTien = item.TongTien,
                tongGiamGia = item.TongGiamGia,
                thanhTien = item.ThanhTien,
                trangThai = FixedLengthHelper.TrimSafe(item.TrangThai)
            })
            .FirstOrDefaultAsync();

        if (booking is null)
        {
            return NotFound(new { message = "Không tìm thấy booking của bạn." });
        }

        return Ok(booking);
    }

    [HttpPost]
    public async Task<ActionResult> CreateBooking(DatDichVuCreateDto request)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.MaTour) ||
            string.IsNullOrWhiteSpace(request.MaKhoiHanh))
        {
            return BadRequest(new { message = "Mã tour và mã lịch khởi hành không được để trống." });
        }

        if (request.SlnguoiLon < 0 ||
            request.SltreEm < 0 ||
            request.SlnguoiLon + request.SltreEm <= 0)
        {
            return BadRequest(new
            {
                message = "Số người lớn và trẻ em phải hợp lệ, tổng số khách phải lớn hơn 0."
            });
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maTourDb = FixedLengthHelper.PadTo20(request.MaTour);
        var maKhoiHanhDb = FixedLengthHelper.PadTo20(request.MaKhoiHanh);

        var tour = await _context.Tours
            .FirstOrDefaultAsync(item => item.MaTour == maTourDb);

        if (tour is null)
        {
            return BadRequest(new { message = "Tour không tồn tại." });
        }

        var lichKhoiHanh = await _context.LichKhoiHanhs
            .FirstOrDefaultAsync(item => item.MaKhoiHanh == maKhoiHanhDb);

        if (lichKhoiHanh is null)
        {
            return BadRequest(new { message = "Lịch khởi hành không tồn tại." });
        }

        if (lichKhoiHanh.MaTour != maTourDb)
        {
            return BadRequest(new { message = "Lịch khởi hành không thuộc tour này." });
        }

        var tongSoKhach = request.SlnguoiLon + request.SltreEm;
        int tongTien;

        try
        {
            tongTien = checked(tour.GiaTour * tongSoKhach);
        }
        catch (OverflowException)
        {
            return BadRequest(new { message = "Tổng tiền vượt quá giới hạn cho phép." });
        }

        string maBooking;
        string maBookingDb;

        do
        {
            maBooking = $"BK{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            maBookingDb = FixedLengthHelper.PadTo20(maBooking);
        }
        while (await _context.DatDichVus
            .AnyAsync(item => item.MaBooking == maBookingDb));

        var booking = new DatDichVu
        {
            MaBooking = maBookingDb,
            MaUser = maUserDb,
            MaTour = maTourDb,
            MaKhoiHanh = maKhoiHanhDb,
            NgayDat = DateOnly.FromDateTime(DateTime.UtcNow),
            SlnguoiLon = request.SlnguoiLon,
            SltreEm = request.SltreEm,
            TongTien = tongTien,
            TongGiamGia = 0,
            ThanhTien = tongTien,
            TrangThai = FixedLengthHelper.PadTo20("ChoXacNhan")
        };

        _context.DatDichVus.Add(booking);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            maBooking = FixedLengthHelper.TrimSafe(booking.MaBooking),
            maTour = FixedLengthHelper.TrimSafe(booking.MaTour),
            maKhoiHanh = FixedLengthHelper.TrimSafe(booking.MaKhoiHanh),
            ngayDat = booking.NgayDat,
            slnguoiLon = booking.SlnguoiLon,
            sltreEm = booking.SltreEm,
            tongTien = booking.TongTien,
            tongGiamGia = booking.TongGiamGia,
            thanhTien = booking.ThanhTien,
            trangThai = FixedLengthHelper.TrimSafe(booking.TrangThai)
        });
    }

    [HttpPut("{maBooking}/huy")]
    public async Task<ActionResult> CancelMyBooking(string maBooking)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maBookingDb = FixedLengthHelper.PadTo20(maBooking);

        var booking = await _context.DatDichVus
            .FirstOrDefaultAsync(item =>
                item.MaBooking == maBookingDb &&
                item.MaUser == maUserDb);

        if (booking is null)
        {
            return NotFound(new { message = "Không tìm thấy booking của bạn." });
        }

        if (FixedLengthHelper.TrimSafe(booking.TrangThai) != "ChoXacNhan")
        {
            return BadRequest(new
            {
                message = "Chỉ có thể hủy booking đang ở trạng thái chờ xác nhận."
            });
        }

        booking.TrangThai = FixedLengthHelper.PadTo20("DaHuy");
        await _context.SaveChangesAsync();

        return Ok(new
        {
            maBooking = FixedLengthHelper.TrimSafe(booking.MaBooking),
            trangThai = FixedLengthHelper.TrimSafe(booking.TrangThai)
        });
    }

    private string? GetCurrentMaUser()
    {
        return User.FindFirst("MaUser")?.Value;
    }
}