using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public ThanhToanController(
        AppDbContext context,
        IHanhViLogger hanhViLogger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _hanhViLogger = hanhViLogger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
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
                loaiThanhToan = FixedLengthHelper.TrimSafe(payment.LoaiThanhToan),
                gateway = payment.Gateway,
                gatewayTxnId = payment.GatewayTxnId,
                paidAt = payment.PaidAt
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

    [HttpPost("tao-phien-cong")]
    [Authorize(Roles = "KhachHang")]
    public async Task<ActionResult> CreateGatewaySession(
        ThanhToanCreateDto request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        var maUser = GetCurrentMaUser();
        if (maUser is null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.MaBooking))
            return BadRequest(new { message = "Mã booking không được để trống." });
        if (request.SoTien <= 0)
            return BadRequest(new { message = "Số tiền thanh toán phải lớn hơn 0." });

        var loaiThanhToan = request.LoaiThanhToan?.Trim();
        var loaiHopLe = new[] { "DatCoc", "ThanhToanConLai", "ThanhToanDu" };
        if (string.IsNullOrWhiteSpace(loaiThanhToan) || !loaiHopLe.Contains(loaiThanhToan))
        {
            return BadRequest(new
            {
                message = "Loại thanh toán không hợp lệ. Giá trị hợp lệ gồm: DatCoc, ThanhToanConLai, ThanhToanDu."
            });
        }

        var phuongThuc = request.PhuongThuc?.Trim();
        if (phuongThuc != "VNPay" && phuongThuc != "MoMo")
            return BadRequest(new { message = "PhuongThuc phải là VNPay hoặc MoMo." });

        idempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey)
            ? null
            : idempotencyKey.Trim();
        if (idempotencyKey?.Length > 100)
            return BadRequest(new { message = "Idempotency-Key không được dài quá 100 ký tự." });

        VnPaySettings? vnPaySettings = null;
        MoMoSettings? moMoSettings = null;
        string missingSettings;
        if (phuongThuc == "VNPay")
        {
            vnPaySettings = GetVnPaySettings(out missingSettings);
        }
        else
        {
            moMoSettings = GetMoMoSettings(out missingSettings);
        }

        if (vnPaySettings is null && moMoSettings is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = $"Cấu hình {phuongThuc} chưa đầy đủ: {missingSettings}."
            });
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maBookingDb = FixedLengthHelper.PadTo20(request.MaBooking);

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
                if (!string.Equals(existing.PhuongThuc?.Trim(), phuongThuc, StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(existing.PayUrl))
                {
                    return Conflict(new
                    {
                        message = "Idempotency-Key đã được dùng cho một yêu cầu thanh toán khác."
                    });
                }
                return Ok(ToGatewayResponse(existing));
            }
        }

        var booking = await _context.DatDichVus
            .FirstOrDefaultAsync(item =>
                item.MaBooking == maBookingDb &&
                item.MaUser == maUserDb);
        if (booking is null)
            return NotFound(new { message = "Không tìm thấy booking thuộc tài khoản của bạn." });
        if (FixedLengthHelper.TrimSafe(booking.TrangThai) == "DaHuy")
            return BadRequest(new { message = "Booking đã hủy, không thể thanh toán." });

        var thanhCong = FixedLengthHelper.PadTo20("ThanhCong");
        var daXacNhan = FixedLengthHelper.PadTo20("DaXacNhan");
        var choXacNhan = FixedLengthHelper.PadTo20("ChoXacNhan");
        var daThanhToan = await _context.ThanhToans
            .Where(payment => payment.MaBooking == maBookingDb &&
                (payment.TrangThai == thanhCong || payment.TrangThai == daXacNhan))
            .SumAsync(payment => (int?)payment.SoTien) ?? 0;
        var dangCho = await _context.ThanhToans
            .Where(payment => payment.MaBooking == maBookingDb && payment.TrangThai == choXacNhan)
            .SumAsync(payment => (int?)payment.SoTien) ?? 0;
        var conLaiKhaDung = (booking.ThanhTien ?? 0) - daThanhToan - dangCho;
        if (request.SoTien > conLaiKhaDung)
        {
            return BadRequest(new
            {
                message = conLaiKhaDung <= 0
                    ? "Booking đã có đủ yêu cầu thanh toán đang chờ xác nhận."
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
        while (await _context.ThanhToans.AnyAsync(payment => payment.MaTt == maTtDb));

        string payUrl;
        if (vnPaySettings is not null)
        {
            payUrl = BuildVnPayUrl(
                vnPaySettings,
                maTt,
                request.SoTien,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
        }
        else
        {
            try
            {
                payUrl = await CreateMoMoPayUrlAsync(
                    moMoSettings!, maTt, request.SoTien, HttpContext.RequestAborted);
            }
            catch (HttpRequestException error)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    message = $"Không tạo được phiên thanh toán MoMo sandbox: {error.Message}"
                });
            }
        }

        var paymentEntity = new ThanhToan
        {
            MaTt = maTtDb,
            MaBooking = maBookingDb,
            SoTien = request.SoTien,
            NgayTt = DateTime.UtcNow,
            TrangThai = choXacNhan,
            PhuongThuc = phuongThuc,
            LoaiThanhToan = FixedLengthHelper.PadTo20(loaiThanhToan),
            IdempotencyKey = idempotencyKey,
            Gateway = phuongThuc,
            GatewayOrderId = maTt,
            PayUrl = payUrl
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
            if (existing is not null && existing.MaBookingNavigation.MaUser == maUserDb &&
                string.Equals(existing.PhuongThuc?.Trim(), phuongThuc, StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(existing.PayUrl))
            {
                return Ok(ToGatewayResponse(existing));
            }
            throw;
        }

        await _hanhViLogger.LogAsync(maUserDb, booking.MaTour, $"ThanhToan_{phuongThuc}_ChoXacNhan");
        return StatusCode(StatusCodes.Status201Created, ToGatewayResponse(paymentEntity));
    }

    [HttpGet("vnpay/return")]
    [AllowAnonymous]
    public async Task<IActionResult> VnPayReturn()
    {
        var settings = GetVnPaySettings(out var missingSettings);
        if (settings is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = $"Cấu hình VNPay chưa đầy đủ: {missingSettings}."
            });
        }

        var signatureValid = VerifyVnPaySignature(Request.Query, settings.HashSecret);
        var orderId = Request.Query["vnp_TxnRef"].ToString();
        var payment = signatureValid && !string.IsNullOrWhiteSpace(orderId)
            ? await _context.ThanhToans.FirstOrDefaultAsync(item =>
                item.GatewayOrderId == orderId && item.PhuongThuc == "VNPay")
            : null;
        var gatewaySuccessful = signatureValid &&
            Request.Query["vnp_ResponseCode"] == "00" &&
            Request.Query["vnp_TransactionStatus"] == "00";

        // Return URL chỉ hiển thị kết quả phía cổng. Trạng thái thanh toán trong DB
        // vẫn là ChoXacNhan cho tới khi IPN hợp lệ được xử lý.
        return Redirect(BuildFrontendRedirect(
            settings.FrontendReturnUrl,
            payment is null ? null : FixedLengthHelper.TrimSafe(payment.MaBooking),
            signatureValid ? (gatewaySuccessful ? "success" : "failed") : "invalid"));
    }

    [HttpGet("vnpay/ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> VnPayIpn()
    {
        var settings = GetVnPaySettings(out var missingSettings);
        if (settings is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                RspCode = "99",
                Message = $"Missing VNPay configuration: {missingSettings}"
            });
        }

        if (!VerifyVnPaySignature(Request.Query, settings.HashSecret))
        {
            return StatusCode(StatusCodes.Status400BadRequest, new
            {
                RspCode = "97",
                Message = "Invalid signature"
            });
        }

        if (Request.Query["vnp_TmnCode"] != settings.TmnCode)
            return Ok(new { RspCode = "01", Message = "Invalid merchant" });

        var orderId = Request.Query["vnp_TxnRef"].ToString();
        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);
        var payment = await _context.ThanhToans
            .FromSqlInterpolated($"""
                SELECT * FROM dbo.ThanhToan WITH (UPDLOCK, ROWLOCK)
                WHERE GatewayOrderId = {orderId} AND PhuongThuc = {"VNPay"}
                """)
            .FirstOrDefaultAsync();
        if (payment is null)
            return Ok(new { RspCode = "01", Message = "Order not found" });

        if (!long.TryParse(Request.Query["vnp_Amount"], NumberStyles.None,
                CultureInfo.InvariantCulture, out var gatewayAmount) ||
            gatewayAmount != (long)(payment.SoTien ?? 0) * 100L)
        {
            return Ok(new { RspCode = "04", Message = "Invalid amount" });
        }

        var gatewayTxnId = Request.Query["vnp_TransactionNo"].ToString();
        var paymentStatus = FixedLengthHelper.TrimSafe(payment.TrangThai);
        if (!string.IsNullOrWhiteSpace(gatewayTxnId) &&
            paymentStatus == "DaXacNhan" &&
            string.Equals(payment.GatewayTxnId?.Trim(), gatewayTxnId, StringComparison.Ordinal))
        {
            await transaction.CommitAsync();
            return Ok(new { RspCode = "02", Message = "Order already confirmed" });
        }
        if (paymentStatus != "ChoXacNhan")
            return Ok(new { RspCode = "01", Message = "Invalid order status" });

        var successful = Request.Query["vnp_ResponseCode"] == "00" &&
            Request.Query["vnp_TransactionStatus"] == "00";
        if (!successful)
        {
            payment.TrangThai = FixedLengthHelper.PadTo20("TuChoi");
            payment.GatewayTxnId = gatewayTxnId;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok(new { RspCode = "00", Message = "Payment result acknowledged" });
        }

        var booking = await _context.DatDichVus
            .FirstOrDefaultAsync(item => item.MaBooking == payment.MaBooking);
        if (booking is null || FixedLengthHelper.TrimSafe(booking.TrangThai) == "DaHuy")
        {
            payment.TrangThai = FixedLengthHelper.PadTo20("TuChoi");
            payment.GatewayTxnId = gatewayTxnId;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok(new { RspCode = "01", Message = "Invalid order" });
        }

        var thanhCong = FixedLengthHelper.PadTo20("ThanhCong");
        var daXacNhan = FixedLengthHelper.PadTo20("DaXacNhan");
        var daThanhToan = await _context.ThanhToans
            .Where(item => item.MaBooking == payment.MaBooking &&
                (item.TrangThai == thanhCong || item.TrangThai == daXacNhan))
            .SumAsync(item => (int?)item.SoTien) ?? 0;
        if (daThanhToan + (payment.SoTien ?? 0) > (booking.ThanhTien ?? 0))
            return Ok(new { RspCode = "04", Message = "Invalid amount" });

        payment.TrangThai = FixedLengthHelper.PadTo20("DaXacNhan");
        payment.GatewayTxnId = gatewayTxnId;
        payment.PaidAt = DateTime.UtcNow;
        var bookingStatus = FixedLengthHelper.TrimSafe(booking.TrangThai);
        if (daThanhToan + (payment.SoTien ?? 0) >= (booking.ThanhTien ?? 0) &&
            bookingStatus is "ChoXacNhan" or "DaXacNhan")
        {
            booking.TrangThai = FixedLengthHelper.PadTo20("DaThanhToan");
        }
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        await _hanhViLogger.LogAsync(booking.MaUser, booking.MaTour, "ThanhToan_DaXacNhan");

        return Ok(new { RspCode = "00", Message = "Confirm success" });
    }

    [HttpGet("momo/return")]
    [AllowAnonymous]
    public async Task<IActionResult> MoMoReturn()
    {
        var settings = GetMoMoSettings(out var missingSettings);
        if (settings is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = $"Cấu hình MoMo chưa đầy đủ: {missingSettings}."
            });
        }

        var values = Request.Query.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToString(),
            StringComparer.Ordinal);
        var signatureValid = VerifyMoMoSignature(values, settings);
        values.TryGetValue("orderId", out var orderId);
        var payment = signatureValid && !string.IsNullOrWhiteSpace(orderId)
            ? await _context.ThanhToans.FirstOrDefaultAsync(item =>
                item.GatewayOrderId == orderId && item.PhuongThuc == "MoMo")
            : null;
        var gatewaySuccessful = signatureValid &&
            values.TryGetValue("resultCode", out var resultCode) && resultCode == "0";

        // MoMo redirect không được dùng để ghi nhận tiền; IPN mới có quyền xác nhận.
        return Redirect(BuildFrontendRedirect(
            settings.FrontendReturnUrl,
            payment is null ? null : FixedLengthHelper.TrimSafe(payment.MaBooking),
            signatureValid ? (gatewaySuccessful ? "success" : "failed") : "invalid"));
    }

    [HttpPost("momo/ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> MoMoIpn([FromBody] JsonElement payload)
    {
        var settings = GetMoMoSettings(out var missingSettings);
        if (settings is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = $"Cấu hình MoMo chưa đầy đủ: {missingSettings}."
            });
        }

        var values = JsonObjectToStrings(payload);
        if (!VerifyMoMoSignature(values, settings))
            return BadRequest(new { message = "Chữ ký MoMo không hợp lệ." });

        values.TryGetValue("orderId", out var orderId);
        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);
        var payment = await _context.ThanhToans
            .FromSqlInterpolated($"""
                SELECT * FROM dbo.ThanhToan WITH (UPDLOCK, ROWLOCK)
                WHERE GatewayOrderId = {orderId} AND PhuongThuc = {"MoMo"}
                """)
            .FirstOrDefaultAsync();
        if (payment is null)
            return NotFound(new { message = "Không tìm thấy giao dịch MoMo." });

        if (!values.TryGetValue("amount", out var amountText) ||
            !long.TryParse(amountText, NumberStyles.None, CultureInfo.InvariantCulture, out var gatewayAmount) ||
            gatewayAmount != payment.SoTien)
        {
            return BadRequest(new { message = "Số tiền MoMo không khớp." });
        }

        var gatewayTxnId = values.GetValueOrDefault("transId");
        var paymentStatus = FixedLengthHelper.TrimSafe(payment.TrangThai);
        if (!string.IsNullOrWhiteSpace(gatewayTxnId) &&
            paymentStatus == "DaXacNhan" &&
            string.Equals(payment.GatewayTxnId?.Trim(), gatewayTxnId, StringComparison.Ordinal))
        {
            await transaction.CommitAsync();
            return NoContent();
        }
        if (paymentStatus != "ChoXacNhan")
            return BadRequest(new { message = "Trạng thái giao dịch MoMo không hợp lệ." });
        if (!values.TryGetValue("resultCode", out var resultCode) || resultCode != "0")
        {
            payment.TrangThai = FixedLengthHelper.PadTo20("TuChoi");
            payment.GatewayTxnId = gatewayTxnId;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return NoContent();
        }

        var booking = await _context.DatDichVus
            .FirstOrDefaultAsync(item => item.MaBooking == payment.MaBooking);
        if (booking is null || FixedLengthHelper.TrimSafe(booking.TrangThai) == "DaHuy")
        {
            payment.TrangThai = FixedLengthHelper.PadTo20("TuChoi");
            payment.GatewayTxnId = gatewayTxnId;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return BadRequest(new { message = "Booking không hợp lệ hoặc đã hủy." });
        }

        var thanhCong = FixedLengthHelper.PadTo20("ThanhCong");
        var daXacNhan = FixedLengthHelper.PadTo20("DaXacNhan");
        var daThanhToan = await _context.ThanhToans
            .Where(item => item.MaBooking == payment.MaBooking &&
                (item.TrangThai == thanhCong || item.TrangThai == daXacNhan))
            .SumAsync(item => (int?)item.SoTien) ?? 0;
        if (daThanhToan + (payment.SoTien ?? 0) > (booking.ThanhTien ?? 0))
            return BadRequest(new { message = "Xác nhận sẽ làm tổng đã thanh toán vượt quá tổng phải thu." });

        payment.TrangThai = FixedLengthHelper.PadTo20("DaXacNhan");
        payment.GatewayTxnId = gatewayTxnId;
        payment.PaidAt = DateTime.UtcNow;
        var bookingStatus = FixedLengthHelper.TrimSafe(booking.TrangThai);
        if (daThanhToan + (payment.SoTien ?? 0) >= (booking.ThanhTien ?? 0) &&
            bookingStatus is "ChoXacNhan" or "DaXacNhan")
        {
            booking.TrangThai = FixedLengthHelper.PadTo20("DaThanhToan");
        }
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        await _hanhViLogger.LogAsync(booking.MaUser, booking.MaTour, "ThanhToan_DaXacNhan");

        return NoContent();
    }

    // Sale/Admin xác nhận khoản thanh toán (đối soát sao kê với ChuyenKhoan, thu tiền mặt tại quầy)
    [HttpPut("{maTt}/xac-nhan")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> XacNhan(string maTt)
    {
        var maTtDb = FixedLengthHelper.PadTo20(maTt);
        var payment = await _context.ThanhToans.FirstOrDefaultAsync(p => p.MaTt == maTtDb);
        if (payment is null) return NotFound(new { message = "Không tìm thấy thanh toán." });
        if (IsGatewayPayment(payment))
            return BadRequest(new { message = "Giao dịch cổng do IPN xác nhận." });
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
        if (IsGatewayPayment(payment))
            return BadRequest(new { message = "Giao dịch cổng do IPN xác nhận." });
        if (FixedLengthHelper.TrimSafe(payment.TrangThai) != "ChoXacNhan")
            return BadRequest(new { message = "Chỉ thanh toán ở trạng thái ChoXacNhan mới được từ chối." });
        payment.TrangThai = FixedLengthHelper.PadTo20("TuChoi");
        await _context.SaveChangesAsync();
        return Ok(new { maTt = FixedLengthHelper.TrimSafe(payment.MaTt), trangThai = FixedLengthHelper.TrimSafe(payment.TrangThai), lyDo = dto?.LyDoTuChoi?.Trim() });
    }

    private VnPaySettings? GetVnPaySettings(out string missingSettings)
    {
        var values = new Dictionary<string, string?>
        {
            ["TmnCode"] = _configuration["VnPay:TmnCode"],
            ["HashSecret"] = _configuration["VnPay:HashSecret"],
            ["BaseUrl"] = _configuration["VnPay:BaseUrl"],
            ["ReturnUrl"] = _configuration["VnPay:ReturnUrl"],
            ["IpnUrl"] = _configuration["VnPay:IpnUrl"],
            ["FrontendReturnUrl"] = _configuration["VnPay:FrontendReturnUrl"]
        };
        missingSettings = string.Join(", ", values
            .Where(item => string.IsNullOrWhiteSpace(item.Value))
            .Select(item => $"VnPay:{item.Key}"));
        if (missingSettings.Length > 0)
            return null;

        return new VnPaySettings(
            values["TmnCode"]!.Trim(),
            values["HashSecret"]!.Trim(),
            values["BaseUrl"]!.Trim(),
            values["ReturnUrl"]!.Trim(),
            values["IpnUrl"]!.Trim(),
            values["FrontendReturnUrl"]!.Trim());
    }

    private MoMoSettings? GetMoMoSettings(out string missingSettings)
    {
        var values = new Dictionary<string, string?>
        {
            ["PartnerCode"] = _configuration["MoMo:PartnerCode"],
            ["AccessKey"] = _configuration["MoMo:AccessKey"],
            ["SecretKey"] = _configuration["MoMo:SecretKey"],
            ["Endpoint"] = _configuration["MoMo:Endpoint"],
            ["RequestType"] = _configuration["MoMo:RequestType"],
            ["RedirectUrl"] = _configuration["MoMo:RedirectUrl"],
            ["IpnUrl"] = _configuration["MoMo:IpnUrl"],
            ["FrontendReturnUrl"] = _configuration["MoMo:FrontendReturnUrl"]
        };
        missingSettings = string.Join(", ", values
            .Where(item => string.IsNullOrWhiteSpace(item.Value))
            .Select(item => $"MoMo:{item.Key}"));
        if (missingSettings.Length > 0)
            return null;

        return new MoMoSettings(
            values["PartnerCode"]!.Trim(),
            values["AccessKey"]!.Trim(),
            values["SecretKey"]!.Trim(),
            values["Endpoint"]!.Trim(),
            values["RequestType"]!.Trim(),
            values["RedirectUrl"]!.Trim(),
            values["IpnUrl"]!.Trim(),
            values["FrontendReturnUrl"]!.Trim());
    }

    private static string BuildVnPayUrl(
        VnPaySettings settings,
        string orderId,
        int amount,
        string ipAddress)
    {
        if (!IPAddress.TryParse(ipAddress, out var parsedIp) ||
            parsedIp.AddressFamily != AddressFamily.InterNetwork)
        {
            ipAddress = "127.0.0.1";
        }

        var vietnamTime = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7));
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = settings.TmnCode,
            ["vnp_Amount"] = ((long)amount * 100L).ToString(CultureInfo.InvariantCulture),
            ["vnp_CreateDate"] = vietnamTime.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            ["vnp_CurrCode"] = "VND",
            ["vnp_IpAddr"] = ipAddress,
            ["vnp_Locale"] = "vn",
            ["vnp_OrderInfo"] = $"Thanh toan booking {orderId}",
            ["vnp_OrderType"] = "other",
            ["vnp_ReturnUrl"] = settings.ReturnUrl,
            ["vnp_TxnRef"] = orderId,
            ["vnp_ExpireDate"] = vietnamTime.AddMinutes(15)
                .ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)
        };
        var signData = BuildVnPayQuery(parameters);
        var signature = ComputeHmacHex(HashAlgorithmName.SHA512, settings.HashSecret, signData);
        var separator = settings.BaseUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{settings.BaseUrl}{separator}{signData}&vnp_SecureHash={signature}";
    }

    private async Task<string> CreateMoMoPayUrlAsync(
        MoMoSettings settings,
        string orderId,
        int amount,
        CancellationToken cancellationToken)
    {
        var requestId = orderId;
        const string extraData = "";
        var orderInfo = $"Thanh toan booking {orderId}";
        var rawSignature =
            $"accessKey={settings.AccessKey}&amount={amount}&extraData={extraData}" +
            $"&ipnUrl={settings.IpnUrl}&orderId={orderId}&orderInfo={orderInfo}" +
            $"&partnerCode={settings.PartnerCode}&redirectUrl={settings.RedirectUrl}" +
            $"&requestId={requestId}&requestType={settings.RequestType}";
        var signature = ComputeHmacHex(HashAlgorithmName.SHA256, settings.SecretKey, rawSignature);
        var payload = new
        {
            partnerCode = settings.PartnerCode,
            requestId,
            amount,
            orderId,
            orderInfo,
            redirectUrl = settings.RedirectUrl,
            ipnUrl = settings.IpnUrl,
            requestType = settings.RequestType,
            extraData,
            lang = "vi",
            signature
        };

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var response = await client.PostAsJsonAsync(settings.Endpoint, payload, cancellationToken);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            var resultCode = GetJsonText(result, "resultCode");
            var payUrl = GetJsonText(result, "payUrl");
            if (resultCode != "0" || string.IsNullOrWhiteSpace(payUrl))
            {
                var message = GetJsonText(result, "message");
                throw new HttpRequestException(
                    $"MoMo trả resultCode={resultCode}, message={message}.");
            }
            return payUrl;
        }
        catch (Exception error) when (error is not HttpRequestException &&
                                      error is not OperationCanceledException)
        {
            throw new HttpRequestException("Phản hồi MoMo không hợp lệ.", error);
        }
    }

    private static bool VerifyVnPaySignature(
        IQueryCollection query,
        string secret)
    {
        var supplied = query["vnp_SecureHash"].ToString();
        if (string.IsNullOrWhiteSpace(supplied))
            return false;

        var parameters = query
            .Where(item => item.Key.StartsWith("vnp_", StringComparison.Ordinal) &&
                item.Key != "vnp_SecureHash" && item.Key != "vnp_SecureHashType")
            .ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.Ordinal);
        var signData = BuildVnPayQuery(parameters);
        var expected = ComputeHmacHex(HashAlgorithmName.SHA512, secret, signData);
        return FixedTimeHexEquals(expected, supplied);
    }

    private static string BuildVnPayQuery(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        return string.Join("&", parameters
            .OrderBy(item => item.Key, VnPayKeyComparer)
            .Where(item => !string.IsNullOrEmpty(item.Value))
            .Select(item => $"{VnPayUrlEncode(item.Key)}={VnPayUrlEncode(item.Value)}"));
    }

    private static readonly CompareInfo VnPayCompareInfo = CompareInfo.GetCompareInfo("en-US");
    private static readonly IComparer<string> VnPayKeyComparer = Comparer<string>.Create(
        (left, right) => VnPayCompareInfo.Compare(left, right, CompareOptions.Ordinal));

    private static string VnPayUrlEncode(string value)
    {
        var encoded = WebUtility.UrlEncode(value);
        var result = new StringBuilder(encoded.Length);
        for (var index = 0; index < encoded.Length; index++)
        {
            if (encoded[index] == '%' && index + 2 < encoded.Length)
            {
                result.Append('%');
                result.Append(char.ToUpperInvariant(encoded[index + 1]));
                result.Append(char.ToUpperInvariant(encoded[index + 2]));
                index += 2;
                continue;
            }
            result.Append(encoded[index]);
        }
        return result.ToString();
    }

    private static bool VerifyMoMoSignature(
        IReadOnlyDictionary<string, string> values,
        MoMoSettings settings)
    {
        if (!values.TryGetValue("signature", out var supplied) ||
            !values.TryGetValue("partnerCode", out var partnerCode) ||
            !string.Equals(partnerCode, settings.PartnerCode, StringComparison.Ordinal))
        {
            return false;
        }

        string Value(string key) => values.GetValueOrDefault(key) ?? string.Empty;
        var rawSignature =
            $"accessKey={settings.AccessKey}&amount={Value("amount")}&extraData={Value("extraData")}" +
            $"&message={Value("message")}&orderId={Value("orderId")}&orderInfo={Value("orderInfo")}" +
            $"&orderType={Value("orderType")}&partnerCode={Value("partnerCode")}" +
            $"&payType={Value("payType")}&requestId={Value("requestId")}" +
            $"&responseTime={Value("responseTime")}&resultCode={Value("resultCode")}" +
            $"&transId={Value("transId")}";
        var expected = ComputeHmacHex(HashAlgorithmName.SHA256, settings.SecretKey, rawSignature);
        return FixedTimeHexEquals(expected, supplied);
    }

    private static string ComputeHmacHex(HashAlgorithmName algorithm, string secret, string data)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var bytes = Encoding.UTF8.GetBytes(data);
        using HMAC hmac = algorithm == HashAlgorithmName.SHA512
            ? new HMACSHA512(key)
            : new HMACSHA256(key);
        var hash = hmac.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool FixedTimeHexEquals(string expected, string supplied)
    {
        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(expected),
                Convert.FromHexString(supplied));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static Dictionary<string, string> JsonObjectToStrings(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
            return new Dictionary<string, string>(StringComparer.Ordinal);

        return payload.EnumerateObject().ToDictionary(
            property => property.Name,
            property => property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString() ?? string.Empty
                : property.Value.GetRawText(),
            StringComparer.Ordinal);
    }

    private static string GetJsonText(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty(propertyName, out var value))
        {
            return string.Empty;
        }
        return value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : value.GetRawText();
    }

    private static string BuildFrontendRedirect(
        string frontendBaseUrl,
        string? maBooking,
        string status)
    {
        var target = frontendBaseUrl.TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(maBooking))
            target += $"/booking/{Uri.EscapeDataString(maBooking)}";
        var separator = target.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        var paid = status == "success" ? "1" : "0";
        var bookingQuery = string.IsNullOrWhiteSpace(maBooking)
            ? string.Empty
            : $"maBooking={Uri.EscapeDataString(maBooking)}&";
        return $"{target}{separator}{bookingQuery}status={Uri.EscapeDataString(status)}&paid={paid}";
    }

    private static bool IsGatewayPayment(ThanhToan payment)
    {
        var method = payment.PhuongThuc?.Trim();
        return method == "VNPay" || method == "MoMo";
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
        loaiThanhToan = FixedLengthHelper.TrimSafe(payment.LoaiThanhToan),
        gateway = payment.Gateway,
        gatewayTxnId = payment.GatewayTxnId,
        gatewayOrderId = payment.GatewayOrderId,
        payUrl = payment.PayUrl,
        paidAt = payment.PaidAt
    };

    private static object ToGatewayResponse(ThanhToan payment) => new
    {
        maTt = FixedLengthHelper.TrimSafe(payment.MaTt),
        phuongThuc = payment.PhuongThuc,
        payUrl = payment.PayUrl,
        trangThai = FixedLengthHelper.TrimSafe(payment.TrangThai)
    };

    private sealed record VnPaySettings(
        string TmnCode,
        string HashSecret,
        string BaseUrl,
        string ReturnUrl,
        string IpnUrl,
        string FrontendReturnUrl);

    private sealed record MoMoSettings(
        string PartnerCode,
        string AccessKey,
        string SecretKey,
        string Endpoint,
        string RequestType,
        string RedirectUrl,
        string IpnUrl,
        string FrontendReturnUrl);
}
