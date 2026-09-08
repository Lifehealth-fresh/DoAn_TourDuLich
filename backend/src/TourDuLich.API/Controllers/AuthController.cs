using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using TourDuLich.API.DTOs;
using TourDuLich.Application.Helpers;
using TourDuLich.Application.Services;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(AppDbContext context, JwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult> Register(RegisterDto request)
    {
        var soDienThoai = request.SoDienThoai?.Trim();
        var matKhau = request.MatKhau?.Trim();

        if (string.IsNullOrWhiteSpace(soDienThoai) || string.IsNullOrWhiteSpace(matKhau))
        {
            return BadRequest(new { message = "Số điện thoại và mật khẩu không được để trống." });
        }

        var soDienThoaiDb = FixedLengthHelper.PadTo20(soDienThoai);

        var daTonTai = await _context.NguoiSuDungs
            .AnyAsync(user => user.SoDienThoai == soDienThoaiDb);

        if (daTonTai)
        {
            return Conflict(new { message = "Số điện thoại đã được đăng ký." });
        }

        var vaiTroKhachHang = await _context.VaiTros
            .FirstOrDefaultAsync(role =>
                role.TenVaiTro.Trim().ToLower() == "khachhang");

        if (vaiTroKhachHang is null)
        {
            return BadRequest(new { message = "Chưa cấu hình vai trò KhachHang trong hệ thống." });
        }

        string maUser;
        string maUserDb;

        do
        {
            maUser = Guid.NewGuid().ToString("N")[..20].ToUpperInvariant();
            maUserDb = FixedLengthHelper.PadTo20(maUser);
        }
        while (await _context.NguoiSuDungs
            .AnyAsync(user => user.MaUser == maUserDb));

        var nguoiSuDung = new NguoiSuDung
        {
            MaUser = maUserDb,
            SoDienThoai = soDienThoaiDb,
            MatKhau = BCrypt.Net.BCrypt.HashPassword(matKhau),
            MaVaiTro = vaiTroKhachHang.MaVaiTro
        };

        _context.NguoiSuDungs.Add(nguoiSuDung);
        await _context.SaveChangesAsync();

        var token = _jwtTokenService.GenerateToken(
            FixedLengthHelper.TrimSafe(nguoiSuDung.MaUser)!,
            nguoiSuDung.MaVaiTro,
            vaiTroKhachHang.TenVaiTro.Trim());

        return StatusCode(StatusCodes.Status201Created, new
        {
            token,
            maUser = FixedLengthHelper.TrimSafe(nguoiSuDung.MaUser),
            soDienThoai = FixedLengthHelper.TrimSafe(nguoiSuDung.SoDienThoai),
            maVaiTro = nguoiSuDung.MaVaiTro
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult> Login(LoginDto request)
    {
        var soDienThoai = request.SoDienThoai?.Trim();
        var matKhau = request.MatKhau?.Trim();

        if (string.IsNullOrWhiteSpace(soDienThoai) || string.IsNullOrWhiteSpace(matKhau))
        {
            return Unauthorized(new { message = "Sai số điện thoại hoặc mật khẩu" });
        }

        var soDienThoaiDb = FixedLengthHelper.PadTo20(soDienThoai);

        var nguoiSuDung = await _context.NguoiSuDungs
            .Include(user => user.MaVaiTroNavigation)
            .FirstOrDefaultAsync(user => user.SoDienThoai == soDienThoaiDb);

        if (nguoiSuDung is null ||
            !BCrypt.Net.BCrypt.Verify(matKhau, nguoiSuDung.MatKhau))
        {
            return Unauthorized(new { message = "Sai số điện thoại hoặc mật khẩu" });
        }

        var maUser = FixedLengthHelper.TrimSafe(nguoiSuDung.MaUser)!;
        var token = _jwtTokenService.GenerateToken(
            maUser,
            nguoiSuDung.MaVaiTro,
            nguoiSuDung.MaVaiTroNavigation.TenVaiTro.Trim());

        return Ok(new
        {
            token,
            maUser,
            maVaiTro = nguoiSuDung.MaVaiTro
        });
    }
}
