using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using TourDuLich.API.Authorization;
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
    private readonly RefreshTokenService _refreshTokenService;
    private readonly IPermissionService _permissions;

    public AuthController(
        AppDbContext context,
        JwtTokenService jwtTokenService,
        RefreshTokenService refreshTokenService,
        IPermissionService permissions)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _permissions = permissions;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult> Register(RegisterDto request)
    {
        var soDienThoai = request.SoDienThoai?.Trim();
        var matKhau = request.MatKhau?.Trim();

        var credentialError = CredentialRules.Validate(soDienThoai, matKhau);
        if (credentialError is not null)
            return BadRequest(new { message = credentialError });

        var soDienThoaiDb = FixedLengthHelper.PadTo20(soDienThoai!);

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

        return StatusCode(StatusCodes.Status201Created, await ToSessionAsync(
            nguoiSuDung,
            vaiTroKhachHang.TenVaiTro.Trim()));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult> Login(LoginDto request)
    {
        var soDienThoai = request.SoDienThoai?.Trim();
        var matKhau = request.MatKhau?.Trim();

        if (string.IsNullOrWhiteSpace(soDienThoai) || string.IsNullOrEmpty(matKhau))
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

        return Ok(await ToSessionAsync(
            nguoiSuDung,
            nguoiSuDung.MaVaiTroNavigation.TenVaiTro.Trim()));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult> Refresh(RefreshTokenDto request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Unauthorized(new { message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." });
        var existing = await _refreshTokenService.FindActiveAsync(request.RefreshToken);
        if (existing is null)
            return Unauthorized(new { message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." });

        await _refreshTokenService.RevokeAsync(existing);
        var user = existing.MaUserNavigation;
        var tenVaiTro = user.MaVaiTroNavigation.TenVaiTro.Trim();
        return Ok(await ToSessionAsync(user, tenVaiTro));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<ActionResult> Logout(RefreshTokenDto? request)
    {
        var maUser = User.FindFirst("MaUser")?.Value;
        if (!string.IsNullOrWhiteSpace(maUser))
        {
            await _refreshTokenService.RevokeAllForUserAsync(maUser);
            return Ok(new { message = "Đã đăng xuất." });
        }

        if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            var existing = await _refreshTokenService.FindActiveAsync(request.RefreshToken);
            if (existing is not null)
                await _refreshTokenService.RevokeAsync(existing);
        }

        return Ok(new { message = "Đã đăng xuất." });
    }

    [HttpGet("toi")]
    [Authorize]
    public async Task<ActionResult> Me()
    {
        var maUser = User.FindFirst("MaUser")?.Value;
        var tenVaiTro = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (string.IsNullOrWhiteSpace(maUser))
            return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });

        return Ok(new
        {
            maUser,
            tenVaiTro,
            quyen = await _permissions.GetEffectiveGrantsAsync(maUser, tenVaiTro)
        });
    }

    private async Task<object> ToSessionAsync(NguoiSuDung nguoiSuDung, string tenVaiTro)
    {
        var maUser = FixedLengthHelper.TrimSafe(nguoiSuDung.MaUser)!;
        var token = _jwtTokenService.GenerateToken(maUser, nguoiSuDung.MaVaiTro, tenVaiTro);
        var refreshToken = await _refreshTokenService.IssueAsync(maUser);
        return new
        {
            token,
            refreshToken,
            expiresIn = _jwtTokenService.AccessTokenSeconds,
            maUser,
            soDienThoai = FixedLengthHelper.TrimSafe(nguoiSuDung.SoDienThoai),
            maVaiTro = nguoiSuDung.MaVaiTro,
            tenVaiTro,
            quyen = await _permissions.GetEffectiveGrantsAsync(maUser, tenVaiTro)
        };
    }
}
