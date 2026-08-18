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

        var vaiTroTonTai = await _context.VaiTros
            .AnyAsync(role => role.MaVaiTro == request.MaVaiTro);

        if (!vaiTroTonTai)
        {
            return BadRequest(new { message = "Vai trò không tồn tại." });
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
            MaVaiTro = request.MaVaiTro
        };

        _context.NguoiSuDungs.Add(nguoiSuDung);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            maUser = FixedLengthHelper.TrimSafe(nguoiSuDung.MaUser),
            soDienThoai = FixedLengthHelper.TrimSafe(nguoiSuDung.SoDienThoai),
            maVaiTro = nguoiSuDung.MaVaiTro
        });
    }

    [HttpPost("login")]
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
            .FirstOrDefaultAsync(user => user.SoDienThoai == soDienThoaiDb);

        if (nguoiSuDung is null ||
            !BCrypt.Net.BCrypt.Verify(matKhau, nguoiSuDung.MatKhau))
        {
            return Unauthorized(new { message = "Sai số điện thoại hoặc mật khẩu" });
        }

        var maUser = FixedLengthHelper.TrimSafe(nguoiSuDung.MaUser)!;
        var token = _jwtTokenService.GenerateToken(maUser, nguoiSuDung.MaVaiTro);

        return Ok(new
        {
            token,
            maUser,
            maVaiTro = nguoiSuDung.MaVaiTro
        });
    }
}