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
public class KhachHangController : ControllerBase
{
    private readonly AppDbContext _context;

    public KhachHangController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("me")]
    public async Task<ActionResult> GetMyProfile()
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);

        var khachHang = await _context.KhachHangs
            .Where(kh => kh.MaUser == maUserDb)
            .Select(kh => new
            {
                maKhachHang = FixedLengthHelper.TrimSafe(kh.MaKhachHang),
                ho = kh.Ho,
                ten = kh.Ten,
                hoGiayTo = kh.HoGiayTo,
                tenGiayTo = kh.TenGiayTo,
                quocTich = kh.QuocTich,
                danhXung = FixedLengthHelper.TrimSafe(kh.DanhXung),
                gioiTinh = FixedLengthHelper.TrimSafe(kh.GioiTinh),
                ngaySinh = kh.NgaySinh,
                email = kh.Email,
                soDienThoai = FixedLengthHelper.TrimSafe(kh.SoDienThoai),
                giayTos = kh.GiayTos.Select(gt => new
                {
                    maGiayTo = FixedLengthHelper.TrimSafe(gt.MaGiayTo),
                    loaiGiayTo = gt.LoaiGiayTo,
                    soTrenGiayTo = gt.SoTrenGiayTo,
                    ngayCap = gt.NgayCap,
                    ngayHetHan = gt.NgayHetHan,
                    noiCap = gt.NoiCap
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (khachHang is null)
        {
            return NotFound(new { message = "Bạn chưa có hồ sơ khách hàng." });
        }

        return Ok(khachHang);
    }

    [HttpPost("me")]
    public async Task<ActionResult> CreateMyProfile(KhachHangCreateDto request)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Ho) || string.IsNullOrWhiteSpace(request.Ten))
        {
            return BadRequest(new { message = "Họ và tên không được để trống." });
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);

        var daCoHoSo = await _context.KhachHangs
            .AnyAsync(kh => kh.MaUser == maUserDb);

        if (daCoHoSo)
        {
            return Conflict(new { message = "Tài khoản này đã có hồ sơ khách hàng." });
        }

        var nguoiSuDung = await _context.NguoiSuDungs
            .FirstOrDefaultAsync(user => user.MaUser == maUserDb);

        if (nguoiSuDung is null)
        {
            return Unauthorized();
        }

        var soDienThoai = FixedLengthHelper.TrimSafe(nguoiSuDung.SoDienThoai) ?? string.Empty;

        if (soDienThoai.Length > 15)
        {
            return BadRequest(new
            {
                message = "Số điện thoại tài khoản vượt quá giới hạn 15 ký tự của hồ sơ khách hàng."
            });
        }

        string maKhachHang;
        string maKhachHangDb;

        do
        {
            maKhachHang = $"KH{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            maKhachHangDb = FixedLengthHelper.PadTo20(maKhachHang);
        }
        while (await _context.KhachHangs
            .AnyAsync(kh => kh.MaKhachHang == maKhachHangDb));

        var khachHang = new KhachHang
        {
            MaKhachHang = maKhachHangDb,
            Ho = request.Ho.Trim(),
            Ten = request.Ten.Trim(),
            HoGiayTo = request.HoGiayTo?.Trim(),
            TenGiayTo = request.TenGiayTo?.Trim(),
            QuocTich = request.QuocTich?.Trim(),
            DanhXung = request.DanhXung?.Trim(),
            GioiTinh = request.GioiTinh?.Trim(),
            NgaySinh = request.NgaySinh,
            Email = request.Email?.Trim(),
            SoDienThoai = soDienThoai,
            MaUser = maUserDb
        };

        _context.KhachHangs.Add(khachHang);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            maKhachHang = FixedLengthHelper.TrimSafe(khachHang.MaKhachHang),
            ho = khachHang.Ho,
            ten = khachHang.Ten,
            soDienThoai = FixedLengthHelper.TrimSafe(khachHang.SoDienThoai)
        });
    }

    [HttpPut("me")]
    public async Task<ActionResult> UpdateMyProfile(KhachHangUpdateDto request)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Ho) || string.IsNullOrWhiteSpace(request.Ten))
        {
            return BadRequest(new { message = "Họ và tên không được để trống." });
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);

        var khachHang = await _context.KhachHangs
            .FirstOrDefaultAsync(kh => kh.MaUser == maUserDb);

        if (khachHang is null)
        {
            return NotFound(new { message = "Bạn chưa có hồ sơ khách hàng." });
        }

        khachHang.Ho = request.Ho.Trim();
        khachHang.Ten = request.Ten.Trim();
        khachHang.HoGiayTo = request.HoGiayTo?.Trim();
        khachHang.TenGiayTo = request.TenGiayTo?.Trim();
        khachHang.QuocTich = request.QuocTich?.Trim();
        khachHang.DanhXung = request.DanhXung?.Trim();
        khachHang.GioiTinh = request.GioiTinh?.Trim();
        khachHang.NgaySinh = request.NgaySinh;
        khachHang.Email = request.Email?.Trim();

        await _context.SaveChangesAsync();

        return Ok(new
        {
            maKhachHang = FixedLengthHelper.TrimSafe(khachHang.MaKhachHang),
            ho = khachHang.Ho,
            ten = khachHang.Ten
        });
    }

    [HttpPost("me/giay-to")]
    public async Task<ActionResult> AddMyDocument(GiayToCreateDto request)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.LoaiGiayTo) ||
            string.IsNullOrWhiteSpace(request.SoTrenGiayTo) ||
            string.IsNullOrWhiteSpace(request.NoiCap))
        {
            return BadRequest(new { message = "Thông tin giấy tờ không được để trống." });
        }

        if (request.NgayHetHan < request.NgayCap)
        {
            return BadRequest(new { message = "Ngày hết hạn phải sau hoặc bằng ngày cấp." });
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);

        var khachHang = await _context.KhachHangs
            .FirstOrDefaultAsync(kh => kh.MaUser == maUserDb);

        if (khachHang is null)
        {
            return NotFound(new { message = "Bạn chưa có hồ sơ khách hàng." });
        }

        string maGiayTo;
        string maGiayToDb;

        do
        {
            maGiayTo = $"GT{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            maGiayToDb = FixedLengthHelper.PadTo20(maGiayTo);
        }
        while (await _context.GiayTos
            .AnyAsync(gt => gt.MaGiayTo == maGiayToDb));

        var giayTo = new GiayTo
        {
            MaGiayTo = maGiayToDb,
            LoaiGiayTo = request.LoaiGiayTo.Trim(),
            SoTrenGiayTo = request.SoTrenGiayTo.Trim(),
            NgayCap = request.NgayCap,
            NgayHetHan = request.NgayHetHan,
            NoiCap = request.NoiCap.Trim(),
            MaKhachHang = khachHang.MaKhachHang
        };

        _context.GiayTos.Add(giayTo);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            maGiayTo = FixedLengthHelper.TrimSafe(giayTo.MaGiayTo),
            loaiGiayTo = giayTo.LoaiGiayTo,
            soTrenGiayTo = giayTo.SoTrenGiayTo
        });
    }

    [HttpGet("me/giay-to")]
    public async Task<ActionResult> GetMyDocuments()
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);

        var giayTos = await _context.KhachHangs
            .Where(kh => kh.MaUser == maUserDb)
            .SelectMany(kh => kh.GiayTos.Select(gt => new
            {
                maGiayTo = FixedLengthHelper.TrimSafe(gt.MaGiayTo),
                loaiGiayTo = gt.LoaiGiayTo,
                soTrenGiayTo = gt.SoTrenGiayTo,
                ngayCap = gt.NgayCap,
                ngayHetHan = gt.NgayHetHan,
                noiCap = gt.NoiCap
            }))
            .ToListAsync();

        var coHoSo = await _context.KhachHangs
            .AnyAsync(kh => kh.MaUser == maUserDb);

        if (!coHoSo)
        {
            return NotFound(new { message = "Bạn chưa có hồ sơ khách hàng." });
        }

        return Ok(giayTos);
    }

    [HttpPut("me/giay-to/{maGiayTo}")]
    public async Task<ActionResult> UpdateMyDocument(
    string maGiayTo,
    GiayToUpdateDto request)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.LoaiGiayTo) ||
            string.IsNullOrWhiteSpace(request.SoTrenGiayTo) ||
            string.IsNullOrWhiteSpace(request.NoiCap))
        {
            return BadRequest(new { message = "Thông tin giấy tờ không được để trống." });
        }

        if (request.NgayHetHan < request.NgayCap)
        {
            return BadRequest(new { message = "Ngày hết hạn phải sau hoặc bằng ngày cấp." });
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maGiayToDb = FixedLengthHelper.PadTo20(maGiayTo);

        var giayTo = await _context.GiayTos
            .FirstOrDefaultAsync(gt =>
                gt.MaGiayTo == maGiayToDb &&
                gt.MaKhachHangNavigation.MaUser == maUserDb);

        if (giayTo is null)
        {
            return NotFound(new { message = "Không tìm thấy giấy tờ thuộc hồ sơ của bạn." });
        }

        giayTo.LoaiGiayTo = request.LoaiGiayTo.Trim();
        giayTo.SoTrenGiayTo = request.SoTrenGiayTo.Trim();
        giayTo.NgayCap = request.NgayCap;
        giayTo.NgayHetHan = request.NgayHetHan;
        giayTo.NoiCap = request.NoiCap.Trim();

        await _context.SaveChangesAsync();

        return Ok(new
        {
            maGiayTo = FixedLengthHelper.TrimSafe(giayTo.MaGiayTo),
            loaiGiayTo = giayTo.LoaiGiayTo,
            soTrenGiayTo = giayTo.SoTrenGiayTo,
            ngayCap = giayTo.NgayCap,
            ngayHetHan = giayTo.NgayHetHan,
            noiCap = giayTo.NoiCap
        });
    }

    [HttpDelete("me/giay-to/{maGiayTo}")]
    public async Task<IActionResult> DeleteMyDocument(string maGiayTo)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maGiayToDb = FixedLengthHelper.PadTo20(maGiayTo);

        var giayTo = await _context.GiayTos
            .FirstOrDefaultAsync(gt =>
                gt.MaGiayTo == maGiayToDb &&
                gt.MaKhachHangNavigation.MaUser == maUserDb);

        if (giayTo is null)
        {
            return NotFound(new { message = "Không tìm thấy giấy tờ thuộc hồ sơ của bạn." });
        }

        _context.GiayTos.Remove(giayTo);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private string? GetCurrentMaUser()
    {
        return User.FindFirst("MaUser")?.Value;
    }
}