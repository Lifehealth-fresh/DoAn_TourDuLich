using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourDuLich.API.Authorization;
using TourDuLich.API.DTOs;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Sale,Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("chuc-nang")]
    [RequirePermission(PermissionCatalog.TaiKhoan, PermissionCatalog.Xem)]
    public ActionResult GetModules() => Ok(PermissionCatalog.Modules.Select(module => new
    {
        ma = module.Ma,
        ten = module.Ten
    }));

    [HttpGet("tai-khoan")]
    [RequirePermission(PermissionCatalog.TaiKhoan, PermissionCatalog.Xem)]
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
        else
        {
            query = query.Where(user =>
                user.MaVaiTroNavigation.TenVaiTro.Trim() != "KhachHang");
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
                maUser = user.MaUser,
                soDienThoai = user.SoDienThoai,
                tenVaiTro = user.MaVaiTroNavigation.TenVaiTro
            })
            .ToListAsync();

        var ids = accounts.Select(item => item.maUser).ToList();
        var stored = await _context.QuyenNhanViens
            .AsNoTracking()
            .Where(item => ids.Contains(item.MaUser))
            .ToListAsync();

        var payload = accounts.Select(account =>
        {
            var role = account.tenVaiTro.Trim();
            var rows = stored.Where(item => item.MaUser == account.maUser);
            return new
            {
                maUser = FixedLengthHelper.TrimSafe(account.maUser),
                soDienThoai = FixedLengthHelper.TrimSafe(account.soDienThoai),
                tenVaiTro = role,
                quyen = MapGrants(role, rows)
            };
        });

        return Ok(payload);
    }

    [HttpGet("tai-khoan/{maUser}")]
    [RequirePermission(PermissionCatalog.TaiKhoan, PermissionCatalog.Xem)]
    public async Task<ActionResult> GetAccount(string maUser)
    {
        var maUserDb = FixedLengthHelper.PadTo20(maUser);

        var account = await _context.NguoiSuDungs
            .AsNoTracking()
            .Include(user => user.MaVaiTroNavigation)
            .FirstOrDefaultAsync(user => user.MaUser == maUserDb);

        if (account is null)
            return NotFound(new { message = "Không tìm thấy tài khoản." });

        var rows = await _context.QuyenNhanViens
            .AsNoTracking()
            .Where(item => item.MaUser == maUserDb)
            .ToListAsync();

        var role = account.MaVaiTroNavigation.TenVaiTro.Trim();
        return Ok(new
        {
            maUser = FixedLengthHelper.TrimSafe(account.MaUser),
            soDienThoai = FixedLengthHelper.TrimSafe(account.SoDienThoai),
            tenVaiTro = role,
            quyen = MapGrants(role, rows)
        });
    }

    [HttpPost("tai-khoan")]
    [RequirePermission(PermissionCatalog.TaiKhoan, PermissionCatalog.Them)]
    public async Task<ActionResult> CreateAccount(AdminCreateAccountDto request)
        => await CreateStaffAsync(request);

    [HttpPost("tai-khoan/sale")]
    [RequirePermission(PermissionCatalog.TaiKhoan, PermissionCatalog.Them)]
    public async Task<ActionResult> CreateSale(AdminCreateSaleDto request)
        => await CreateStaffAsync(new AdminCreateAccountDto
        {
            SoDienThoai = request.SoDienThoai,
            MatKhau = request.MatKhau,
            TenVaiTro = "Sale"
        });

    [HttpPut("tai-khoan/{maUser}/quyen")]
    [RequirePermission(PermissionCatalog.TaiKhoan, PermissionCatalog.Sua)]
    public async Task<ActionResult> UpdateGrants(string maUser, AdminUpdateQuyenDto request)
    {
        var targetUserDb = FixedLengthHelper.PadTo20(maUser);
        var account = await _context.NguoiSuDungs
            .Include(user => user.MaVaiTroNavigation)
            .FirstOrDefaultAsync(user => user.MaUser == targetUserDb);

        if (account is null)
            return NotFound(new { message = "Không tìm thấy tài khoản." });

        var role = account.MaVaiTroNavigation.TenVaiTro.Trim();
        if (string.Equals(role, "KhachHang", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Không gán quyền nghiệp vụ cho tài khoản khách hàng." });

        var error = GuardSelfTaiKhoan(account.MaUser, request.Quyen);
        if (error is not null)
            return BadRequest(new { message = error });

        await ReplaceGrantsAsync(account.MaUser, request.Quyen);
        await _context.SaveChangesAsync();

        var rows = await _context.QuyenNhanViens
            .AsNoTracking()
            .Where(item => item.MaUser == account.MaUser)
            .ToListAsync();

        return Ok(new
        {
            maUser = FixedLengthHelper.TrimSafe(account.MaUser),
            soDienThoai = FixedLengthHelper.TrimSafe(account.SoDienThoai),
            tenVaiTro = role,
            quyen = MapGrants(role, rows)
        });
    }

    [HttpPut("tai-khoan/{maUser}/vai-tro")]
    [RequirePermission(PermissionCatalog.TaiKhoan, PermissionCatalog.Sua)]
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

        if (string.Equals(role.TenVaiTro.Trim(), "KhachHang", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Không chuyển tài khoản quản trị thành khách hàng." });
        }

        account.MaVaiTro = role.MaVaiTro;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            maUser = FixedLengthHelper.TrimSafe(account.MaUser),
            tenVaiTro = role.TenVaiTro.Trim()
        });
    }

    private async Task<ActionResult> CreateStaffAsync(AdminCreateAccountDto request)
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

        var roleName = string.IsNullOrWhiteSpace(request.TenVaiTro) ? "Sale" : request.TenVaiTro.Trim();
        if (!string.Equals(roleName, "Sale", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Chỉ tạo được tài khoản Sale hoặc Admin." });
        }

        var role = await FindRoleAsync(roleName);
        if (role is null)
        {
            return BadRequest(new
            {
                message = $"Chưa cấu hình vai trò {roleName} trong hệ thống."
            });
        }

        var maUserDb = await GenerateUserIdAsync();
        var account = new NguoiSuDung
        {
            MaUser = maUserDb,
            SoDienThoai = phoneDb,
            MatKhau = BCrypt.Net.BCrypt.HashPassword(password),
            MaVaiTro = role.MaVaiTro
        };

        _context.NguoiSuDungs.Add(account);
        await ReplaceGrantsAsync(maUserDb, request.Quyen);
        await _context.SaveChangesAsync();

        var rows = await _context.QuyenNhanViens
            .AsNoTracking()
            .Where(item => item.MaUser == maUserDb)
            .ToListAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            maUser = FixedLengthHelper.TrimSafe(account.MaUser),
            soDienThoai = FixedLengthHelper.TrimSafe(account.SoDienThoai),
            tenVaiTro = role.TenVaiTro.Trim(),
            quyen = MapGrants(role.TenVaiTro.Trim(), rows)
        });
    }

    private async Task ReplaceGrantsAsync(string maUserDb, IEnumerable<QuyenChucNangDto>? incoming)
    {
        var byModule = (incoming ?? [])
            .Where(item => PermissionCatalog.IsKnown(item.ChucNang))
            .GroupBy(item => item.ChucNang.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

        var existing = await _context.QuyenNhanViens
            .Where(item => item.MaUser == maUserDb)
            .ToListAsync();
        _context.QuyenNhanViens.RemoveRange(existing);

        foreach (var module in PermissionCatalog.Modules)
        {
            byModule.TryGetValue(module.Ma, out var dto);
            var toanQuyen = dto?.ToanQuyen == true;
            _context.QuyenNhanViens.Add(new QuyenNhanVien
            {
                MaQuyen = await GenerateGrantIdAsync(),
                MaUser = maUserDb,
                ChucNang = module.Ma,
                Them = toanQuyen || dto?.Them == true,
                Sua = toanQuyen || dto?.Sua == true,
                Xoa = toanQuyen || dto?.Xoa == true,
                ToanQuyen = toanQuyen
            });
        }
    }

    private string? GuardSelfTaiKhoan(string targetUserDb, IEnumerable<QuyenChucNangDto>? incoming)
    {
        var currentUser = User.FindFirst("MaUser")?.Value;
        if (currentUser is null || FixedLengthHelper.PadTo20(currentUser) != targetUserDb)
            return null;

        var taiKhoan = incoming?.FirstOrDefault(item =>
            string.Equals(item.ChucNang, PermissionCatalog.TaiKhoan, StringComparison.OrdinalIgnoreCase));
        var keepsAccess = taiKhoan is not null &&
            (taiKhoan.ToanQuyen || taiKhoan.Them || taiKhoan.Sua || taiKhoan.Xoa);
        return keepsAccess
            ? null
            : "Không được gỡ quyền quản lý tài khoản của chính mình.";
    }

    private IReadOnlyList<PermissionGrant> MapGrants(string role, IEnumerable<QuyenNhanVien> rows)
    {
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            return PermissionService.FullGrants();

        var stored = rows.ToList();
        if (stored.Count == 0 && string.Equals(role, "Sale", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionCatalog.Modules.Select(module => new PermissionGrant
            {
                ChucNang = module.Ma,
                TenChucNang = module.Ten,
                Them = module.Ma != PermissionCatalog.TaiKhoan,
                Sua = module.Ma != PermissionCatalog.TaiKhoan,
                Xoa = module.Ma != PermissionCatalog.TaiKhoan,
                ToanQuyen = module.Ma != PermissionCatalog.TaiKhoan
            }).ToList();
        }

        return PermissionService.MergeStored(stored);
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

    private async Task<string> GenerateGrantIdAsync()
    {
        string id;
        do
        {
            id = FixedLengthHelper.PadTo20(Guid.NewGuid().ToString("N")[..20].ToUpperInvariant());
        }
        while (await _context.QuyenNhanViens.AnyAsync(item => item.MaQuyen == id));
        return id;
    }
}
