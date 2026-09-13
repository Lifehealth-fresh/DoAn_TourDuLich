using System.Security.Claims;

namespace TourDuLich.API.Authorization;

public interface IPermissionService
{
    Task<bool> CanAsync(ClaimsPrincipal user, string chucNang, string hanhDong, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionGrant>> GetEffectiveGrantsAsync(
        string maUser,
        string? tenVaiTro,
        CancellationToken cancellationToken = default);
}

public sealed class PermissionGrant
{
    public string ChucNang { get; init; } = null!;
    public string TenChucNang { get; init; } = null!;
    public bool Them { get; init; }
    public bool Sua { get; init; }
    public bool Xoa { get; init; }
    public bool ToanQuyen { get; init; }
}
