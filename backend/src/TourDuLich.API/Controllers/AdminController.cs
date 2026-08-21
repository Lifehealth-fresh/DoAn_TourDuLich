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
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("tai-khoan")]
    public async Task<ActionResult> GetAccounts(
        [FromQuery] string? tenVaiTro,
        [FromQuery] string? soDienThoai)
    {
        var query = _context.NguoiSuDungs
            .AsNoTracking()
            .Include(user => user.MaVaiTroNavigation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(tenVaiTro))
        {
            var roleName = tenVaiTro.Trim().ToLower();
            query = query.Where(user =>
                user.MaVaiTroNavigation.TenVaiTro.Trim().ToLower() == roleName);
        }

        if (!string.IsNullOrWhiteSpace(soDienThoai))
        {
            var phone = soDienThoai.Trim();
            query = query.Where(user =>
                user.SoDienThoai.Trim().Contains(phone));
        }

        var accounts = await query
            .OrderBy(user => user.SoDienThoai)
            .Select(user => new
            {
                maUser = FixedLengthHelper.TrimSafe(user.MaUser),
                soDienThoai = FixedLengthHelper.TrimSafe(user.SoDienThoai),
                tenVaiTro = user.MaVaiTroNavigation.TenVaiTro.Trim()
            })
            .ToListAsync();

        return Ok(accounts);
    }

    [HttpGet("tai-khoan/{maUser}")]
    public async Task<ActionResult> GetAccount(string maUser)
    {
        var maUserDb = FixedLengthHelper.PadTo20(maUser);

        var account = await _context.NguoiSuDungs
            .AsNoTracking()
            .Include(user => user.MaVaiTroNavigation)
            .Where(user => user.MaUser == maUserDb)
            .Select(user => new
            {
                maUser = FixedLengthHelper.TrimSafe(user.MaUser),
                soDienThoai = FixedLengthHelper.TrimSafe(user.SoDienThoai),
                tenVaiTro = user.MaVaiTroNavigation.TenVaiTro.Trim()
            })
            .FirstOrDefaultAsync();

        return account is null
            ? NotFound(new { message = "Không tìm thấy tài khoản." })
            : Ok(account);
    }

    [HttpPost("tai-khoan/sale")]
    public async Task<ActionResult> CreateSale(AdminCreateSaleDto request)
    {
        var phone = request.SoDienThoai?.Trim();
        var password = request.MatKhau?.Trim();

        if (string.IsNullOrWhiteSpace(phone) ||
            string.IsNullOrWhiteSpace(password))
        {
            return BadRequest(new
            {
                message = "Số điện thoại và mật khẩu không được để trống."
            });
        }

        var phoneDb = FixedLengthHelper.PadTo20(phone);

        if (await _context.NguoiSuDungs
            .AnyAsync(user => user.SoDienThoai == phoneDb))
        {
            return Conflict(new
            {
                message = "Số điện thoại đã được đăng ký."
            });
        }

        var saleRole = await FindRoleAsync("Sale");

        if (saleRole is null)
        {
            return BadRequest(new
            {
                message = "Chưa cấu hình vai trò Sale trong hệ thống."
            });
        }

        var maUserDb = await GenerateUserIdAsync();

        var account = new NguoiSuDung
        {
            MaUser = maUserDb,
            SoDienThoai = phoneDb,
            MatKhau = BCrypt.Net.BCrypt.HashPassword(password),
            MaVaiTro = saleRole.MaVaiTro
        };

        _context.NguoiSuDungs.Add(account);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            maUser = FixedLengthHelper.TrimSafe(account.MaUser),
            soDienThoai = FixedLengthHelper.TrimSafe(account.SoDienThoai),
            tenVaiTro = saleRole.TenVaiTro.Trim()
        });
    }

    [HttpPut("tai-khoan/{maUser}/vai-tro")]
    public async Task<ActionResult> ChangeRole(
        string maUser,
        AdminChangeRoleDto request)
    {
        var currentUser = User.FindFirst("MaUser")?.Value;
        var targetUserDb = FixedLengthHelper.PadTo20(maUser);

        if (currentUser is not null &&
            FixedLengthHelper.PadTo20(currentUser) == targetUserDb)
        {
            return BadRequest(new
            {
                message = "Admin không được tự thay đổi vai trò của chính mình."
            });
        }

        if (string.IsNullOrWhiteSpace(request.TenVaiTro))
        {
            return BadRequest(new { message = "Tên vai trò không được để trống." });
        }

        var account = await _context.NguoiSuDungs
            .FirstOrDefaultAsync(user => user.MaUser == targetUserDb);

        if (account is null)
        {
            return NotFound(new { message = "Không tìm thấy tài khoản." });
        }

        var role = await FindRoleAsync(request.TenVaiTro);

        if (role is null)
        {
            return BadRequest(new { message = "Vai trò không tồn tại." });
        }

        account.MaVaiTro = role.MaVaiTro;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            maUser = FixedLengthHelper.TrimSafe(account.MaUser),
            tenVaiTro = role.TenVaiTro.Trim()
        });
    }

    private async Task<VaiTro?> FindRoleAsync(string roleName)
    {
        var normalizedName = roleName.Trim().ToLower();

        return await _context.VaiTros
            .FirstOrDefaultAsync(role =>
                role.TenVaiTro.Trim().ToLower() == normalizedName);
    }

    private async Task<string> GenerateUserIdAsync()
    {
        string maUserDb;

        do
        {
            var maUser = Guid.NewGuid().ToString("N")[..20].ToUpperInvariant();
            maUserDb = FixedLengthHelper.PadTo20(maUser);
        }
        while (await _context.NguoiSuDungs
            .AnyAsync(user => user.MaUser == maUserDb));

        return maUserDb;
    }
}
