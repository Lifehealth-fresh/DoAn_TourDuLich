using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourDuLich.API.DTOs;
using TourDuLich.API.Services;
using TourDuLich.Application.Helpers;
using TourDuLich.Application.Services;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DatDichVuController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHanhViLogger _hanhViLogger;

    public DatDichVuController(AppDbContext context, IHanhViLogger hanhViLogger)
    {
        _context = context;
        _hanhViLogger = hanhViLogger;
    }

    [HttpGet("cua-toi")]
    [Authorize(Roles = "KhachHang")]
    public async Task<ActionResult> GetMyBookings([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);

        var query = _context.DatDichVus.AsNoTracking()
            .Where(booking => booking.MaUser == maUserDb)
            ;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var totalCount = await query.CountAsync();
        var bookings = await query.OrderByDescending(booking => booking.NgayDat).ThenBy(booking => booking.MaBooking)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(booking => new
            {
                maBooking = FixedLengthHelper.TrimSafe(booking.MaBooking),
                maTour = FixedLengthHelper.TrimSafe(booking.MaTour),
                maKhachHang = FixedLengthHelper.TrimSafe(booking.MaKhachHang),
                tenTour = booking.MaTourNavigation.TenTour,
                maKhoiHanh = FixedLengthHelper.TrimSafe(booking.MaKhoiHanh),
                ngayDat = booking.NgayDat,
                slnguoiLon = booking.SlnguoiLon,
                sltreEm = booking.SltreEm,
                tongTien = booking.TongTien,
                tongGiamGia = booking.TongGiamGia,
                thanhTien = booking.ThanhTien,
                tyLePhatHuy = booking.TyLePhatHuy,
                soTienPhatHuy = booking.SoTienPhatHuy,
                trangThai = FixedLengthHelper.TrimSafe(booking.TrangThai)
            })
            .ToListAsync();

        return Ok(new { items = bookings, page, pageSize, totalCount });
    }

    [HttpGet("danh-sach")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> GetAllForStaff(
        [FromQuery] string? trangThai = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var daXacNhan = FixedLengthHelper.PadTo20("DaXacNhan");
        var thanhCong = FixedLengthHelper.PadTo20("ThanhCong");
        var query = _context.DatDichVus.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(trangThai))
        {
            query = query.Where(item => item.TrangThai == FixedLengthHelper.PadTo20(trangThai));
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var totalCount = await query.CountAsync();
        var bookings = await query
            .OrderByDescending(item => item.NgayDat)
            .ThenByDescending(item => item.MaBooking)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new
            {
                maBooking = FixedLengthHelper.TrimSafe(item.MaBooking),
                maTour = FixedLengthHelper.TrimSafe(item.MaTour),
                maKhachHang = FixedLengthHelper.TrimSafe(item.MaKhachHang),
                tenTour = item.MaTourNavigation.TenTour,
                hoTen = item.MaKhachHangNavigation == null
                    ? null
                    : (item.MaKhachHangNavigation.Ho + " " + item.MaKhachHangNavigation.Ten).Trim(),
                soDienThoai = item.MaKhachHangNavigation != null
                    ? FixedLengthHelper.TrimSafe(item.MaKhachHangNavigation.SoDienThoai)
                    : FixedLengthHelper.TrimSafe(item.MaUserNavigation.SoDienThoai),
                maKhoiHanh = FixedLengthHelper.TrimSafe(item.MaKhoiHanh),
                ngayKhoiHanh = item.MaKhoiHanhNavigation != null ? item.MaKhoiHanhNavigation.NgayKhoiHanh : null,
                ngayDat = item.NgayDat,
                slnguoiLon = item.SlnguoiLon,
                sltreEm = item.SltreEm,
                tongTien = item.TongTien,
                tongGiamGia = item.TongGiamGia,
                thanhTien = item.ThanhTien,
                tongDaThanhToan = item.ThanhToans
                    .Where(payment => payment.TrangThai == daXacNhan || payment.TrangThai == thanhCong)
                    .Sum(payment => (long?)payment.SoTien) ?? 0L,
                conLai = (item.ThanhTien ?? 0) - (item.ThanhToans
                    .Where(payment => payment.TrangThai == daXacNhan || payment.TrangThai == thanhCong)
                    .Sum(payment => (long?)payment.SoTien) ?? 0L),
                trangThai = FixedLengthHelper.TrimSafe(item.TrangThai)
            })
            .ToListAsync();

        return Ok(new { items = bookings, page, pageSize, totalCount });
    }

    [HttpGet("{maBooking}")]
    [Authorize(Roles = "KhachHang,Sale,Admin")]
    public async Task<ActionResult> GetMyBooking(string maBooking)
    {
        var daXacNhan = FixedLengthHelper.PadTo20("DaXacNhan");
        var thanhCong = FixedLengthHelper.PadTo20("ThanhCong");
        var isStaff = User.IsInRole("Sale") || User.IsInRole("Admin");
        var maUser = GetCurrentMaUser();

        if (!isStaff && maUser is null)
        {
            return Unauthorized();
        }

        var maBookingDb = FixedLengthHelper.PadTo20(maBooking);
        var query = _context.DatDichVus.Where(item => item.MaBooking == maBookingDb);

        if (!isStaff)
        {
            query = query.Where(item => item.MaUser == FixedLengthHelper.PadTo20(maUser!));
        }

        var booking = await query
            .Select(item => new
            {
                maBooking = FixedLengthHelper.TrimSafe(item.MaBooking),
                maTour = FixedLengthHelper.TrimSafe(item.MaTour),
                maKhachHang = FixedLengthHelper.TrimSafe(item.MaKhachHang),
                tenTour = item.MaTourNavigation.TenTour,
                hoTen = item.MaKhachHangNavigation == null
                    ? null
                    : (item.MaKhachHangNavigation.Ho + " " + item.MaKhachHangNavigation.Ten).Trim(),
                soDienThoai = item.MaKhachHangNavigation != null
                    ? FixedLengthHelper.TrimSafe(item.MaKhachHangNavigation.SoDienThoai)
                    : FixedLengthHelper.TrimSafe(item.MaUserNavigation.SoDienThoai),
                maKhoiHanh = FixedLengthHelper.TrimSafe(item.MaKhoiHanh),
                ngayKhoiHanh = item.MaKhoiHanhNavigation != null ? item.MaKhoiHanhNavigation.NgayKhoiHanh : null,
                ngayKetThuc = item.MaKhoiHanhNavigation != null ? item.MaKhoiHanhNavigation.NgayKetThuc : null,
                diaDiem = item.MaKhoiHanhNavigation != null ? item.MaKhoiHanhNavigation.DiaDiem : null,
                ngayDat = item.NgayDat,
                slnguoiLon = item.SlnguoiLon,
                sltreEm = item.SltreEm,
                tongTien = item.TongTien,
                tongGiamGia = item.TongGiamGia,
                thanhTien = item.ThanhTien,
                tongDaThanhToan = item.ThanhToans
                    .Where(payment => payment.TrangThai == daXacNhan || payment.TrangThai == thanhCong)
                    .Sum(payment => (long?)payment.SoTien) ?? 0L,
                conLai = (item.ThanhTien ?? 0) - (item.ThanhToans
                    .Where(payment => payment.TrangThai == daXacNhan || payment.TrangThai == thanhCong)
                    .Sum(payment => (long?)payment.SoTien) ?? 0L),
                tyLePhatHuy = item.TyLePhatHuy,
                soTienPhatHuy = item.SoTienPhatHuy,
                trangThai = FixedLengthHelper.TrimSafe(item.TrangThai)
            })
            .FirstOrDefaultAsync();

        if (booking is null)
        {
            return NotFound(new { message = isStaff ? "Không tìm thấy booking." : "Không tìm thấy booking của bạn." });
        }

        return Ok(booking);
    }

    [HttpGet("{maBooking}/ho-so-khach")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> GetGuestProfile(string maBooking)
    {
        var key = FixedLengthHelper.PadTo20(maBooking);
        var booking = await _context.DatDichVus.AsNoTracking().Where(b => b.MaBooking == key)
            .Select(b => new { b.MaBooking, b.MaUser, b.MaKhachHang }).FirstOrDefaultAsync();
        if (booking is null) return NotFound(new { message = "Không tìm thấy booking." });
        var profile = await _context.KhachHangs.AsNoTracking().Include(k => k.GiayTos)
            .Where(k => booking.MaKhachHang != null ? k.MaKhachHang == booking.MaKhachHang : k.MaUser == booking.MaUser)
            .OrderBy(k => k.MaKhachHang).FirstOrDefaultAsync();
        return Ok(new
        {
            maBooking = booking.MaBooking.Trim(), maKhachHang = profile?.MaKhachHang.Trim(),
            ho = profile?.Ho.Trim(), ten = profile?.Ten.Trim(),
            soDienThoai = profile?.SoDienThoai.Trim(), email = profile?.Email?.Trim(),
            ngaySinh = profile?.NgaySinh, quocTich = profile?.QuocTich?.Trim(),
            giayTo = (profile?.GiayTos ?? []).Select(g => new
            {
                loaiGiayTo = g.LoaiGiayTo.Trim(), soTrenGiayTo = g.SoTrenGiayTo.Trim(),
                ngayCap = g.NgayCap, ngayHetHan = g.NgayHetHan, noiCap = g.NoiCap.Trim()
            }).ToArray()
        });
    }

    [HttpPost]
    [Authorize(Roles = "KhachHang")]
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
            (long)request.SlnguoiLon + request.SltreEm <= 0 ||
            (long)request.SlnguoiLon + request.SltreEm > int.MaxValue)
        {
            return BadRequest(new
            {
                message = "Số người lớn và trẻ em phải hợp lệ, tổng số khách phải lớn hơn 0."
            });
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maTourDb = FixedLengthHelper.PadTo20(request.MaTour);
        var maKhoiHanhDb = FixedLengthHelper.PadTo20(request.MaKhoiHanh);

        KhachHang? khachHang;
        if (!string.IsNullOrWhiteSpace(request.MaKhachHang))
        {
            var maKhachHangDb = FixedLengthHelper.PadTo20(request.MaKhachHang);
            khachHang = await _context.KhachHangs
                .Include(item => item.GiayTos)
                .FirstOrDefaultAsync(item =>
                    item.MaKhachHang == maKhachHangDb &&
                    item.MaUser == maUserDb);

            if (khachHang is null)
            {
                return BadRequest(new
                {
                    message = "Hồ sơ khách hàng không hợp lệ hoặc không thuộc tài khoản của bạn."
                });
            }
        }
        else
        {
            khachHang = await _context.KhachHangs
                .Include(item => item.GiayTos)
                .Where(item => item.MaUser == maUserDb)
                .OrderBy(item => item.MaKhachHang)
                .FirstOrDefaultAsync();
        }

        var tongSoKhach = request.SlnguoiLon + request.SltreEm;

        await using var transaction =
            await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        // Lock Tour for the fallback capacity and price, then the departure.
        var tourLocked = await _context.Tours
            .FromSqlRaw("SELECT * FROM [Tour] WITH (UPDLOCK, HOLDLOCK) WHERE [MaTour] = {0}", maTourDb)
            .FirstOrDefaultAsync();
        if (tourLocked is null)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { message = "Tour không tồn tại." });
        }

        var departure = await _context.LichKhoiHanhs
            .FromSqlRaw("SELECT * FROM dbo.LichKhoiHanh WITH (UPDLOCK, HOLDLOCK) WHERE MaKhoiHanh = {0}", maKhoiHanhDb)
            .FirstOrDefaultAsync();
        if (departure is null || departure.MaTour != maTourDb)
            return BadRequest(new { message = "Lịch khởi hành không tồn tại hoặc không thuộc tour này." });
        if (!departure.NgayKhoiHanh.HasValue || departure.NgayKhoiHanh.Value <= DateTime.UtcNow)
            return BadRequest(new { message = "Lịch khởi hành đã qua, không thể đặt." });
        if (tourLocked.TrangThai?.Trim() != "HoatDong")
            return BadRequest(new { message = "Tour hiện không mở bán." });
        if (tourLocked.LoaiTour.Trim() == "TuThietKe")
        {
            var approved = FixedLengthHelper.PadTo20("DaDuyet");
            if (!await _context.YeuCauThietKes.AnyAsync(r => r.MaTourTao == maTourDb && r.MaUser == maUserDb && r.TrangThai == approved))
                return BadRequest(new { message = "Chỉ chủ yêu cầu tự thiết kế đã được duyệt mới được đặt tour này." });
        }
        var capacity = departure.SoCho ?? tourLocked.Slkhach;
        var tongSoKhachDaDat = await DepartureAvailability.HeldBookings(_context.DatDichVus)
            .Where(b => b.MaKhoiHanh == maKhoiHanhDb)
            .SumAsync(b => (long?)(b.SlnguoiLon ?? 0) + (b.SltreEm ?? 0)) ?? 0L;

        if (tongSoKhachDaDat + tongSoKhach > capacity)
        {
            await transaction.RollbackAsync();
            return BadRequest(new
            {
                message = "Lịch khởi hành không đủ chỗ trống."
            });
        }

        int tongTien;

        try
        {
            tongTien = checked(tourLocked.GiaTour * tongSoKhach);
        }
        catch (OverflowException)
        {
            await transaction.RollbackAsync();
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
            MaKhachHang = khachHang?.MaKhachHang,
            NgayDat = DateOnly.FromDateTime(DateTime.UtcNow),
            SlnguoiLon = request.SlnguoiLon,
            SltreEm = request.SltreEm,
            TongTien = tongTien,
            TongGiamGia = 0,
            ThanhTien = tongTien,
            TrangThai = FixedLengthHelper.PadTo20("ChoXacNhan")
        };

        _context.DatDichVus.Add(booking);

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
            MaHopDong = await GenerateMaHopDongAsync(),
            MaBooking = maBookingDb,
            SoHopDong = $"HD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..50],
            NgayKy = null,
            DieuKhoanCamKet = tourLocked.DieuKhoan,
            FileHopDongUrl = null,
            NguoiDaiDien = null,
            TrangThai = FixedLengthHelper.PadTo20("DuThao"),
            HoTenKhach = hoTenKhach,
            LoaiGiayTo = giayToMoiNhat?.LoaiGiayTo,
            SoGiayTo = giayToMoiNhat?.SoTrenGiayTo
        };
        _context.HopDongs.Add(hopDong);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        await _hanhViLogger.LogAsync(
            maUserDb,
            booking.MaTour,
            "DatTour");

        return StatusCode(StatusCodes.Status201Created, new
        {
            maBooking = FixedLengthHelper.TrimSafe(booking.MaBooking),
            maTour = FixedLengthHelper.TrimSafe(booking.MaTour),
            maKhachHang = FixedLengthHelper.TrimSafe(booking.MaKhachHang),
            maKhoiHanh = FixedLengthHelper.TrimSafe(booking.MaKhoiHanh),
            ngayDat = booking.NgayDat,
            slnguoiLon = booking.SlnguoiLon,
            sltreEm = booking.SltreEm,
            tongTien = booking.TongTien,
            tongGiamGia = booking.TongGiamGia,
            thanhTien = booking.ThanhTien,
            tyLePhatHuy = booking.TyLePhatHuy,
            soTienPhatHuy = booking.SoTienPhatHuy,
            trangThai = FixedLengthHelper.TrimSafe(booking.TrangThai),
            maHopDong = FixedLengthHelper.TrimSafe(hopDong.MaHopDong)
        });
    }

    [HttpPut("{maBooking}/trang-thai")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> UpdateStatus(
        string maBooking,
        DatDichVuTrangThaiDto request)
    {
        var maBookingDb = FixedLengthHelper.PadTo20(maBooking);
        var trangThaiMoi = request.TrangThai?.Trim();

        if (string.IsNullOrWhiteSpace(trangThaiMoi))
        {
            return BadRequest(new { message = "Trạng thái không được để trống." });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var booking = await LockBookingForSeatTransitionAsync(maBookingDb);

        if (booking is null)
        {
            return NotFound(new { message = "Không tìm thấy booking." });
        }

        var trangThaiCu = FixedLengthHelper.TrimSafe(booking.TrangThai) ?? string.Empty;
        var hopLe = (trangThaiCu, trangThaiMoi) switch
        {
            ("ChoXacNhan", "DaXacNhan") => true,
            ("ChoXacNhan", "DaHuy") => true,
            ("DaXacNhan", "DaThanhToan") => true,
            ("DaXacNhan", "DaHuy") => true,
            ("DaThanhToan", "HoanThanh") => true,
            _ => false
        };

        if (!hopLe)
        {
            return BadRequest(new
            {
                message = $"Không thể chuyển từ trạng thái {trangThaiCu} sang {trangThaiMoi}."
            });
        }

        var daXacNhan = FixedLengthHelper.PadTo20("DaXacNhan");
        var thanhCong = FixedLengthHelper.PadTo20("ThanhCong");
        var tongDaTra = await _context.ThanhToans
            .Where(payment => payment.MaBooking == maBookingDb &&
                (payment.TrangThai == daXacNhan || payment.TrangThai == thanhCong))
            .SumAsync(payment => (long?)payment.SoTien) ?? 0L;
        var conLai = (booking.ThanhTien ?? 0) - tongDaTra;

        if (trangThaiCu == "ChoXacNhan" && trangThaiMoi == "DaXacNhan" && tongDaTra <= 0)
            return BadRequest(new { message = "Khách chưa thanh toán (cọc hoặc hết), không thể xác nhận." });

        if (trangThaiCu == "DaXacNhan" && trangThaiMoi == "DaThanhToan" && conLai > 0)
            return BadRequest(new { message = $"Khách chưa thanh toán đủ. Đã trả {tongDaTra}, còn {conLai}." });

        if (trangThaiMoi == "DaHuy" && tongDaTra > 0)
            return BadRequest(new { message = "Khách đã thanh toán. Chờ khách gửi hủy rồi bấm Xác nhận hoàn tiền." });


        booking.TrangThai = FixedLengthHelper.PadTo20(trangThaiMoi);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        if (trangThaiMoi == "HoanThanh")
        {
            await _hanhViLogger.LogAsync(
                booking.MaUser,
                booking.MaTour,
                "HoanThanh");
        }

        return Ok(new
        {
            maBooking = FixedLengthHelper.TrimSafe(booking.MaBooking),
            trangThai = FixedLengthHelper.TrimSafe(booking.TrangThai),
            tongDaThanhToan = tongDaTra,
            conLai
        });
    }

    [HttpPut("{maBooking}/huy")]
    [Authorize(Roles = "KhachHang")]
    public async Task<ActionResult> CancelMyBooking(string maBooking)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maBookingDb = FixedLengthHelper.PadTo20(maBooking);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var booking = await LockBookingForSeatTransitionAsync(maBookingDb);

        if (booking is null || booking.MaUser != maUserDb)
        {
            return NotFound(new { message = "Không tìm thấy booking của bạn." });
        }

        var trangThaiBooking = FixedLengthHelper.TrimSafe(booking.TrangThai);
        if (trangThaiBooking == "ChoHoanTien")
        {
            return BadRequest(new { message = "Đã gửi yêu cầu hủy. Đang chờ Sale xác nhận hoàn tiền." });
        }
        if (trangThaiBooking is not ("ChoXacNhan" or "DaXacNhan" or "DaThanhToan"))
        {
            return BadRequest(new
            {
                message = "Booking đã hoàn thành hoặc đã hủy, không thể hủy thêm."
            });
        }


        var lichKhoiHanh = booking.MaKhoiHanh is null
            ? null
            : await _context.LichKhoiHanhs
                .FirstOrDefaultAsync(item => item.MaKhoiHanh == booking.MaKhoiHanh);

        var soNgayConLai = lichKhoiHanh?.NgayKhoiHanh is DateTime ngayKhoiHanh
            ? (ngayKhoiHanh - DateTime.UtcNow).TotalDays
            : 0;

        var tyLePhatHuy = soNgayConLai >= 10
            ? 0
            : soNgayConLai >= 8
                ? 30
                : soNgayConLai >= 5
                    ? 50
                    : soNgayConLai >= 2
                        ? 70
                        : 100;

        var daXacNhan = FixedLengthHelper.PadTo20("DaXacNhan");
        var thanhCong = FixedLengthHelper.PadTo20("ThanhCong");
        var tongDaTra = await _context.ThanhToans
            .Where(payment => payment.MaBooking == maBookingDb &&
                (payment.TrangThai == daXacNhan || payment.TrangThai == thanhCong))
            .SumAsync(payment => (long?)payment.SoTien) ?? 0L;

        var soTienPhatHuy = (int)Math.Round(
            (booking.ThanhTien ?? 0) * tyLePhatHuy / 100m);
        soTienPhatHuy = (int)Math.Min(soTienPhatHuy, tongDaTra);
        booking.TyLePhatHuy = tyLePhatHuy;
        booking.SoTienPhatHuy = soTienPhatHuy;
        booking.TrangThai = FixedLengthHelper.PadTo20(tongDaTra <= 0 ? "DaHuy" : "ChoHoanTien");
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            maBooking = FixedLengthHelper.TrimSafe(booking.MaBooking),
            trangThai = FixedLengthHelper.TrimSafe(booking.TrangThai),
            tyLePhatHuy = booking.TyLePhatHuy,
            soTienPhatHuy = booking.SoTienPhatHuy,
            soTienChoHoan = tongDaTra,
            daHoanTien = tongDaTra <= 0 ? 0 : (int?)null
        });
    }

    [HttpPut("{maBooking}/xac-nhan-hoan-tien")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> ConfirmRefund(string maBooking)
    {
        var maBookingDb = FixedLengthHelper.PadTo20(maBooking);
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var booking = await LockBookingForSeatTransitionAsync(maBookingDb);
        if (booking is null)
            return NotFound(new { message = "Không tìm thấy booking." });

        var trangThai = FixedLengthHelper.TrimSafe(booking.TrangThai);
        if (trangThai != "ChoHoanTien")
            return BadRequest(new { message = "Chỉ xác nhận hoàn tiền khi khách đã gửi yêu cầu hủy (Chờ hoàn tiền)." });

        var daXacNhan = FixedLengthHelper.PadTo20("DaXacNhan");
        var thanhCong = FixedLengthHelper.PadTo20("ThanhCong");
        var tongDaTra = await _context.ThanhToans
            .Where(payment => payment.MaBooking == maBookingDb &&
                (payment.TrangThai == daXacNhan || payment.TrangThai == thanhCong))
            .SumAsync(payment => (long?)payment.SoTien) ?? 0L;

        booking.TrangThai = FixedLengthHelper.PadTo20("DaHuy");
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            maBooking = FixedLengthHelper.TrimSafe(booking.MaBooking),
            trangThai = FixedLengthHelper.TrimSafe(booking.TrangThai),
            soTienHoan = tongDaTra,
            soTienPhatHuy = booking.SoTienPhatHuy
        });
    }


    // Serialize seat-release/status writers with CreateBooking. A stale Sale update must
    // not revive a cancelled booking after its seats have already been sold again.
    private async Task<DatDichVu?> LockBookingForSeatTransitionAsync(string key)
    {
        var tourKey = await _context.DatDichVus.AsNoTracking().Where(b => b.MaBooking == key)
            .Select(b => b.MaTour).FirstOrDefaultAsync();
        if (tourKey is null) return null;
        await _context.Tours.FromSqlRaw(
            "SELECT * FROM dbo.Tour WITH (UPDLOCK, HOLDLOCK) WHERE MaTour = {0}", tourKey).FirstOrDefaultAsync();
        return await _context.DatDichVus.FromSqlRaw(
            "SELECT * FROM dbo.DatDichVu WITH (UPDLOCK, HOLDLOCK) WHERE MaBooking = {0}", key).FirstOrDefaultAsync();
    }

    private string? GetCurrentMaUser()
    {
        return User.FindFirst("MaUser")?.Value;
    }

    private async Task<string> GenerateMaHopDongAsync()
    {
        string maHopDongDb;

        do
        {
            var maHopDong = $"HD{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            maHopDongDb = FixedLengthHelper.PadTo20(maHopDong);
        }
        while (await _context.HopDongs.AnyAsync(item => item.MaHopDong == maHopDongDb));

        return maHopDongDb;
    }
}
