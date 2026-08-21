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
    private static readonly HashSet<string> DonViHopLe = new(StringComparer.OrdinalIgnoreCase)
    {
        "%",
        "VND"
    };

    private readonly AppDbContext _context;

    public KhuyenMaiController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/KhuyenMai
    [HttpGet]
    public async Task<ActionResult> GetActivePromotions()
    {
        var now = DateTime.UtcNow;
        var trangThaiHoatDong = FixedLengthHelper.PadTo20("HoatDong");

        var items = await _context.KhuyenMais
            .AsNoTracking()
            .Where(item =>
                item.TrangThai == trangThaiHoatDong &&
                item.NgayBd <= now &&
                item.NgayKt >= now)
            .OrderByDescending(item => item.NgayBd)
            .Select(item => new
            {
                maKm = FixedLengthHelper.TrimSafe(item.MaKm),
                tenKm = item.TenKm,
                maCode = PadTo10Safe(item.MaCode),
                ngayBd = item.NgayBd,
                ngayKt = item.NgayKt,
                donVi = FixedLengthHelper.TrimSafe(item.DonVi),
                giamGia = item.GiamGia,
                coCongDon = item.CoCongDon
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET /api/KhuyenMai/{maKM}
    [HttpGet("{maKm}")]
    public async Task<ActionResult> GetPromotion(string maKm)
    {
        var maKmDb = FixedLengthHelper.PadTo20(maKm);

        var item = await _context.KhuyenMais
            .AsNoTracking()
            .Where(km => km.MaKm == maKmDb)
            .Select(km => new
            {
                maKm = FixedLengthHelper.TrimSafe(km.MaKm),
                tenKm = km.TenKm,
                maCode = PadTo10Safe(km.MaCode),
                ngayBd = km.NgayBd,
                ngayKt = km.NgayKt,
                donVi = FixedLengthHelper.TrimSafe(km.DonVi),
                giamGia = km.GiamGia,
                coCongDon = km.CoCongDon,
                trangThai = FixedLengthHelper.TrimSafe(km.TrangThai),
                maNhomKm = FixedLengthHelper.TrimSafe(km.MaNhomKm),
                dieuKien = km.DieuKienKms.Select(dk => new
                {
                    maDk = FixedLengthHelper.TrimSafe(dk.MaDk),
                    donToiThieu = dk.DonToiThieu,
                    lanDatDau = dk.LanDatDau,
                    soLuong = dk.SoLuong
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (item is null)
        {
            return NotFound(new { message = $"Không tìm thấy khuyến mãi '{maKm}'." });
        }

        return Ok(item);
    }

    // POST /api/KhuyenMai
    [HttpPost]
    [Authorize(Roles = "Admin,Sale")]
    public async Task<ActionResult> Create(KhuyenMaiCreateDto request)
    {
        var validation = ValidatePromotionFields(
            request.NgayBd,
            request.NgayKt,
            request.DonVi,
            request.GiamGia);

        if (validation is not null)
        {
            return validation;
        }

        if (string.IsNullOrWhiteSpace(request.TenKm) ||
            string.IsNullOrWhiteSpace(request.MaCode))
        {
            return BadRequest(new { message = "TenKM và MaCode không được để trống." });
        }

        var maCodeDb = PadTo10(request.MaCode);

        if (await _context.KhuyenMais.AnyAsync(item => item.MaCode == maCodeDb))
        {
            return Conflict(new { message = "MaCode đã tồn tại." });
        }

        string? maNhomKmDb = null;

        if (!string.IsNullOrWhiteSpace(request.MaNhomKm))
        {
            maNhomKmDb = FixedLengthHelper.PadTo20(request.MaNhomKm);

            var nhomTonTai = await _context.NhomKhuyenMais
                .AnyAsync(item => item.MaNhomKm == maNhomKmDb);

            if (!nhomTonTai)
            {
                return BadRequest(new { message = "Nhóm khuyến mãi không tồn tại." });
            }
        }

        var maKmDb = await GenerateMaKmAsync();

        var khuyenMai = new KhuyenMai
        {
            MaKm = maKmDb,
            TenKm = request.TenKm.Trim(),
            MaCode = maCodeDb,
            NgayBd = request.NgayBd,
            NgayKt = request.NgayKt,
            DonVi = FixedLengthHelper.PadTo20(request.DonVi.Trim()),
            GiamGia = request.GiamGia,
            CoCongDon = request.CoCongDon,
            MaNhomKm = maNhomKmDb,
            TrangThai = FixedLengthHelper.PadTo20("HoatDong")
        };

        _context.KhuyenMais.Add(khuyenMai);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            maKm = FixedLengthHelper.TrimSafe(khuyenMai.MaKm),
            tenKm = khuyenMai.TenKm,
            maCode = PadTo10Safe(khuyenMai.MaCode),
            ngayBd = khuyenMai.NgayBd,
            ngayKt = khuyenMai.NgayKt,
            donVi = FixedLengthHelper.TrimSafe(khuyenMai.DonVi),
            giamGia = khuyenMai.GiamGia,
            coCongDon = khuyenMai.CoCongDon,
            trangThai = FixedLengthHelper.TrimSafe(khuyenMai.TrangThai)
        });
    }

    // PUT /api/KhuyenMai/{maKM}
    [HttpPut("{maKm}")]
    [Authorize(Roles = "Admin,Sale")]
    public async Task<IActionResult> Update(string maKm, KhuyenMaiUpdateDto request)
    {
        var maKmDb = FixedLengthHelper.PadTo20(maKm);

        var khuyenMai = await _context.KhuyenMais
            .FirstOrDefaultAsync(item => item.MaKm == maKmDb);

        if (khuyenMai is null)
        {
            return NotFound(new { message = $"Không tìm thấy khuyến mãi '{maKm}'." });
        }

        var ngayBd = request.NgayBd ?? khuyenMai.NgayBd;
        var ngayKt = request.NgayKt ?? khuyenMai.NgayKt;
        var donVi = request.DonVi ?? FixedLengthHelper.TrimSafe(khuyenMai.DonVi);
        var giamGia = request.GiamGia ?? khuyenMai.GiamGia ?? 0;

        if (request.NgayBd.HasValue || request.NgayKt.HasValue ||
            request.DonVi is not null || request.GiamGia.HasValue)
        {
            if (!ngayBd.HasValue || !ngayKt.HasValue || donVi is null)
            {
                return BadRequest(new { message = "Ngày bắt đầu, ngày kết thúc và đơn vị giảm giá phải hợp lệ." });
            }

            var validation = ValidatePromotionFields(
                ngayBd.Value,
                ngayKt.Value,
                donVi,
                giamGia);

            if (validation is not null)
            {
                return validation;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.MaCode))
        {
            var maCodeDb = PadTo10(request.MaCode);

            if (await _context.KhuyenMais.AnyAsync(item =>
                    item.MaCode == maCodeDb && item.MaKm != maKmDb))
            {
                return Conflict(new { message = "MaCode đã tồn tại." });
            }

            khuyenMai.MaCode = maCodeDb;
        }

        if (!string.IsNullOrWhiteSpace(request.TenKm))
        {
            khuyenMai.TenKm = request.TenKm.Trim();
        }

        if (request.NgayBd.HasValue)
        {
            khuyenMai.NgayBd = request.NgayBd;
        }

        if (request.NgayKt.HasValue)
        {
            khuyenMai.NgayKt = request.NgayKt;
        }

        if (!string.IsNullOrWhiteSpace(request.DonVi))
        {
            khuyenMai.DonVi = FixedLengthHelper.PadTo20(request.DonVi.Trim());
        }

        if (request.GiamGia.HasValue)
        {
            khuyenMai.GiamGia = request.GiamGia;
        }

        if (request.CoCongDon.HasValue)
        {
            khuyenMai.CoCongDon = request.CoCongDon;
        }

        if (request.MaNhomKm is not null)
        {
            if (string.IsNullOrWhiteSpace(request.MaNhomKm))
            {
                khuyenMai.MaNhomKm = null;
            }
            else
            {
                var maNhomKmDb = FixedLengthHelper.PadTo20(request.MaNhomKm);
                var nhomTonTai = await _context.NhomKhuyenMais
                    .AnyAsync(item => item.MaNhomKm == maNhomKmDb);

                if (!nhomTonTai)
                {
                    return BadRequest(new { message = "Nhóm khuyến mãi không tồn tại." });
                }

                khuyenMai.MaNhomKm = maNhomKmDb;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.TrangThai))
        {
            khuyenMai.TrangThai = FixedLengthHelper.PadTo20(request.TrangThai.Trim());
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/KhuyenMai/{maKM}
    [HttpDelete("{maKm}")]
    [Authorize(Roles = "Admin,Sale")]
    public async Task<IActionResult> Delete(string maKm)
    {
        var maKmDb = FixedLengthHelper.PadTo20(maKm);

        var khuyenMai = await _context.KhuyenMais
            .FirstOrDefaultAsync(item => item.MaKm == maKmDb);

        if (khuyenMai is null)
        {
            return NotFound(new { message = $"Không tìm thấy khuyến mãi '{maKm}'." });
        }

        var daSuDung = await _context.DatDichVuKhuyenMais
            .AnyAsync(item => item.MaKhuyenMai == maKmDb);

        if (daSuDung)
        {
            khuyenMai.TrangThai = FixedLengthHelper.PadTo20("NgungHoatDong");
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Khuyến mãi đã được sử dụng — đã chuyển sang trạng thái NgungHoatDong."
            });
        }

        _context.KhuyenMais.Remove(khuyenMai);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/KhuyenMai/{maKM}/dieu-kien
    [HttpPost("{maKm}/dieu-kien")]
    [Authorize(Roles = "Admin,Sale")]
    public async Task<ActionResult> AddCondition(
        string maKm,
        DieuKienKmCreateDto request)
    {
        var maKmDb = FixedLengthHelper.PadTo20(maKm);

        var khuyenMaiTonTai = await _context.KhuyenMais
            .AnyAsync(item => item.MaKm == maKmDb);

        if (!khuyenMaiTonTai)
        {
            return NotFound(new { message = $"Không tìm thấy khuyến mãi '{maKm}'." });
        }

        var maDkDb = await GenerateMaDkAsync();

        var dieuKien = new DieuKienKm
        {
            MaDk = maDkDb,
            MaKhuyenMai = maKmDb,
            DonToiThieu = request.DonToiThieu,
            LanDatDau = request.LanDatDau,
            SoLuong = request.SoLuong
        };

        _context.DieuKienKms.Add(dieuKien);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            maDk = FixedLengthHelper.TrimSafe(dieuKien.MaDk),
            maKhuyenMai = FixedLengthHelper.TrimSafe(dieuKien.MaKhuyenMai),
            donToiThieu = dieuKien.DonToiThieu,
            lanDatDau = dieuKien.LanDatDau,
            soLuong = dieuKien.SoLuong
        });
    }

    // POST /api/KhuyenMai/ap-dung
    [HttpPost("ap-dung")]
    [Authorize]
    public async Task<ActionResult> ApplyPromotion(KhuyenMaiApDungDto request)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.MaBooking) ||
            string.IsNullOrWhiteSpace(request.MaCode))
        {
            return BadRequest(new { message = "MaBooking và MaCode không được để trống." });
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maBookingDb = FixedLengthHelper.PadTo20(request.MaBooking);
        var maCodeDb = PadTo10(request.MaCode);
        var trangThaiChoXacNhan = FixedLengthHelper.PadTo20("ChoXacNhan");

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
                message = "Chỉ có thể áp dụng khuyến mãi cho booking đang ở trạng thái ChoXacNhan."
            });
        }

        var now = DateTime.UtcNow;
        var trangThaiHoatDong = FixedLengthHelper.PadTo20("HoatDong");

        var khuyenMai = await _context.KhuyenMais
            .FirstOrDefaultAsync(item =>
                item.MaCode == maCodeDb &&
                item.TrangThai == trangThaiHoatDong &&
                item.NgayBd <= now &&
                item.NgayKt >= now);

        if (khuyenMai is null)
        {
            return BadRequest(new
            {
                message = "Mã khuyến mãi không hợp lệ hoặc đã hết hạn."
            });
        }

        var maKmDb = khuyenMai.MaKm;

        var dieuKien = await _context.DieuKienKms
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.MaKhuyenMai == maKmDb);

        if (dieuKien is not null)
        {
            var tongTien = booking.TongTien ?? 0;

            if (dieuKien.DonToiThieu.HasValue &&
                tongTien < dieuKien.DonToiThieu.Value)
            {
                return BadRequest(new
                {
                    message = "Booking chưa đạt đơn tối thiểu để áp dụng khuyến mãi."
                });
            }

            if (dieuKien.SoLuong.HasValue)
            {
                var daSuDung = await _context.DatDichVuKhuyenMais
                    .CountAsync(item => item.MaKhuyenMai == maKmDb);

                if (daSuDung >= dieuKien.SoLuong.Value)
                {
                    return BadRequest(new
                    {
                        message = "Mã khuyến mãi đã hết lượt sử dụng."
                    });
                }
            }
        }

        var daApMaNay = await _context.DatDichVuKhuyenMais
            .AnyAsync(item =>
                item.MaBooking == maBookingDb &&
                item.MaKhuyenMai == maKmDb);

        if (daApMaNay)
        {
            return Conflict(new
            {
                message = "Mã khuyến mãi này đã được áp dụng cho booking."
            });
        }

        if (khuyenMai.CoCongDon != true)
        {
            var daCoKmKhac = await _context.DatDichVuKhuyenMais
                .AnyAsync(item => item.MaBooking == maBookingDb);

            if (daCoKmKhac)
            {
                return BadRequest(new
                {
                    message = "Booking đã áp dụng khuyến mãi khác, không hỗ trợ cộng dồn."
                });
            }
        }

        var tongTienBooking = booking.TongTien ?? 0;
        var donVi = FixedLengthHelper.TrimSafe(khuyenMai.DonVi);
        var giamGia = khuyenMai.GiamGia ?? 0;

        int soTienGiam;

        if (string.Equals(donVi, "%", StringComparison.Ordinal))
        {
            soTienGiam = tongTienBooking * giamGia / 100;
        }
        else
        {
            soTienGiam = Math.Min(giamGia, tongTienBooking);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var apDung = new DatDichVuKhuyenMai
        {
            MaBooking = maBookingDb,
            MaKhuyenMai = maKmDb,
            SoTienGiam = soTienGiam
        };

        _context.DatDichVuKhuyenMais.Add(apDung);

        booking.TongGiamGia = (booking.TongGiamGia ?? 0) + soTienGiam;
        booking.ThanhTien = tongTienBooking - booking.TongGiamGia;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            maBooking = FixedLengthHelper.TrimSafe(booking.MaBooking),
            soTienGiam,
            tongGiamGiaMoi = booking.TongGiamGia,
            thanhTienMoi = booking.ThanhTien
        });
    }

    private string? GetCurrentMaUser()
    {
        return User.FindFirst("MaUser")?.Value;
    }

    private static string PadTo10(string value)
    {
        return value.Trim().PadRight(10);
    }

    private static string? PadTo10Safe(string? value)
    {
        return value?.Trim();
    }

    private ActionResult? ValidatePromotionFields(
        DateTime ngayBd,
        DateTime ngayKt,
        string donVi,
        int giamGia)
    {
        if (ngayKt <= ngayBd)
        {
            return BadRequest(new { message = "NgayKT phải lớn hơn NgayBD." });
        }

        if (giamGia <= 0)
        {
            return BadRequest(new { message = "GiamGia phải lớn hơn 0." });
        }

        var donViTrim = donVi.Trim();

        if (!DonViHopLe.Contains(donViTrim))
        {
            return BadRequest(new { message = "DonVi chỉ nhận '%' hoặc 'VND'." });
        }

        if (string.Equals(donViTrim, "%", StringComparison.OrdinalIgnoreCase) &&
            giamGia > 100)
        {
            return BadRequest(new { message = "GiamGia không được vượt quá 100 khi DonVi = '%'." });
        }

        return null;
    }

    private async Task<string> GenerateMaKmAsync()
    {
        string maKmDb;

        do
        {
            var maKm = $"KM{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            maKmDb = FixedLengthHelper.PadTo20(maKm);
        }
        while (await _context.KhuyenMais
            .AnyAsync(item => item.MaKm == maKmDb));

        return maKmDb;
    }

    private async Task<string> GenerateMaDkAsync()
    {
        string maDkDb;

        do
        {
            var maDk = $"DK{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            maDkDb = FixedLengthHelper.PadTo20(maDk);
        }
        while (await _context.DieuKienKms
            .AnyAsync(item => item.MaDk == maDkDb));

        return maDkDb;
    }
}
