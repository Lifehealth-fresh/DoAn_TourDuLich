using Microsoft.EntityFrameworkCore;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.Application.Services;

public sealed class DestinationMatch
{
    public TinhThanh? Province { get; init; }
    public KhuVuc? Region { get; init; }
    public string Label => Province?.TenTinh ?? Region?.TenKhuVuc ?? string.Empty;
}

public interface IDestinationResolver
{
    Task<DestinationMatch?> ResolveAsync(string? text, string? maTinh, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TinhThanh>> SearchAsync(string? query, CancellationToken cancellationToken = default);
}

public sealed class DestinationResolver : IDestinationResolver
{
    private readonly AppDbContext _context;
    public DestinationResolver(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<TinhThanh>> SearchAsync(string? query, CancellationToken cancellationToken = default)
    {
        var rows = await LoadProvincesAsync(cancellationToken);
        var term = VietnameseText.Fold(query);
        if (term.Length == 0)
            return rows.OrderBy(item => item.TenTinh).ToList();
        return rows
            .Select(item => (item, score: Score(item, term)))
            .Where(pair => pair.score > 0)
            .OrderByDescending(pair => pair.score)
            .ThenBy(pair => pair.item.TenTinh.Length)
            .ThenBy(pair => pair.item.TenTinh)
            .Select(pair => pair.item)
            .ToList();
    }

    public async Task<DestinationMatch?> ResolveAsync(string? text, string? maTinh, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(maTinh))
        {
            var id = FixedLengthHelper.PadTo20(maTinh);
            var byId = await _context.TinhThanhs.AsNoTracking()
                .Include(item => item.MaKhuVucNavigation)
                .FirstOrDefaultAsync(item => item.MaTinh == id, cancellationToken);
            if (byId is not null)
                return new DestinationMatch { Province = byId, Region = byId.MaKhuVucNavigation };
        }

        var destination = (text ?? string.Empty).Trim();
        if (destination.Length == 0)
            return null;

        var rows = await LoadProvincesAsync(cancellationToken);
        var term = VietnameseText.Fold(destination);
        var ranked = rows
            .Select(item => (item, score: Score(item, term)))
            .Where(pair => pair.score > 0)
            .OrderByDescending(pair => pair.score)
            .ThenBy(pair => pair.item.TenTinh.Length)
            .ToList();
        if (ranked.Count > 0)
        {
            var best = ranked[0].item;
            return new DestinationMatch { Province = best, Region = best.MaKhuVucNavigation };
        }

        var fromPoint = await _context.DiemThamQuans.AsNoTracking()
            .Where(item => item.MaTinh != null)
            .Select(item => new { item.MaTinh, item.TenDiaDanh, item.DiaChi })
            .ToListAsync(cancellationToken);
        var sight = fromPoint
            .Where(item => VietnameseText.ContainsFold(item.TenDiaDanh, destination) ||
                           VietnameseText.ContainsFold(item.DiaChi, destination) ||
                           VietnameseText.ContainsFold(destination, item.TenDiaDanh))
            .Select(item => item.MaTinh)
            .FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(sight))
        {
            var fromSight = rows.FirstOrDefault(item => item.MaTinh == sight)
                ?? await _context.TinhThanhs.AsNoTracking()
                    .Include(item => item.MaKhuVucNavigation)
                    .FirstOrDefaultAsync(item => item.MaTinh == sight, cancellationToken);
            if (fromSight is not null)
                return new DestinationMatch { Province = fromSight, Region = fromSight.MaKhuVucNavigation };
        }

        return null;
    }

    private async Task<List<TinhThanh>> LoadProvincesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _context.TinhThanhs.AsNoTracking()
                .Include(item => item.MaKhuVucNavigation)
                .Include(item => item.Aliases)
                .ToListAsync(cancellationToken);
        }
        catch (Exception)
        {
            return await _context.TinhThanhs.AsNoTracking()
                .Include(item => item.MaKhuVucNavigation)
                .ToListAsync(cancellationToken);
        }
    }

    private static int Score(TinhThanh province, string term)
    {
        if (term.Length == 0)
            return 0;
        var name = VietnameseText.Fold(province.TenTinh);
        if (name == term)
            return 400;
        var aliases = province.Aliases?
            .Select(item => VietnameseText.Fold(item.TenAlias))
            .Where(item => item.Length > 0)
            .ToList() ?? [];
        if (aliases.Any(item => item == term))
            return 350;
        if (name.StartsWith(term, StringComparison.Ordinal) || aliases.Any(item => item.StartsWith(term, StringComparison.Ordinal)))
            return 220;
        if (name.Contains(term, StringComparison.Ordinal) || aliases.Any(item => item.Contains(term, StringComparison.Ordinal)))
            return 160;
        if (term.Contains(name, StringComparison.Ordinal) && name.Length >= 4)
            return 120;
        var longAlias = aliases.FirstOrDefault(item => term.Contains(item) && item.Length >= 4);
        return longAlias is null ? 0 : 110;
    }
}
