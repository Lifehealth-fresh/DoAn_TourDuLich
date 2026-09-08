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
public class DanhGiaController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHanhViLogger _hanhViLogger;

    public DanhGiaController(AppDbContext context, IHanhViLogger hanhViLogger)
    {
        _context = context;
        _hanhViLogger = hanhViLogger;
    }

    // GET /api/DanhGia/tour/{maTour}
    [HttpGet("tour/{maTour}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetTourReviews(string maTour, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var maTourDb = FixedLengthHelper.PadTo20(maTour);

        var tourTonTai = await _context.Tours
            .AnyAsync(item => item.MaTour == maTourDb);

        if (!tourTonTai)
        {
            return NotFound(new { message = $"Không tìm thấy tour '{maTour}'." });
        }

        var query = _context.DanhGiaTours
            .AsNoTracking()
            .Where(item => item.MaTour == maTourDb);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var tongDanhGia = await query.CountAsync();
        var diemTrungBinh = tongDanhGia == 0
            ? (double?)null
            : await query.AverageAsync(item => (double?)(item.SaoDanhGia ?? 0));

        var danhGias = await query
            .OrderByDescending(item => item.ThoiGian)
            .ThenBy(item => item.MaDanhGiaTour)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new
            {
                maDanhGiaTour = FixedLengthHelper.TrimSafe(item.MaDanhGiaTour),
                saoDanhGia = item.SaoDanhGia,
                nhanXet = item.NhanXet,
                thoiGian = item.ThoiGian,
                media = item.MediaDanhGiaTours.OrderBy(media => media.ThuTu).Select(media => new
                {
                    url = media.Url,
                    loaiMedia = FixedLengthHelper.TrimSafe(media.LoaiMedia),
                    thuTu = media.ThuTu
                })
            })
            .ToListAsync();

        return Ok(new
        {
            maTour = FixedLengthHelper.TrimSafe(maTourDb),
            diemTrungBinh,
            tongDanhGia,
            page,
            pageSize,
            danhGias
        });
    }

    // GET /api/DanhGia/huong-dan-vien/{maHDV}
    [HttpGet("huong-dan-vien/{maHdv}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetHdvReviews(string maHdv, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var maHdvDb = FixedLengthHelper.PadTo20(maHdv);

        var hdvTonTai = await _context.HuongDanViens
            .AnyAsync(item => item.MaHuongDanVien == maHdvDb);

        if (!hdvTonTai)
        {
            return NotFound(new { message = $"Không tìm thấy hướng dẫn viên '{maHdv}'." });
        }

        var query = _context.DanhGiaHdvs
            .AsNoTracking()
            .Where(item => item.MaHdv == maHdvDb);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var tongDanhGia = await query.CountAsync();
        var diemTrungBinh = tongDanhGia == 0
            ? (double?)null
            : await query.AverageAsync(item => (double?)item.SaoDanhGia);

        var danhGias = await query
            .OrderByDescending(item => item.ThoiGian)
            .ThenBy(item => item.MaDanhGiaHdv)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new
            {
                maDanhGiaHdv = FixedLengthHelper.TrimSafe(item.MaDanhGiaHdv),
                saoDanhGia = item.SaoDanhGia,
                nhanXet = item.NhanXet,
                thoiGian = item.ThoiGian,
                media = item.MediaDanhGiaHdvs.OrderBy(media => media.ThuTu).Select(media => new
                {
                    url = media.Url,
                    loaiMedia = FixedLengthHelper.TrimSafe(media.LoaiMedia),
                    thuTu = media.ThuTu
                })
            })
            .ToListAsync();

        return Ok(new
        {
            maHdv = FixedLengthHelper.TrimSafe(maHdvDb),
            diemTrungBinh,
            tongDanhGia,
            page,
            pageSize,
            danhGias
        });
    }

    // GET /api/DanhGia/san-pham-doi-tac/{maSanPham}
    [HttpGet("san-pham-doi-tac/{maSanPham}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetSanPhamReviews(string maSanPham, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var maSanPhamDb = FixedLengthHelper.PadTo20(maSanPham);

        var sanPhamTonTai = await _context.SanPhamDoiTacs
            .AnyAsync(item => item.MaSanPham == maSanPhamDb);

        if (!sanPhamTonTai)
        {
            return NotFound(new { message = $"Không tìm thấy sản phẩm '{maSanPham}'." });
        }

        var query = _context.DanhGiaSanPhamDoiTacs
            .AsNoTracking()
            .Where(item => item.MaSanPham == maSanPhamDb);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var tongDanhGia = await query.CountAsync();
        var diemTrungBinh = tongDanhGia == 0
            ? (double?)null
            : await query.AverageAsync(item => (double?)item.SaoDanhGia);

        var danhGias = await query
            .OrderByDescending(item => item.ThoiGian)
            .ThenBy(item => item.MaDanhGia)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new
            {
                maDanhGia = FixedLengthHelper.TrimSafe(item.MaDanhGia),
                saoDanhGia = item.SaoDanhGia,
                nhanXet = item.NhanXet,
                thoiGian = item.ThoiGian,
                media = item.MediaDanhGiaSanPhams.OrderBy(media => media.ThuTu).Select(media => new
                {
                    url = media.Url,
                    loaiMedia = FixedLengthHelper.TrimSafe(media.LoaiMedia),
                    thuTu = media.ThuTu
                })
            })
            .ToListAsync();

        return Ok(new
        {
            maSanPham = FixedLengthHelper.TrimSafe(maSanPhamDb),
            diemTrungBinh,
            tongDanhGia,
            page,
            pageSize,
            danhGias
        });
    }

    // POST /api/DanhGia/tour
    [HttpPost("tour")]
    [Authorize]
    [Authorize(Roles = "KhachHang")]
    public async Task<ActionResult> CreateTourReview(DanhGiaTourCreateDto request)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.MaTour))
        {
            return BadRequest(new { message = "MaTour không được để trống." });
        }

        var saoValidation = ValidateSaoDanhGia(request.SaoDanhGia);

        if (saoValidation is not null)
        {
            return saoValidation;
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maTourDb = FixedLengthHelper.PadTo20(request.MaTour);
        var trangThaiHoanThanh = FixedLengthHelper.PadTo20("HoanThanh");

        var daHoanThanh = await _context.DatDichVus
            .AnyAsync(item =>
                item.MaUser == maUserDb &&
                item.MaTour == maTourDb &&
                item.TrangThai == trangThaiHoanThanh);

        if (!daHoanThanh)
        {
            return BadRequest(new
            {
                message = "Bạn cần hoàn thành tour này trước khi đánh giá."
            });
        }

        var daDanhGia = await _context.DanhGiaTours
            .AnyAsync(item =>
                item.MaUser == maUserDb &&
                item.MaTour == maTourDb);

        if (daDanhGia)
        {
            return Conflict(new
            {
                message = "Bạn đã đánh giá tour này. Dùng PUT để cập nhật đánh giá."
            });
        }

        var media = ParseMedia(request.MediaUrls, out var mediaError);
        if (mediaError is not null)
            return BadRequest(new { message = mediaError });

        var maDanhGiaTourDb = await GenerateMaDanhGiaTourAsync();

        var danhGia = new DanhGiaTour
        {
            MaDanhGiaTour = maDanhGiaTourDb,
            MaUser = maUserDb,
            MaTour = maTourDb,
            SaoDanhGia = request.SaoDanhGia,
            NhanXet = request.NhanXet?.Trim(),
            ThoiGian = DateTime.UtcNow
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();
        _context.DanhGiaTours.Add(danhGia);
        await _context.SaveChangesAsync();
        foreach (var item in media)
        {
            _context.MediaDanhGiaTours.Add(new MediaDanhGiaTour
            {
                MaMedia = await GenerateMediaTourIdAsync(),
                MaDanhGiaTour = danhGia.MaDanhGiaTour,
                Url = item.Url,
                LoaiMedia = item.LoaiMedia,
                ThuTu = item.ThuTu
            });
        }
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        await _hanhViLogger.LogAsync(maUserDb, maTourDb, "DanhGiaTour");

        return StatusCode(StatusCodes.Status201Created, new
        {
            maDanhGiaTour = FixedLengthHelper.TrimSafe(danhGia.MaDanhGiaTour),
            maTour = FixedLengthHelper.TrimSafe(danhGia.MaTour),
            saoDanhGia = danhGia.SaoDanhGia,
            nhanXet = danhGia.NhanXet,
            thoiGian = danhGia.ThoiGian
        });
    }

    // PUT /api/DanhGia/tour/{maDanhGiaTour}
    [HttpPut("tour/{maDanhGiaTour}")]
    [Authorize]
    [Authorize(Roles = "KhachHang")]
    public async Task<IActionResult> UpdateTourReview(
        string maDanhGiaTour,
        DanhGiaTourUpdateDto request)
    {
        if (User.IsInRole("Sale") || User.IsInRole("Admin"))
            return Forbid();

        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var saoValidation = ValidateSaoDanhGia(request.SaoDanhGia);

        if (saoValidation is not null)
        {
            return saoValidation;
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maDanhGiaTourDb = FixedLengthHelper.PadTo20(maDanhGiaTour);

        var danhGia = await _context.DanhGiaTours
            .FirstOrDefaultAsync(item =>
                item.MaDanhGiaTour == maDanhGiaTourDb &&
                item.MaUser == maUserDb);

        if (danhGia is null)
        {
            return NotFound(new { message = "Không tìm thấy đánh giá của bạn." });
        }

        danhGia.SaoDanhGia = request.SaoDanhGia;
        danhGia.NhanXet = request.NhanXet?.Trim();
        danhGia.ThoiGian = DateTime.UtcNow;

        var media = ParseMedia(request.MediaUrls, out var mediaError);
        if (mediaError is not null)
            return BadRequest(new { message = mediaError });

        await using var transaction = await _context.Database.BeginTransactionAsync();
        if (request.MediaUrls is not null)
        {
            _context.MediaDanhGiaTours.RemoveRange(_context.MediaDanhGiaTours
                .Where(item => item.MaDanhGiaTour == danhGia.MaDanhGiaTour));
            foreach (var item in media)
            {
                _context.MediaDanhGiaTours.Add(new MediaDanhGiaTour
                {
                    MaMedia = await GenerateMediaTourIdAsync(),
                    MaDanhGiaTour = danhGia.MaDanhGiaTour,
                    Url = item.Url,
                    LoaiMedia = item.LoaiMedia,
                    ThuTu = item.ThuTu
                });
            }
        }
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return NoContent();
    }

    // POST /api/DanhGia/huong-dan-vien
    [HttpPost("huong-dan-vien")]
    [Authorize]
    [Authorize(Roles = "KhachHang")]
    public async Task<ActionResult> CreateHdvReview(DanhGiaHdvCreateDto request)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.MaHdv))
        {
            return BadRequest(new { message = "MaHDV không được để trống." });
        }

        var saoValidation = ValidateSaoDanhGia(request.SaoDanhGia);

        if (saoValidation is not null)
        {
            return saoValidation;
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maHdvDb = FixedLengthHelper.PadTo20(request.MaHdv);
        var trangThaiHoanThanh = FixedLengthHelper.PadTo20("HoanThanh");

        var hdvTonTai = await _context.HuongDanViens
            .AnyAsync(item => item.MaHuongDanVien == maHdvDb);

        if (!hdvTonTai)
        {
            return BadRequest(new { message = "Hướng dẫn viên không tồn tại." });
        }

        var coBookingHoanThanh = await _context.DatDichVus
            .Where(item => item.MaUser == maUserDb &&
                           item.TrangThai == trangThaiHoanThanh &&
                           item.MaKhoiHanh != null)
            .Join(_context.LichDanTours,
                booking => booking.MaKhoiHanh!,
                assignment => assignment.MaKhoiHanh,
                (_, assignment) => assignment)
            .AnyAsync(item => item.MaHdv == maHdvDb);

        if (!coBookingHoanThanh)
        {
            return BadRequest(new
            {
                message = "Bạn chưa từng đi tour do hướng dẫn viên này dẫn."
            });
        }

        if (await _context.DanhGiaHdvs.AnyAsync(item =>
                item.MaUser == maUserDb && item.MaHdv == maHdvDb))
        {
            return Conflict(new { message = "Bạn đã đánh giá rồi." });
        }

        var media = ParseMedia(request.MediaUrls, out var mediaError);
        if (mediaError is not null)
            return BadRequest(new { message = mediaError });

        var maDanhGiaHdvDb = await GenerateMaDanhGiaHdvAsync();

        var danhGia = new DanhGiaHdv
        {
            MaDanhGiaHdv = maDanhGiaHdvDb,
            MaUser = maUserDb,
            MaHdv = maHdvDb,
            SaoDanhGia = request.SaoDanhGia,
            NhanXet = request.NhanXet?.Trim(),
            ThoiGian = DateTime.UtcNow
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();
        _context.DanhGiaHdvs.Add(danhGia);
        await _context.SaveChangesAsync();
        foreach (var item in media)
        {
            _context.MediaDanhGiaHdvs.Add(new MediaDanhGiaHdv
            {
                MaMedia = await GenerateMediaHdvIdAsync(),
                MaDanhGiaHdv = danhGia.MaDanhGiaHdv,
                Url = item.Url,
                LoaiMedia = item.LoaiMedia,
                ThuTu = item.ThuTu
            });
        }
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        await _hanhViLogger.LogAsync(maUserDb, null, "DanhGiaHdv");

        return StatusCode(StatusCodes.Status201Created, new
        {
            maDanhGiaHdv = FixedLengthHelper.TrimSafe(danhGia.MaDanhGiaHdv),
            maHdv = FixedLengthHelper.TrimSafe(danhGia.MaHdv),
            saoDanhGia = danhGia.SaoDanhGia,
            nhanXet = danhGia.NhanXet,
            thoiGian = danhGia.ThoiGian
        });
    }

    // POST /api/DanhGia/san-pham-doi-tac
    [HttpPost("san-pham-doi-tac")]
    [Authorize]
    [Authorize(Roles = "KhachHang")]
    public async Task<ActionResult> CreateSanPhamReview(
        DanhGiaSanPhamDoiTacCreateDto request)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.MaSanPham))
        {
            return BadRequest(new { message = "MaSanPham không được để trống." });
        }

        var saoValidation = ValidateSaoDanhGia(request.SaoDanhGia);

        if (saoValidation is not null)
        {
            return saoValidation;
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maSanPhamDb = FixedLengthHelper.PadTo20(request.MaSanPham);

        var sanPhamTonTai = await _context.SanPhamDoiTacs
            .AnyAsync(item => item.MaSanPham == maSanPhamDb);

        if (!sanPhamTonTai)
        {
            return BadRequest(new { message = "Sản phẩm đối tác không tồn tại." });
        }

        var daSuDung = await _context.DatDichVus
            .Where(item => item.MaUser == maUserDb &&
                           item.TrangThai == FixedLengthHelper.PadTo20("HoanThanh"))
            .Join(_context.LichTrinhs,
                booking => booking.MaTour,
                schedule => schedule.MaTour,
                (_, schedule) => schedule)
            .AnyAsync(item => item.MaSanPham == maSanPhamDb);

        if (!daSuDung)
        {
            return BadRequest(new
            {
                message = "Bạn chưa từng sử dụng sản phẩm này trong tour đã hoàn thành."
            });
        }

        if (await _context.DanhGiaSanPhamDoiTacs.AnyAsync(item =>
                item.MaUser == maUserDb && item.MaSanPham == maSanPhamDb))
        {
            return Conflict(new { message = "Bạn đã đánh giá rồi." });
        }

        var media = ParseMedia(request.MediaUrls, out var mediaError);
        if (mediaError is not null)
            return BadRequest(new { message = mediaError });

        var maDanhGiaDb = await GenerateMaDanhGiaSanPhamAsync();

        var danhGia = new DanhGiaSanPhamDoiTac
        {
            MaDanhGia = maDanhGiaDb,
            MaSanPham = maSanPhamDb,
            MaUser = maUserDb,
            SaoDanhGia = request.SaoDanhGia,
            NhanXet = request.NhanXet?.Trim(),
            ThoiGian = DateTime.UtcNow
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();
        _context.DanhGiaSanPhamDoiTacs.Add(danhGia);
        await _context.SaveChangesAsync();
        foreach (var item in media)
        {
            _context.MediaDanhGiaSanPhams.Add(new MediaDanhGiaSanPham
            {
                MaMedia = await GenerateMediaSanPhamIdAsync(),
                MaDanhGia = danhGia.MaDanhGia,
                Url = item.Url,
                LoaiMedia = item.LoaiMedia,
                ThuTu = item.ThuTu
            });
        }
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        await _hanhViLogger.LogAsync(maUserDb, null, "DanhGiaSanPham");

        return StatusCode(StatusCodes.Status201Created, new
        {
            maDanhGia = FixedLengthHelper.TrimSafe(danhGia.MaDanhGia),
            maSanPham = FixedLengthHelper.TrimSafe(danhGia.MaSanPham),
            saoDanhGia = danhGia.SaoDanhGia,
            nhanXet = danhGia.NhanXet,
            thoiGian = danhGia.ThoiGian
        });
    }

    private string? GetCurrentMaUser()
    {
        return User.FindFirst("MaUser")?.Value;
    }

    private static ActionResult? ValidateSaoDanhGia(int saoDanhGia)
    {
        if (saoDanhGia < 1 || saoDanhGia > 5)
        {
            return new BadRequestObjectResult(new
            {
                message = "SaoDanhGia phải nằm trong khoảng 1-5."
            });
        }

        return null;
    }

    private async Task<string> GenerateMaDanhGiaTourAsync()
    {
        string maDanhGiaTourDb;

        do
        {
            var maDanhGiaTour = $"DG{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            maDanhGiaTourDb = FixedLengthHelper.PadTo20(maDanhGiaTour);
        }
        while (await _context.DanhGiaTours
            .AnyAsync(item => item.MaDanhGiaTour == maDanhGiaTourDb));

        return maDanhGiaTourDb;
    }

    private async Task<string> GenerateMaDanhGiaHdvAsync()
    {
        string maDanhGiaHdvDb;

        do
        {
            var maDanhGiaHdv = $"DH{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            maDanhGiaHdvDb = FixedLengthHelper.PadTo20(maDanhGiaHdv);
        }
        while (await _context.DanhGiaHdvs
            .AnyAsync(item => item.MaDanhGiaHdv == maDanhGiaHdvDb));

        return maDanhGiaHdvDb;
    }

    private async Task<string> GenerateMaDanhGiaSanPhamAsync()
    {
        string maDanhGiaDb;

        do
        {
            var maDanhGia = $"DS{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            maDanhGiaDb = FixedLengthHelper.PadTo20(maDanhGia);
        }
        while (await _context.DanhGiaSanPhamDoiTacs
            .AnyAsync(item => item.MaDanhGia == maDanhGiaDb));

        return maDanhGiaDb;
    }

    private static List<MediaInput> ParseMedia(List<MediaItemDto>? items, out string? error)
    {
        var result = new List<MediaInput>();
        if (items is null)
        {
            error = null;
            return result;
        }

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Url))
            {
                error = "MediaUrl không được để trống.";
                return result;
            }
            if (item.LoaiMedia is not ("Anh" or "Video"))
            {
                error = "LoaiMedia chỉ nhận Anh hoặc Video.";
                return result;
            }
            result.Add(new MediaInput(item.Url.Trim(), item.LoaiMedia, result.Count));
        }

        error = null;
        return result;
    }

    private async Task<string> GenerateMediaTourIdAsync() => await GenerateMediaIdAsync(
        id => _context.MediaDanhGiaTours.AnyAsync(item => item.MaMedia == id), "MT");

    private async Task<string> GenerateMediaHdvIdAsync() => await GenerateMediaIdAsync(
        id => _context.MediaDanhGiaHdvs.AnyAsync(item => item.MaMedia == id), "MH");

    private async Task<string> GenerateMediaSanPhamIdAsync() => await GenerateMediaIdAsync(
        id => _context.MediaDanhGiaSanPhams.AnyAsync(item => item.MaMedia == id), "MS");

    private static async Task<string> GenerateMediaIdAsync(
        Func<string, Task<bool>> exists,
        string prefix)
    {
        string id;
        do
        {
            id = FixedLengthHelper.PadTo20($"{prefix}{Guid.NewGuid():N}"[..20].ToUpperInvariant());
        } while (await exists(id));
        return id;
    }

    private sealed record MediaInput(string Url, string LoaiMedia, int ThuTu);
}
