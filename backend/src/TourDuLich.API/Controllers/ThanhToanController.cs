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
public class ThanhToanController : ControllerBase
{
    private readonly AppDbContext _context;

    public ThanhToanController(AppDbContext context)
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

        var bookingExists = await _context.DatDichVus
            .AnyAsync(booking =>
                booking.MaBooking == maBookingDb &&
                booking.MaUser == maUserDb);

        if (!bookingExists)
        {
            return NotFound(new
            {
                message = "Không tìm thấy booking thuộc tài khoản của bạn."
            });
        }

        var payments = await _context.ThanhToans
            .Where(payment => payment.MaBooking == maBookingDb)
            .OrderByDescending(payment => payment.NgayTt)
            .Select(payment => new
            {
                maTt = FixedLengthHelper.TrimSafe(payment.MaTt),
                maBooking = FixedLengthHelper.TrimSafe(payment.MaBooking),
                soTien = payment.SoTien,
                ngayTt = payment.NgayTt,
                trangThai = FixedLengthHelper.TrimSafe(payment.TrangThai),
                phuongThuc = payment.PhuongThuc,
                loaiThanhToan = FixedLengthHelper.TrimSafe(payment.LoaiThanhToan)
            })
            .ToListAsync();

        return Ok(payments);
    }

    [HttpGet("theo-booking/{maBooking}/tong-hop")]
    public async Task<ActionResult> GetSummary(string maBooking)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var maBookingDb = FixedLengthHelper.PadTo20(maBooking);
        var thanhCong = FixedLengthHelper.PadTo20("ThanhCong");

        var booking = await _context.DatDichVus
            .FirstOrDefaultAsync(item =>
                item.MaBooking == maBookingDb);

        if (booking is null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy booking."
            });
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var isOwner = booking.MaUser == maUserDb;
        var isSale = User.IsInRole("Sale");
        var isAdmin = User.IsInRole("Admin");

        if (!isOwner && !isSale && !isAdmin)
        {
            return NotFound(new { message = "Không tìm thấy booking." });
        }

        var daThanhToan = await _context.ThanhToans
            .Where(payment =>
                payment.MaBooking == maBookingDb &&
                payment.TrangThai == thanhCong)
            .SumAsync(payment => (int?)payment.SoTien) ?? 0;

        var tongTien = booking.ThanhTien ?? 0;

        if (isSale && !isOwner)
        {
            return Ok(new
            {
                maBooking = FixedLengthHelper.TrimSafe(booking.MaBooking),
                tongTien,
                daThanhToan,
                conLai = tongTien - daThanhToan
            });
        }

        return Ok(new
        {
            maBooking = FixedLengthHelper.TrimSafe(booking.MaBooking),
            tongTien,
            daThanhToan,
            conLai = tongTien - daThanhToan
        });
    }

    [HttpPost]
    public async Task<ActionResult> Create(ThanhToanCreateDto request)
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

        if (request.SoTien <= 0)
        {
            return BadRequest(new { message = "Số tiền thanh toán phải lớn hơn 0." });
        }

        var loaiThanhToan = request.LoaiThanhToan?.Trim();

        var loaiHopLe = new[]
        {
            "DatCoc",
            "ThanhToanConLai",
            "ThanhToanDu"
        };

        if (string.IsNullOrWhiteSpace(loaiThanhToan) ||
            !loaiHopLe.Contains(loaiThanhToan))
        {
            return BadRequest(new
            {
                message = "Loại thanh toán không hợp lệ. Giá trị hợp lệ gồm: DatCoc, ThanhToanConLai, ThanhToanDu."
            });
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maBookingDb = FixedLengthHelper.PadTo20(request.MaBooking);
        var thanhCong = FixedLengthHelper.PadTo20("ThanhCong");

        var booking = await _context.DatDichVus
            .FirstOrDefaultAsync(item =>
                item.MaBooking == maBookingDb &&
                item.MaUser == maUserDb);

        if (booking is null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy booking thuộc tài khoản của bạn."
            });
        }

        var tongTien = booking.ThanhTien ?? 0;

        var daThanhToan = await _context.ThanhToans
            .Where(payment =>
                payment.MaBooking == maBookingDb &&
                payment.TrangThai == thanhCong)
            .SumAsync(payment => (int?)payment.SoTien) ?? 0;

        var conLai = tongTien - daThanhToan;

        if (request.SoTien > conLai)
        {
            return BadRequest(new
            {
                message = "Số tiền vượt quá phần còn lại cần thanh toán"
            });
        }

        string maTt;
        string maTtDb;

        do
        {
            maTt = $"TT{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            maTtDb = FixedLengthHelper.PadTo20(maTt);
        }
        while (await _context.ThanhToans
            .AnyAsync(payment => payment.MaTt == maTtDb));

        var paymentEntity = new ThanhToan
        {
            MaTt = maTtDb,
            MaBooking = maBookingDb,
            SoTien = request.SoTien,
            NgayTt = DateTime.UtcNow,
            TrangThai = thanhCong,
            PhuongThuc = request.PhuongThuc?.Trim(),
            LoaiThanhToan = FixedLengthHelper.PadTo20(loaiThanhToan)
        };

        _context.ThanhToans.Add(paymentEntity);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            maTt = FixedLengthHelper.TrimSafe(paymentEntity.MaTt),
            maBooking = FixedLengthHelper.TrimSafe(paymentEntity.MaBooking),
            soTien = paymentEntity.SoTien,
            ngayTt = paymentEntity.NgayTt,
            trangThai = FixedLengthHelper.TrimSafe(paymentEntity.TrangThai),
            phuongThuc = paymentEntity.PhuongThuc,
            loaiThanhToan = FixedLengthHelper.TrimSafe(paymentEntity.LoaiThanhToan)
        });
    }

    private string? GetCurrentMaUser()
    {
        return User.FindFirst("MaUser")?.Value;
    }
}
