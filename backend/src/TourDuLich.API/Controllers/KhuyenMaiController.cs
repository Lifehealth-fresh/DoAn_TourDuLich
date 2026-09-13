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
public class KhuyenMaiController : ControllerBase
{
    private readonly AppDbContext _context;
    public KhuyenMaiController(AppDbContext context) => _context = context;
    private bool IsStaff => User.IsInRole("Sale") || User.IsInRole("Admin");

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult> GetActivePromotions([FromQuery] int all = 0)
    {
        var query = _context.KhuyenMais.AsNoTracking();
        if (all != 1 || !IsStaff) query = ActiveOnly(query);
        var items = await query.OrderByDescending(item => item.NgayBd)
            .Select(item => new
            {
                maKm = FixedLengthHelper.TrimSafe(item.MaKm), tenKm = item.TenKm,
                maCode = item.MaCode == null ? null : item.MaCode.Trim(),
                ngayBd = item.NgayBd, ngayKt = item.NgayKt,
                donVi = FixedLengthHelper.TrimSafe(item.DonVi), giamGia = item.GiamGia,
                coCongDon = item.CoCongDon, trangThai = FixedLengthHelper.TrimSafe(item.TrangThai),
                maNhomKm = FixedLengthHelper.TrimSafe(item.MaNhomKm),
                dieuKien = item.DieuKienKms.Select(dk => new
                {
                    maDk = FixedLengthHelper.TrimSafe(dk.MaDk), donToiThieu = dk.DonToiThieu,
                    lanDatDau = dk.LanDatDau, soLuong = dk.SoLuong
                }).ToList(),
                maTours = item.KmTours.Select(link => FixedLengthHelper.TrimSafe(link.MaTour)).ToList()
            }).ToListAsync();
        return Ok(items);
    }

    [HttpGet("{maKm}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetPromotion(string maKm)
    {
        var key = FixedLengthHelper.PadTo20(maKm);
        var query = _context.KhuyenMais.AsNoTracking().Where(item => item.MaKm == key);
        if (!IsStaff) query = ActiveOnly(query);
        var item = await query.Select(km => new
        {
            maKm = FixedLengthHelper.TrimSafe(km.MaKm), tenKm = km.TenKm,
            maCode = km.MaCode == null ? null : km.MaCode.Trim(),
            ngayBd = km.NgayBd, ngayKt = km.NgayKt,
            donVi = FixedLengthHelper.TrimSafe(km.DonVi), giamGia = km.GiamGia, coCongDon = km.CoCongDon,
            trangThai = FixedLengthHelper.TrimSafe(km.TrangThai), maNhomKm = FixedLengthHelper.TrimSafe(km.MaNhomKm),
            dieuKien = km.DieuKienKms.Select(dk => new
            {
                maDk = FixedLengthHelper.TrimSafe(dk.MaDk), donToiThieu = dk.DonToiThieu,
                lanDatDau = dk.LanDatDau, soLuong = dk.SoLuong
            }).ToList(),
            maTours = km.KmTours.Select(link => FixedLengthHelper.TrimSafe(link.MaTour)).ToList()
        }).FirstOrDefaultAsync();
        return item is null ? NotFound(new { message = $"Không tìm thấy khuyến mãi '{maKm}'." }) : Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Sale")]
    public async Task<ActionResult> Create(KhuyenMaiCreateDto request)
    {
        var error = ValidateFields(request.TenKm, request.MaCode, request.NgayBd, request.NgayKt,
            request.DonVi, request.GiamGia, request.TrangThai) ?? ValidateCondition(request.DieuKien);
        if (error is not null) return BadRequest(new { message = error });
        error = await ValidateToursAsync(request.MaTours);
        if (error is not null) return BadRequest(new { message = error });
        var group = string.IsNullOrWhiteSpace(request.MaNhomKm) ? null : FixedLengthHelper.PadTo20(request.MaNhomKm);
        if (group is not null && !await _context.NhomKhuyenMais.AnyAsync(item => item.MaNhomKm == group))
            return BadRequest(new { message = "Nhóm khuyến mãi không tồn tại." });

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var code = PadTo10(request.MaCode);
        // Serialize code uniqueness checks without adding a new index/column.
        if (await _context.KhuyenMais.FromSqlRaw(
            "SELECT * FROM dbo.KhuyenMai WITH (UPDLOCK, HOLDLOCK) WHERE MaCode = {0}", code).AnyAsync())
            return Conflict(new { message = "MaCode đã tồn tại." });
        var entity = new KhuyenMai
        {
            MaKm = await GenerateMaKmAsync(), TenKm = request.TenKm.Trim(), MaCode = code,
            NgayBd = request.NgayBd, NgayKt = request.NgayKt, DonVi = FixedLengthHelper.PadTo20(request.DonVi.Trim().ToUpperInvariant()),
            GiamGia = request.GiamGia, CoCongDon = request.CoCongDon, MaNhomKm = group,
            TrangThai = FixedLengthHelper.PadTo20(request.TrangThai.Trim())
        };
        _context.KhuyenMais.Add(entity);
        await _context.SaveChangesAsync();
        await ReplaceRulesAsync(entity.MaKm, request.DieuKien, request.MaTours);
        await transaction.CommitAsync();
        return StatusCode(StatusCodes.Status201Created, new
        {
            maKm = FixedLengthHelper.TrimSafe(entity.MaKm), tenKm = entity.TenKm, maCode = entity.MaCode.Trim(),
            ngayBd = entity.NgayBd, ngayKt = entity.NgayKt, donVi = FixedLengthHelper.TrimSafe(entity.DonVi),
            giamGia = entity.GiamGia, coCongDon = entity.CoCongDon, trangThai = FixedLengthHelper.TrimSafe(entity.TrangThai)
        });
    }

    [HttpPut("{maKm}")]
    [Authorize(Roles = "Admin,Sale")]
    public async Task<IActionResult> Update(string maKm, KhuyenMaiUpdateDto request)
    {
        var key = FixedLengthHelper.PadTo20(maKm);
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var entity = await LockedPromotion(key).FirstOrDefaultAsync();
        if (entity is null) return NotFound(new { message = $"Không tìm thấy khuyến mãi '{maKm}'." });
        var name = request.TenKm ?? entity.TenKm;
        var code = request.MaCode ?? entity.MaCode;
        var start = request.NgayBd ?? entity.NgayBd;
        var end = request.NgayKt ?? entity.NgayKt;
        var unit = request.DonVi ?? FixedLengthHelper.TrimSafe(entity.DonVi);
        var discount = request.GiamGia ?? entity.GiamGia ?? 0;
        var state = request.TrangThai ?? FixedLengthHelper.TrimSafe(entity.TrangThai);
        var error = ValidateFields(name, code, start, end, unit, discount, state) ?? ValidateCondition(request.DieuKien);
        if (error is not null) return BadRequest(new { message = error });
        error = await ValidateToursAsync(request.MaTours);
        if (error is not null) return BadRequest(new { message = error });
        var codeDb = PadTo10(code!);
        if (await _context.KhuyenMais.FromSqlRaw(
                "SELECT * FROM dbo.KhuyenMai WITH (UPDLOCK, HOLDLOCK) WHERE MaCode = {0}", codeDb)
            .AnyAsync(item => item.MaKm != key))
            return Conflict(new { message = "MaCode đã tồn tại." });
        var group = request.MaNhomKm is null ? entity.MaNhomKm :
            string.IsNullOrWhiteSpace(request.MaNhomKm) ? null : FixedLengthHelper.PadTo20(request.MaNhomKm);
        if (group is not null && !await _context.NhomKhuyenMais.AnyAsync(item => item.MaNhomKm == group))
            return BadRequest(new { message = "Nhóm khuyến mãi không tồn tại." });
        entity.TenKm = name!.Trim(); entity.MaCode = codeDb;
        entity.NgayBd = start; entity.NgayKt = end; entity.DonVi = FixedLengthHelper.PadTo20(unit!.Trim().ToUpperInvariant());
        entity.GiamGia = discount; entity.CoCongDon = request.CoCongDon ?? entity.CoCongDon;
        entity.TrangThai = FixedLengthHelper.PadTo20(state!); entity.MaNhomKm = group;
        await _context.SaveChangesAsync();
        await ReplaceRulesAsync(key, request.DieuKien, request.MaTours);
        await transaction.CommitAsync();
        return NoContent();
    }

    [HttpDelete("{maKm}")]
    [Authorize(Roles = "Admin,Sale")]
    public async Task<IActionResult> Delete(string maKm)
    {
        var key = FixedLengthHelper.PadTo20(maKm);
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var entity = await LockedPromotion(key).FirstOrDefaultAsync();
        if (entity is null) return NotFound(new { message = $"Không tìm thấy khuyến mãi '{maKm}'." });
        if (await _context.DatDichVuKhuyenMais.AnyAsync(item => item.MaKhuyenMai == key))
        {
            entity.TrangThai = FixedLengthHelper.PadTo20("NgungHoatDong");
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok(new { message = "Khuyến mãi đã được sử dụng — đã chuyển sang trạng thái NgungHoatDong." });
        }
        _context.DieuKienKms.RemoveRange(await _context.DieuKienKms.Where(item => item.MaKhuyenMai == key).ToListAsync());
        _context.KmTours.RemoveRange(await _context.KmTours.Where(item => item.MaKhuyenMai == key).ToListAsync());
        _context.KhuyenMais.Remove(entity);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return NoContent();
    }

    [HttpPost("{maKm}/dieu-kien")]
    [Authorize(Roles = "Admin,Sale")]
    public async Task<ActionResult> AddCondition(string maKm, DieuKienKmCreateDto request)
    {
        var error = ValidateCondition(request);
        if (error is not null) return BadRequest(new { message = error });
        var key = FixedLengthHelper.PadTo20(maKm);
        await using var transaction = await _context.Database.BeginTransactionAsync();
        if (!await LockedPromotion(key).AnyAsync())
            return NotFound(new { message = $"Không tìm thấy khuyến mãi '{maKm}'." });
        var condition = new DieuKienKm
        {
            MaDk = await GenerateMaDkAsync(), MaKhuyenMai = key,
            DonToiThieu = request.DonToiThieu, LanDatDau = request.LanDatDau, SoLuong = request.SoLuong
        };
        _context.DieuKienKms.Add(condition);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return StatusCode(StatusCodes.Status201Created, new
        {
            maDk = FixedLengthHelper.TrimSafe(condition.MaDk), maKhuyenMai = maKm,
            donToiThieu = condition.DonToiThieu, lanDatDau = condition.LanDatDau, soLuong = condition.SoLuong
        });
    }

    [HttpPost("ap-dung")]
    [Authorize]
    public async Task<ActionResult> ApplyPromotion(KhuyenMaiApDungDto request)
    {
        var user = GetCurrentMaUser();
        if (string.IsNullOrWhiteSpace(user)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.MaBooking) || string.IsNullOrWhiteSpace(request.MaCode))
            return BadRequest(new { message = "MaBooking và MaCode không được để trống." });
        if (request.MaCode.Trim().Length > 10)
            return BadRequest(new { message = "MaCode chỉ được tối đa 10 ký tự." });

        var userDb = FixedLengthHelper.PadTo20(user);
        var bookingDb = FixedLengthHelper.PadTo20(request.MaBooking);
        var codeDb = PadTo10(request.MaCode);
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var booking = await _context.DatDichVus.FromSqlRaw(
                "SELECT * FROM dbo.DatDichVu WITH (UPDLOCK, HOLDLOCK) WHERE MaBooking = {0}", bookingDb)
            .FirstOrDefaultAsync(item => item.MaUser == userDb);
        if (booking is null) return NotFound(new { message = "Không tìm thấy booking của bạn." });
        if (FixedLengthHelper.TrimSafe(booking.TrangThai) is "DaHuy" or "ChoHoanTien" or "HoanThanh")
            return BadRequest(new { message = "Không thể áp dụng khuyến mãi cho vé đã hủy, chờ hoàn tiền hoặc hoàn thành." });

        // Lock the promotion before validating its dates, usage count and tour rules.
        var promotionKey = await _context.KhuyenMais.Where(item => item.MaCode == codeDb)
            .Select(item => item.MaKm).FirstOrDefaultAsync();
        if (promotionKey is null) return BadRequest(new { message = "Mã khuyến mãi không hợp lệ hoặc đã hết hạn." });
        var promotion = await LockedPromotion(promotionKey).FirstOrDefaultAsync(item => item.MaCode == codeDb);
        var now = DateTime.UtcNow;
        if (promotion is null ||
            promotion.TrangThai != FixedLengthHelper.PadTo20("HoatDong") ||
            !promotion.NgayBd.HasValue || !promotion.NgayKt.HasValue ||
            promotion.NgayBd.Value > now || promotion.NgayKt.Value < now)
            return BadRequest(new { message = "Mã khuyến mãi không hợp lệ hoặc đã hết hạn." });

        var key = promotion.MaKm;
        var usedCodes = await _context.DatDichVuKhuyenMais.Where(item => item.MaBooking == bookingDb)
            .Select(item => new { item.MaKhuyenMai, item.SoTienGiam, coCongDon = item.MaKhuyenMaiNavigation.CoCongDon }).ToListAsync();
        if (usedCodes.Any(item => item.MaKhuyenMai == key))
            return Conflict(new { message = "Mã khuyến mãi này đã được áp dụng cho booking." });
        if (usedCodes.Count > 0 && (promotion.CoCongDon != true || usedCodes.Any(item => item.coCongDon != true)))
            return BadRequest(new { message = "Booking đã áp dụng khuyến mãi khác, không hỗ trợ cộng dồn." });

        var total = booking.TongTien ?? 0;
        if (total < 0) return BadRequest(new { message = "Tổng tiền vé không hợp lệ." });
        var conditions = await _context.DieuKienKms.AsNoTracking().Where(item => item.MaKhuyenMai == key).ToListAsync();
        if (conditions.Any(item => item.DonToiThieu.HasValue && total < item.DonToiThieu.Value))
            return BadRequest(new { message = "Booking chưa đạt đơn tối thiểu để áp dụng khuyến mãi." });
        var cancelled = FixedLengthHelper.PadTo20("DaHuy");
        if (conditions.Any(item => item.LanDatDau == true) && await _context.DatDichVus.AnyAsync(item =>
                item.MaUser == userDb && item.MaBooking != bookingDb &&
                item.TrangThai != cancelled))
            return BadRequest(new { message = "Mã khuyến mãi chỉ áp dụng cho đơn đặt tour đầu tiên của tài khoản." });
        if (conditions.Any(item => item.SoLuong.HasValue))
        {
            var used = await _context.DatDichVuKhuyenMais.CountAsync(item => item.MaKhuyenMai == key);
            if (conditions.Any(item => item.SoLuong.HasValue && used >= item.SoLuong.Value))
                return BadRequest(new { message = "Mã khuyến mãi đã hết lượt sử dụng." });
        }
        var tourIds = await _context.KmTours.Where(item => item.MaKhuyenMai == key).Select(item => item.MaTour).ToListAsync();
        if (tourIds.Count > 0 && !tourIds.Contains(booking.MaTour))
            return BadRequest(new { message = "Mã khuyến mãi không áp dụng cho tour của vé này." });

        var unit = FixedLengthHelper.TrimSafe(promotion.DonVi);
        var discountValue = promotion.GiamGia ?? 0;
        if (discountValue < 0 || (unit != "%" && !string.Equals(unit, "VND", StringComparison.OrdinalIgnoreCase)))
            return BadRequest(new { message = "Giá trị hoặc đơn vị khuyến mãi không hợp lệ." });
        var rawDiscount = unit == "%" ? (long)total * discountValue / 100 : discountValue;
        var previousDiscount = Math.Clamp((long)(booking.TongGiamGia ?? 0), 0, total);
        var discount = (int)Math.Clamp(rawDiscount, 0, total - previousDiscount);
        if (discount <= 0) return BadRequest(new { message = "Mã này không còn số tiền có thể giảm trên vé." });
        var totalDiscount = (int)(previousDiscount + discount);
        var netTotal = total - totalDiscount;
        var confirmed = FixedLengthHelper.PadTo20("DaXacNhan");
        var success = FixedLengthHelper.PadTo20("ThanhCong");
        var pending = FixedLengthHelper.PadTo20("ChoXacNhan");
        var paid = await _context.ThanhToans.Where(item => item.MaBooking == bookingDb &&
                (item.TrangThai == confirmed || item.TrangThai == success)).SumAsync(item => (long?)item.SoTien) ?? 0L;
        if (netTotal < paid)
            return BadRequest(new { message = "Không thể áp mã làm thành tiền thấp hơn số tiền đã thanh toán." });
        if (await _context.ThanhToans.AnyAsync(item => item.MaBooking == bookingDb && item.TrangThai == pending))
            return BadRequest(new { message = "Vé đang có thanh toán chờ xác nhận. Hãy xử lý giao dịch đó trước khi áp mã." });

        await InsertBookingDiscountAsync(bookingDb, key, discount);
        booking.TongGiamGia = totalDiscount;
        booking.ThanhTien = netTotal;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new
        {
            maBooking = FixedLengthHelper.TrimSafe(booking.MaBooking), soTienGiam = discount,
            tongGiamGiaMoi = booking.TongGiamGia, thanhTienMoi = booking.ThanhTien,
            tongTien = total, tongGiamGia = totalDiscount, thanhTien = netTotal
        });
    }

    private static IQueryable<KhuyenMai> ActiveOnly(IQueryable<KhuyenMai> query)
    {
        var now = DateTime.UtcNow;
        var active = FixedLengthHelper.PadTo20("HoatDong");
        return query.Where(item => item.TrangThai == active && item.NgayBd <= now && item.NgayKt >= now);
    }
    private IQueryable<KhuyenMai> LockedPromotion(string key) => _context.KhuyenMais.FromSqlRaw(
        "SELECT * FROM dbo.KhuyenMai WITH (UPDLOCK, HOLDLOCK) WHERE MaKM = {0}", key);
    private string? GetCurrentMaUser() => User.FindFirst("MaUser")?.Value;
    private static string PadTo10(string value) => value.Trim().PadRight(10);

    private static string? ValidateFields(string? name, string? code, DateTime? start, DateTime? end, string? unit, int value, string? state)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code)) return "TenKM và MaCode không được để trống.";
        if (name.Trim().Length > 50) return "Tên khuyến mãi chỉ được tối đa 50 ký tự.";
        if (code.Trim().Length > 10) return "MaCode chỉ được tối đa 10 ký tự.";
        if (!start.HasValue || !end.HasValue || end <= start) return "NgayKT phải lớn hơn NgayBD.";
        if (value <= 0) return "GiamGia phải lớn hơn 0.";
        if (unit?.Trim().ToUpperInvariant() is not ("%" or "VND")) return "DonVi chỉ nhận '%' hoặc 'VND'.";
        if (unit.Trim() == "%" && value > 100) return "GiamGia không được vượt quá 100 khi DonVi = '%'.";
        if (state?.Trim() is not ("HoatDong" or "NgungHoatDong")) return "TrangThai chỉ nhận HoatDong hoặc NgungHoatDong.";
        return null;
    }
    private static string? ValidateCondition(DieuKienKmCreateDto? condition) =>
        condition?.DonToiThieu < 0 || condition?.SoLuong < 0 ? "Đơn tối thiểu và số lượt sử dụng không được âm." : null;
    private async Task<string?> ValidateToursAsync(List<string>? ids)
    {
        if (ids is null) return null;
        if (ids.Any(id => string.IsNullOrWhiteSpace(id) || id.Trim().Length > 20)) return "Mã tour áp dụng không hợp lệ.";
        var keys = ids.Select(FixedLengthHelper.PadTo20).Distinct().ToList();
        return await _context.Tours.CountAsync(item => keys.Contains(item.MaTour)) == keys.Count
            ? null : "Có tour áp dụng không tồn tại.";
    }
    private async Task ReplaceRulesAsync(string key, DieuKienKmCreateDto? condition, List<string>? ids)
    {
        // Null means unchanged for old clients; an empty tour list means all tours.
        if (condition is not null)
        {
            _context.DieuKienKms.RemoveRange(await _context.DieuKienKms.Where(item => item.MaKhuyenMai == key).ToListAsync());
            _context.DieuKienKms.Add(new DieuKienKm
            {
                MaDk = await GenerateMaDkAsync(), MaKhuyenMai = key,
                DonToiThieu = condition.DonToiThieu, LanDatDau = condition.LanDatDau, SoLuong = condition.SoLuong
            });
        }
        if (ids is not null)
            _context.KmTours.RemoveRange(await _context.KmTours.Where(item => item.MaKhuyenMai == key).ToListAsync());
        await _context.SaveChangesAsync();
        if (ids is not null)
            foreach (var id in ids.Select(FixedLengthHelper.PadTo20).Distinct())
                await InsertTourRuleAsync(key, id);
    }

    // The supplied SQL schema has non-IDENTITY STT keys; the EF model treats these keys as generated.
    // Parameterized inserts support both layouts without schema changes. Call inside a transaction.
    private Task<int> InsertTourRuleAsync(string key, string tour) => _context.Database.ExecuteSqlInterpolatedAsync($@"
IF COLUMNPROPERTY(OBJECT_ID(N'dbo.KM_Tour'), N'STT', 'IsIdentity') = 1
    INSERT INTO dbo.KM_Tour (MaKhuyenMai, MaTour) VALUES ({key}, {tour});
ELSE
    INSERT INTO dbo.KM_Tour (STT, MaKhuyenMai, MaTour)
    SELECT ISNULL(MAX(STT), 0) + 1, {key}, {tour} FROM dbo.KM_Tour WITH (UPDLOCK, HOLDLOCK);");

    private Task<int> InsertBookingDiscountAsync(string booking, string promotion, int amount) => _context.Database.ExecuteSqlInterpolatedAsync($@"
IF COLUMNPROPERTY(OBJECT_ID(N'dbo.DatDichVu_KhuyenMai'), N'STT', 'IsIdentity') = 1
    INSERT INTO dbo.DatDichVu_KhuyenMai (MaBooking, MaKhuyenMai, SoTienGiam) VALUES ({booking}, {promotion}, {amount});
ELSE
    INSERT INTO dbo.DatDichVu_KhuyenMai (STT, MaBooking, MaKhuyenMai, SoTienGiam)
    SELECT ISNULL(MAX(STT), 0) + 1, {booking}, {promotion}, {amount} FROM dbo.DatDichVu_KhuyenMai WITH (UPDLOCK, HOLDLOCK);");

    private async Task<string> GenerateMaKmAsync()
    {
        string key;
        do { key = FixedLengthHelper.PadTo20($"KM{Guid.NewGuid():N}"[..20].ToUpperInvariant()); }
        while (await _context.KhuyenMais.AnyAsync(item => item.MaKm == key));
        return key;
    }
    private async Task<string> GenerateMaDkAsync()
    {
        string key;
        do { key = FixedLengthHelper.PadTo20($"DK{Guid.NewGuid():N}"[..20].ToUpperInvariant()); }
        while (await _context.DieuKienKms.AnyAsync(item => item.MaDk == key));
        return key;
    }
}
