using Microsoft.EntityFrameworkCore;
using TourDuLich.Application.Helpers;
using TourDuLich.Application.Services;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.API.Services;

/// <summary>Refreshes persisted recommendations within the seven-minute SLA.</summary>
public sealed class AiRecommendationRefreshWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiRecommendationRefreshWorker> _logger;
    private readonly TimeSpan _interval;

    public AiRecommendationRefreshWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<AiRecommendationRefreshWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var seconds = configuration.GetValue<int?>("AiService:RefreshIntervalSeconds") ?? 300;
        _interval = TimeSpan.FromSeconds(Math.Clamp(seconds, 1, 420));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            await RefreshAllAsync(stoppingToken);
            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task RefreshAllAsync(CancellationToken cancellationToken)
    {
        JobRunLog? jobRun = null;
        AppDbContext? context = null;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var client = scope.ServiceProvider.GetRequiredService<IAiRecommendationClient>();
            jobRun = new JobRunLog
            {
                TenJob = "AiRecommendationRefresh",
                ThoiDiemBatDau = DateTime.UtcNow,
                TrangThai = "DangChay"
            };
            context.JobRunLogs.Add(jobRun);
            await context.SaveChangesAsync(cancellationToken);

            var userIds = await context.NguoiSuDungs.AsNoTracking()
                .Select(item => item.MaUser)
                .ToListAsync(cancellationToken);
            var refreshedCount = 0;
            var errors = new List<string>();

            foreach (var userId in userIds)
            {
                try
                {
                    var recommendations = await client.GetRecommendationsAsync(
                        FixedLengthHelper.TrimSafe(userId) ?? userId.Trim(), 5, 0.5, cancellationToken);
                    await ReplaceUserRecommendationsAsync(context, userId, recommendations, cancellationToken);
                    refreshedCount += recommendations.Count;
                }
                catch (Exception error) when (error is not OperationCanceledException)
                {
                    _logger.LogError(error, "AI refresh failed for user {MaUser}.", userId);
                    errors.Add($"{userId.Trim()}: {error.Message}");
                }
            }

            jobRun.ThoiDiemKetThuc = DateTime.UtcNow;
            jobRun.SoBanGhi = refreshedCount;
            jobRun.TrangThai = errors.Count == 0 ? "ThanhCong" : "LoiMotPhan";
            jobRun.Loi = errors.Count == 0 ? null : string.Join(" | ", errors)[..Math.Min(2000, string.Join(" | ", errors).Length)];
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            _logger.LogError(error, "Scheduled AI recommendation refresh failed.");
            if (context is not null && jobRun is not null)
            {
                try
                {
                    jobRun.ThoiDiemKetThuc = DateTime.UtcNow;
                    jobRun.TrangThai = "ThatBai";
                    jobRun.Loi = error.Message[..Math.Min(2000, error.Message.Length)];
                    await context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception loggingError)
                {
                    _logger.LogError(loggingError, "Could not persist the AI job failure log.");
                }
            }
        }
    }

    private static async Task ReplaceUserRecommendationsAsync(
        AppDbContext context,
        string maUser,
        IReadOnlyList<AiRecommendationResult> recommendations,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var previous = await context.AigoiYs.Where(item => item.MaUser == maUser)
            .ToListAsync(cancellationToken);
        context.AigoiYs.RemoveRange(previous);

        foreach (var recommendation in recommendations
            .Where(item => !string.IsNullOrWhiteSpace(item.MaTour))
            .GroupBy(item => FixedLengthHelper.PadTo20(item.MaTour), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item => item.DiemPhuHop).First()))
        {
            var tourId = FixedLengthHelper.PadTo20(recommendation.MaTour);
            if (!await context.Tours.AnyAsync(item => item.MaTour == tourId, cancellationToken))
                continue;
            context.AigoiYs.Add(new AigoiY
            {
                MaRecommodation = FixedLengthHelper.PadTo20($"AI{Guid.NewGuid():N}"[..20].ToUpperInvariant()),
                MaUser = maUser,
                MaTour = tourId,
                DiemPhuHop = Math.Clamp(recommendation.DiemPhuHop, 0, 1),
                LyDo = recommendation.LyDo?.Trim(),
                NgayGoiY = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
