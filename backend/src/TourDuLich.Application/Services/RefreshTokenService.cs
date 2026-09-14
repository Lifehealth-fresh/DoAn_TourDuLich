using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.Application.Services;

public sealed class RefreshTokenService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public RefreshTokenService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public int RefreshTokenDays =>
        int.TryParse(_configuration["Jwt:RefreshTokenDays"], out var days) && days > 0
            ? days
            : 7;

    public async Task<string> IssueAsync(string maUser, CancellationToken cancellationToken = default)
    {
        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        string maRefresh;
        string maRefreshDb;
        do
        {
            maRefresh = $"RT{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            maRefreshDb = FixedLengthHelper.PadTo20(maRefresh);
        }
        while (await _context.RefreshTokens.AnyAsync(item => item.MaRefresh == maRefreshDb, cancellationToken));

        _context.RefreshTokens.Add(new RefreshToken
        {
            MaRefresh = maRefreshDb,
            MaUser = maUserDb,
            TokenHash = Hash(raw),
            HetHan = DateTime.UtcNow.AddDays(RefreshTokenDays),
            TaoLuc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
        return raw;
    }

    public async Task<RefreshToken?> FindActiveAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return null;
        var hash = Hash(rawToken.Trim());
        var now = DateTime.UtcNow;
        return await _context.RefreshTokens
            .Include(item => item.MaUserNavigation)
            .ThenInclude(user => user.MaVaiTroNavigation)
            .FirstOrDefaultAsync(item =>
                item.TokenHash == hash &&
                item.ThuHoiLuc == null &&
                item.HetHan > now, cancellationToken);
    }

    public async Task RevokeAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        if (token.ThuHoiLuc is not null)
            return;
        token.ThuHoiLuc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllForUserAsync(string maUser, CancellationToken cancellationToken = default)
    {
        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var now = DateTime.UtcNow;
        var active = await _context.RefreshTokens
            .Where(item => item.MaUser == maUserDb && item.ThuHoiLuc == null)
            .ToListAsync(cancellationToken);
        foreach (var token in active)
            token.ThuHoiLuc = now;
        if (active.Count > 0)
            await _context.SaveChangesAsync(cancellationToken);
    }

    public static string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
