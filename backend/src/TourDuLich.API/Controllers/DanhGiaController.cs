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
public class DanhGiaController : ControllerBase
{
    private readonly AppDbContext _context;

    public DanhGiaController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/DanhGia/tour/{maTour}
    [HttpGet("tour/{maTour}")]
    public async Task<ActionResult> GetTourReviews(string maTour)
    {
        var maTourDb = FixedLengthHelper.PadTo20(maTour);

        var tourTonTai = await _context.Tours
            .AnyAsync(item => item.MaTour == maTourDb);

        if (!tourTonTai)
        {
            return NotFound(new { message = $"Không tìm thấy tour '{maTour}'." });
        }

        var danhGias = await _context.DanhGiaTours
            .AsNoTracking()
            .Where(item => item.MaTour == maTourDb)
            .OrderByDescending(item => item.ThoiGian)
            .Select(item => new
            {
                maDanhGiaTour = FixedLengthHelper.TrimSafe(item.MaDanhGiaTour),
                maUser = FixedLengthHelper.TrimSafe(item.MaUser),
                saoDanhGia = item.SaoDanhGia,
                nhanXet = item.NhanXet,
                thoiGian = item.ThoiGian
            })
            .ToListAsync();

        var diemTrungBinh = danhGias.Count == 0
            ? (double?)null
            : danhGias.Average(item => item.saoDanhGia ?? 0);

        return Ok(new
        {
            maTour = FixedLengthHelper.TrimSafe(maTourDb),
            diemTrungBinh,
            tongDanhGia = danhGias.Count,
            danhGias
        });
    }

    // GET /api/DanhGia/huong-dan-vien/{maHDV}
    [HttpGet("huong-dan-vien/{maHdv}")]
    public async Task<ActionResult> GetHdvReviews(string maHdv)
    {
        var maHdvDb = FixedLengthHelper.PadTo20(maHdv);

        var hdvTonTai = await _context.HuongDanViens
            .AnyAsync(item => item.MaHuongDanVien == maHdvDb);

        if (!hdvTonTai)
        {
            return NotFound(new { message = $"Không tìm thấy hướng dẫn viên '{maHdv}'." });
        }

        var danhGias = await _context.DanhGiaHdvs
            .AsNoTracking()
            .Where(item => item.MaHdv == maHdvDb)
            .OrderByDescending(item => item.ThoiGian)
            .Select(item => new
            {
                maDanhGiaHdv = FixedLengthHelper.TrimSafe(item.MaDanhGiaHdv),
                maUser = FixedLengthHelper.TrimSafe(item.MaUser),
                saoDanhGia = item.SaoDanhGia,
                nhanXet = item.NhanXet,
                thoiGian = item.ThoiGian
            })
            .ToListAsync();

        var diemTrungBinh = danhGias.Count == 0
            ? (double?)null
            : danhGias.Average(item => item.saoDanhGia);

        return Ok(new
        {
            maHdv = FixedLengthHelper.TrimSafe(maHdvDb),
            diemTrungBinh,
            tongDanhGia = danhGias.Count,
            danhGias
        });
    }

    // GET /api/DanhGia/san-pham-doi-tac/{maSanPham}
    [HttpGet("san-pham-doi-tac/{maSanPham}")]
    public async Task<ActionResult> GetSanPhamReviews(string maSanPham)
    {
        var maSanPhamDb = FixedLengthHelper.PadTo20(maSanPham);

        var sanPhamTonTai = await _context.SanPhamDoiTacs
            .AnyAsync(item => item.MaSanPham == maSanPhamDb);

        if (!sanPhamTonTai)
        {
            return NotFound(new { message = $"Không tìm thấy sản phẩm '{maSanPham}'." });
        }

        var danhGias = await _context.DanhGiaSanPhamDoiTacs
            .AsNoTracking()
            .Where(item => item.MaSanPham == maSanPhamDb)
            .OrderByDescending(item => item.ThoiGian)
            .Select(item => new
            {
                maDanhGia = FixedLengthHelper.TrimSafe(item.MaDanhGia),
                maUser = FixedLengthHelper.TrimSafe(item.MaUser),
                saoDanhGia = item.SaoDanhGia,
                nhanXet = item.NhanXet,
                thoiGian = item.ThoiGian
            })
            .ToListAsync();

        var diemTrungBinh = danhGias.Count == 0
            ? (double?)null
            : danhGias.Average(item => item.saoDanhGia);

        return Ok(new
        {
            maSanPham = FixedLengthHelper.TrimSafe(maSanPhamDb),
            diemTrungBinh,
            tongDanhGia = danhGias.Count,
            danhGias
        });
    }

    // POST /api/DanhGia/tour
    [HttpPost("tour")]
    [Authorize]
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

        _context.DanhGiaTours.Add(danhGia);
        await _context.SaveChangesAsync();

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
    public async Task<IActionResult> UpdateTourReview(
        string maDanhGiaTour,
        DanhGiaTourUpdateDto request)
    {
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

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/DanhGia/huong-dan-vien
    [HttpPost("huong-dan-vien")]
    [Authorize]
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

        // TODO: validate chặt hơn, kiểm tra đúng user đã đi tour do chính HDV này dẫn
        var coBookingHoanThanh = await _context.DatDichVus
            .AnyAsync(item =>
                item.MaUser == maUserDb &&
                item.TrangThai == trangThaiHoanThanh);

        if (!coBookingHoanThanh)
        {
            return BadRequest(new
            {
                message = "Bạn cần hoàn thành ít nhất một tour trước khi đánh giá hướng dẫn viên."
            });
        }

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

        _context.DanhGiaHdvs.Add(danhGia);
        await _context.SaveChangesAsync();

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

        _context.DanhGiaSanPhamDoiTacs.Add(danhGia);
        await _context.SaveChangesAsync();

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
}
