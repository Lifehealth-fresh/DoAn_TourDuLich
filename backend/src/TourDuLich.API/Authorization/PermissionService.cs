using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.API.Authorization;

public sealed class PermissionService : IPermissionService
{
    private readonly AppDbContext _context;

    public PermissionService(AppDbContext context) => _context = context;

    public async Task<bool> CanAsync(
        ClaimsPrincipal user,
        string chucNang,
        string hanhDong,
        CancellationToken cancellationToken = default)
    {
        var role = user.FindFirst(ClaimTypes.Role)?.Value?.Trim();
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            return true;

        var maUser = user.FindFirst("MaUser")?.Value;
        if (string.IsNullOrWhiteSpace(maUser))
            return false;

        var grants = await GetEffectiveGrantsAsync(maUser, role, cancellationToken);
        var row = grants.FirstOrDefault(item =>
            string.Equals(item.ChucNang, chucNang, StringComparison.OrdinalIgnoreCase));
        if (row is null)
            return false;
        if (row.ToanQuyen)
            return true;

        return hanhDong switch
        {
            PermissionCatalog.Them => row.Them,
            PermissionCatalog.Sua => row.Sua,
            PermissionCatalog.Xoa => row.Xoa,
            PermissionCatalog.Xem => row.Them || row.Sua || row.Xoa,
            _ => false
        };
    }

    public async Task<IReadOnlyList<PermissionGrant>> GetEffectiveGrantsAsync(
        string maUser,
        string? tenVaiTro,
        CancellationToken cancellationToken = default)
    {
        var role = tenVaiTro?.Trim();
        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            return FullGrants();

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var rows = await _context.QuyenNhanViens
            .AsNoTracking()
            .Where(item => item.MaUser == maUserDb)
            .ToListAsync(cancellationToken);

        var legacySale = rows.Count == 0
            && string.Equals(role, "Sale", StringComparison.OrdinalIgnoreCase);

        return PermissionCatalog.Modules.Select(module =>
        {
            if (legacySale && module.Ma != PermissionCatalog.TaiKhoan)
                return Grant(module, them: true, sua: true, xoa: true, toanQuyen: true);

            var row = rows.FirstOrDefault(item =>
                string.Equals(item.ChucNang.Trim(), module.Ma, StringComparison.OrdinalIgnoreCase));
            if (row is null)
                return Grant(module, false, false, false, false);
            if (row.ToanQuyen)
                return Grant(module, true, true, true, true);
            return Grant(module, row.Them, row.Sua, row.Xoa, false);
        }).ToList();
    }

    public static IReadOnlyList<PermissionGrant> FullGrants() =>
        PermissionCatalog.Modules.Select(module => Grant(module, true, true, true, true)).ToList();

    public static IReadOnlyList<PermissionGrant> MergeStored(IEnumerable<QuyenNhanVien> rows)
    {
        var stored = rows.ToList();
        return PermissionCatalog.Modules.Select(module =>
        {
            var row = stored.FirstOrDefault(item =>
                string.Equals(item.ChucNang.Trim(), module.Ma, StringComparison.OrdinalIgnoreCase));
            if (row is null)
                return Grant(module, false, false, false, false);
            if (row.ToanQuyen)
                return Grant(module, true, true, true, true);
            return Grant(module, row.Them, row.Sua, row.Xoa, false);
        }).ToList();
    }

    private static PermissionGrant Grant(
        PermissionModule module, bool them, bool sua, bool xoa, bool toanQuyen) => new()
    {
        ChucNang = module.Ma,
        TenChucNang = module.Ten,
        Them = them || toanQuyen,
        Sua = sua || toanQuyen,
        Xoa = xoa || toanQuyen,
        ToanQuyen = toanQuyen
    };
}
