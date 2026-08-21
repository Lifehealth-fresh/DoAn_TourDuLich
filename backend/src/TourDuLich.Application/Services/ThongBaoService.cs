using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace TourDuLich.Application.Services;

public class ThongBaoService
{
    public async Task TaoThongBaoAsync(
        AppDbContext context,
        string maUser,
        string tieuDe,
        string noiDung)
    {
        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maThongBaoDb = await GenerateMaThongBaoAsync(context);

        var thongBao = new ThongBao
        {
            MaThongBao = maThongBaoDb,
            MaUser = maUserDb,
            TieuDe = tieuDe.Trim(),
            NoiDung = noiDung.Trim(),
            DaDoc = false,
            NgayGui = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        context.ThongBaos.Add(thongBao);
    }

    private static async Task<string> GenerateMaThongBaoAsync(AppDbContext context)
    {
        string maThongBaoDb;

        do
        {
            var maThongBao = $"TB{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            maThongBaoDb = FixedLengthHelper.PadTo20(maThongBao);
        }
        while (await context.ThongBaos
            .AnyAsync(item => item.MaThongBao == maThongBaoDb));

        return maThongBaoDb;
    }
}
