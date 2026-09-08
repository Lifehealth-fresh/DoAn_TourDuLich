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
public class ThanhToanController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHanhViLogger _hanhViLogger;

    public ThanhToanController(AppDbContext context, IHanhViLogger hanhViLogger)
    {
        _context = context;
        _hanhViLogger = hanhViLogger;
    }

    [HttpGet("theo-booking/{maBooking}")]
    [Authorize(Roles = "KhachHang")]
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
    [Authorize(Roles = "KhachHang")]
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

        if (FixedLengthHelper.TrimSafe(booking.TrangThai) == "DaHuy")
            return BadRequest(new { message = "Booking đã hủy, không thể tiếp tục thanh toán." });

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var isOwner = booking.MaUser == maUserDb;
        var isSale = User.IsInRole("Sale");
        var isAdmin = User.IsInRole("Admin");

        if (!isOwner && !isSale && !isAdmin)
        {
            return NotFound(new { message = "Không tìm thấy booking." });
        }

        var daXacNhan = FixedLengthHelper.PadTo20("DaXacNhan");
        var daThanhToan = await _context.ThanhToans
            .Where(payment =>
                payment.MaBooking == maBookingDb &&
                (payment.TrangThai == thanhCong || payment.TrangThai == daXacNhan))
            .SumAsync(payment => (int?)payment.SoTien) ?? 0;
        var choXacNhan = FixedLengthHelper.PadTo20("ChoXacNhan");
        var dangCho = await _context.ThanhToans
            .Where(p => p.MaBooking == maBookingDb && p.TrangThai == choXacNhan)
            .SumAsync(p => (int?)p.SoTien) ?? 0;

        var tongTien = booking.ThanhTien ?? 0;

        if (isSale && !isOwner)
        {
            return Ok(new
            {
                maBooking = FixedLengthHelper.TrimSafe(booking.MaBooking),
                tongTien,
                daThanhToan,
                dangCho,
                conLai = tongTien - daThanhToan,
                conLaiKhaDung = tongTien - daThanhToan - dangCho
            });
        }

        return Ok(new
        {
            maBooking = FixedLengthHelper.TrimSafe(booking.MaBooking),
            tongTien,
            daThanhToan,
            dangCho,
            conLai = tongTien - daThanhToan,
            conLaiKhaDung = tongTien - daThanhToan - dangCho
        });
    }

    [HttpPost]
    [Authorize(Roles = "KhachHang")]
    public async Task<ActionResult> Create(
        ThanhToanCreateDto request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
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

        idempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey)
            ? null
            : idempotencyKey.Trim();
        if (idempotencyKey?.Length > 100)
            return BadRequest(new { message = "Idempotency-Key không được dài quá 100 ký tự." });

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

        // A retry with the same key returns the original resource before
        // re-running validation or creating another pending payment.
        if (idempotencyKey is not null)
        {
            var existing = await _context.ThanhToans
                .Include(item => item.MaBookingNavigation)
                .FirstOrDefaultAsync(item => item.MaBooking == maBookingDb &&
                    item.IdempotencyKey == idempotencyKey);
            if (existing is not null)
            {
                if (existing.MaBookingNavigation.MaUser != maUserDb)
                    return NotFound(new { message = "Không tìm thấy booking thuộc tài khoản của bạn." });
                return Ok(ToResponse(existing));
            }
        }

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

        if (FixedLengthHelper.TrimSafe(booking.TrangThai) == "DaHuy")
            return BadRequest(new { message = "Booking đã hủy, không thể thanh toán." });

        // PhuongThuc: ChuyenKhoan (cần Sale đối soát sao kê) hoặc TienMat (thu tại quầy/khách sạn).
        // Không hardcode bắt buộc để tương thích dữ liệu cũ, nhưng chuẩn hóa nếu gửi lên.
        var phuongThuc = request.PhuongThuc?.Trim();
        if (!string.IsNullOrWhiteSpace(phuongThuc) &&
            phuongThuc != "ChuyenKhoan" && phuongThuc != "TienMat")
        {
            return BadRequest(new { message = "PhuongThuc phải là ChuyenKhoan hoặc TienMat." });
        }

        var tongTien = booking.ThanhTien ?? 0;

        // Chỉ tính dòng đã được Sale xác nhận (DaXacNhan/ThanhCong) vào số đã thanh toán.
        // Dòng ChoXacNhan chưa được tính — Sale phải duyệt mới trừ vào conLai.
        var daXacNhanStatuses = new[] { thanhCong, FixedLengthHelper.PadTo20("DaXacNhan") };
        var daThanhToan = await _context.ThanhToans
            .Where(payment =>
                payment.MaBooking == maBookingDb &&
                daXacNhanStatuses.Contains(payment.TrangThai))
            .SumAsync(payment => (int?)payment.SoTien) ?? 0;

        // Chặn vượt dư kể cả khi cộng dồn các dòng ChoXacNhan đang chờ
        var choXacNhan = FixedLengthHelper.PadTo20("ChoXacNhan");
        var dangCho = await _context.ThanhToans
            .Where(p => p.MaBooking == maBookingDb && p.TrangThai == choXacNhan)
            .SumAsync(p => (int?)p.SoTien) ?? 0;
        var conLaiThuc = tongTien - daThanhToan;
        var conLaiKhaDung = conLaiThuc - dangCho;

        if (request.SoTien > conLaiKhaDung)
        {
            return BadRequest(new
            {
                message = conLaiKhaDung <= 0
                    ? "Booking đã có đủ yêu cầu thanh toán đang chờ duyệt, vui lòng chờ Sale xác nhận."
                    : $"Số tiền vượt quá phần còn lại cần thanh toán. Còn lại khả dụng: {conLaiKhaDung}"
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

        // Mock thanh toán (không có webhook ngân hàng): khách tạo ChoXacNhan, Sale duyệt mới thành DaXacNhan.
        var paymentEntity = new ThanhToan
        {
            MaTt = maTtDb,
            MaBooking = maBookingDb,
            SoTien = request.SoTien,
            NgayTt = DateTime.UtcNow,
            TrangThai = choXacNhan,
            PhuongThuc = phuongThuc,
            LoaiThanhToan = FixedLengthHelper.PadTo20(loaiThanhToan),
            IdempotencyKey = idempotencyKey
        };

        _context.ThanhToans.Add(paymentEntity);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException) when (idempotencyKey is not null)
        {
            _context.Entry(paymentEntity).State = EntityState.Detached;
            var existing = await _context.ThanhToans
                .Include(item => item.MaBookingNavigation)
                .FirstOrDefaultAsync(item => item.MaBooking == maBookingDb &&
                    item.IdempotencyKey == idempotencyKey);
            if (existing is not null && existing.MaBookingNavigation.MaUser == maUserDb)
                return Ok(ToResponse(existing));
            throw;
        }
        await _hanhViLogger.LogAsync(maUserDb, booking.MaTour, "ThanhToan_ChoXacNhan");

        return StatusCode(StatusCodes.Status201Created, ToResponse(paymentEntity));
    }

    // Sale/Admin xác nhận khoản thanh toán (đối soát sao kê với ChuyenKhoan, thu tiền mặt tại quầy)
    [HttpPut("{maTt}/xac-nhan")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> XacNhan(string maTt)
    {
        var maTtDb = FixedLengthHelper.PadTo20(maTt);
        var payment = await _context.ThanhToans.FirstOrDefaultAsync(p => p.MaTt == maTtDb);
        if (payment is null) return NotFound(new { message = "Không tìm thấy thanh toán." });
        if (FixedLengthHelper.TrimSafe(payment.TrangThai) != "ChoXacNhan")
            return BadRequest(new { message = "Chỉ thanh toán ở trạng thái ChoXacNhan mới được xác nhận." });

        var booking = await _context.DatDichVus.FirstOrDefaultAsync(b => b.MaBooking == payment.MaBooking);
        if (booking is null) return NotFound(new { message = "Không tìm thấy booking của thanh toán." });
        if (FixedLengthHelper.TrimSafe(booking.TrangThai) == "DaHuy")
            return BadRequest(new { message = "Booking đã hủy, không thể xác nhận thanh toán." });

        var thanhCong = FixedLengthHelper.PadTo20("ThanhCong");
        var daXacNhan = FixedLengthHelper.PadTo20("DaXacNhan");
        var daThanhToan = await _context.ThanhToans
            .Where(p => p.MaBooking == payment.MaBooking && (p.TrangThai == thanhCong || p.TrangThai == daXacNhan))
            .SumAsync(p => (int?)p.SoTien) ?? 0;
        if (daThanhToan + payment.SoTien > (booking.ThanhTien ?? 0))
            return BadRequest(new { message = "Xác nhận sẽ làm tổng đã thanh toán vượt quá tổng phải thu." });

        payment.TrangThai = daXacNhan;
        await _context.SaveChangesAsync();
        await _hanhViLogger.LogAsync(booking.MaUser, booking.MaTour, "ThanhToan_DaXacNhan");
        return Ok(new { maTt = FixedLengthHelper.TrimSafe(payment.MaTt), trangThai = FixedLengthHelper.TrimSafe(payment.TrangThai) });
    }

    [HttpPut("{maTt}/tu-choi")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> TuChoi(string maTt, [FromBody] LyDoTuChoiBoiSaleDto? dto)
    {
        var maTtDb = FixedLengthHelper.PadTo20(maTt);
        var payment = await _context.ThanhToans.FirstOrDefaultAsync(p => p.MaTt == maTtDb);
        if (payment is null) return NotFound(new { message = "Không tìm thấy thanh toán." });
        if (FixedLengthHelper.TrimSafe(payment.TrangThai) != "ChoXacNhan")
            return BadRequest(new { message = "Chỉ thanh toán ở trạng thái ChoXacNhan mới được từ chối." });
        payment.TrangThai = FixedLengthHelper.PadTo20("TuChoi");
        await _context.SaveChangesAsync();
        return Ok(new { maTt = FixedLengthHelper.TrimSafe(payment.MaTt), trangThai = FixedLengthHelper.TrimSafe(payment.TrangThai), lyDo = dto?.LyDoTuChoi?.Trim() });
    }

    private string? GetCurrentMaUser()
    {
        return User.FindFirst("MaUser")?.Value;
    }

    private static object ToResponse(ThanhToan payment) => new
    {
        maTt = FixedLengthHelper.TrimSafe(payment.MaTt),
        maBooking = FixedLengthHelper.TrimSafe(payment.MaBooking),
        soTien = payment.SoTien,
        ngayTt = payment.NgayTt,
        trangThai = FixedLengthHelper.TrimSafe(payment.TrangThai),
        phuongThuc = payment.PhuongThuc,
        loaiThanhToan = FixedLengthHelper.TrimSafe(payment.LoaiThanhToan)
    };
}
