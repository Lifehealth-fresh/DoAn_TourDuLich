using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.Application.Services;

public sealed class HanhViLogger : IHanhViLogger
{
    private readonly AppDbContext _context;
    private readonly ILogger<HanhViLogger> _logger;

    public HanhViLogger(AppDbContext context, ILogger<HanhViLogger> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(string maUserDb, string? maTourDb, string hanhDong)
    {
        try
        {
            var hanhVi = new HanhViKhachHang
            {
                MaHanhDong = await GenerateMaHanhDongAsync(_context),
                MaUser = FixedLengthHelper.PadTo20(maUserDb),
                MaTour = string.IsNullOrWhiteSpace(maTourDb)
                    ? null
                    : FixedLengthHelper.PadTo20(maTourDb),
                HanhDong = FixedLengthHelper.PadTo20(hanhDong),
                ThoiGian = DateTime.UtcNow
            };

            _context.HanhViKhachHangs.Add(hanhVi);
            await _context.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception,
                "Không thể ghi hành vi {HanhDong} của user {MaUser}.",
                hanhDong, maUserDb);
        }
    }

    public static async Task<string> GenerateMaHanhDongAsync(AppDbContext context)
    {
        string maHanhDongDb;

        do
        {
            var maHanhDong = $"HV{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            maHanhDongDb = FixedLengthHelper.PadTo20(maHanhDong);
        }
        while (await context.HanhViKhachHangs
            .AnyAsync(item => item.MaHanhDong == maHanhDongDb));

        return maHanhDongDb;
    }
}
