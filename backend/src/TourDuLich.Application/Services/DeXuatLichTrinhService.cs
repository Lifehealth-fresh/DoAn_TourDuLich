using Microsoft.EntityFrameworkCore;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.Application.Services;

public sealed class DeXuatLichTrinhService : IDeXuatLichTrinhService
{
    private readonly AppDbContext _context;

    public DeXuatLichTrinhService(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<LichTrinhDeXuat>> GenerateAsync(
        YeuCauThietKe request, CancellationToken cancellationToken = default)
    {
        var destination = request.DiemDenMongMuon?.Trim() ?? string.Empty;
        var region = await ResolveRegionAsync(destination, cancellationToken);
        if (region is null)
            return [];

        var points = await _context.DiemThamQuans.AsNoTracking()
            .Where(item => item.MaKhuVuc == region.MaKhuVuc)
            .OrderBy(item => item.MaDthamQuan)
            .ToListAsync(cancellationToken);
        if (points.Count == 0)
            return [];

        var active = FixedLengthHelper.PadTo20("HoatDong");
        var luuTru = FixedLengthHelper.PadTo20(HotelStayRules.LoaiLuuTru);
        var hotels = await _context.SanPhamDoiTacs
            .Include(item => item.MaDoiTacNavigation)
            .Where(item =>
                item.MaDoiTacNavigation.LoaiDoiTac == luuTru &&
                item.MaDoiTacNavigation.MaKhuVuc == region.MaKhuVuc &&
                (item.TrangThai == null || item.TrangThai == active) &&
                (item.MaDoiTacNavigation.TrangThai == null || item.MaDoiTacNavigation.TrangThai == active))
            .OrderBy(item => item.GiaNiemYet)
            .ToListAsync(cancellationToken);
        if (hotels.Count == 0)
            return [];

        var days = Math.Clamp(request.SoNgay ?? 1, 1, 30);
        var plans = new List<LichTrinhDeXuat>();

        for (var planNumber = 1; planNumber <= 3; planNumber++)
        {
            var plan = new LichTrinhDeXuat
            {
                MaDeXuat = await GeneratePlanIdAsync(cancellationToken),
                MaYeuCau = request.MaYeuCau,
                ThuTuPhuongAn = planNumber,
                TenPhuongAn = planNumber switch
                {
                    1 => "Phương án tiết kiệm",
                    2 => "Phương án cân bằng",
                    _ => "Phương án cao cấp"
                },
                GhiChu = $"Đề xuất cùng khu vực {region.TenKhuVuc}. Mỗi ngày kết thúc bằng khách sạn (giá 1 đêm).",
                TrangThai = FixedLengthHelper.PadTo20("DeXuat"),
                NgayTao = DateTime.UtcNow
            };

            var hotel = SelectHotel(hotels, planNumber);
            var selectedPoints = SelectPointsForPlan(points, days, planNumber);
            for (var day = 1; day <= days; day++)
            {
                var dayPoints = selectedPoints.Where(item => item.NgayThu == day).ToList();
                var order = 1;
                foreach (var selection in dayPoints)
                {
                    var point = selection.Point;
                    plan.ChiTiets.Add(new LichTrinhDeXuatChiTiet
                    {
                        MaChiTiet = await GenerateDetailIdAsync(cancellationToken),
                        MaDeXuat = plan.MaDeXuat,
                        NgayThu = day,
                        ThuTuTrongNgay = order++,
                        MaDthamQuan = point.MaDthamQuan,
                        MaSanPham = null,
                        SoLuong = 1,
                        DonGia = 0,
                        ThanhTien = 0,
                        Mota = point.TenDiaDanh
                    });
                }

                plan.ChiTiets.Add(new LichTrinhDeXuatChiTiet
                {
                    MaChiTiet = await GenerateDetailIdAsync(cancellationToken),
                    MaDeXuat = plan.MaDeXuat,
                    NgayThu = day,
                    ThuTuTrongNgay = order,
                    MaDthamQuan = null,
                    MaSanPham = hotel.MaSanPham,
                    SoLuong = 1,
                    DonGia = hotel.GiaNiemYet,
                    ThanhTien = TuThietKeTourPricing.CalculateLine(hotel.GiaNiemYet, 1),
                    Mota = HotelStayRules.HotelCaption(hotel)
                });
            }

            plan.TongTienDuKien = TuThietKeTourPricing.CalculateTotal(plan.ChiTiets.Select(item => item.ThanhTien));
            plans.Add(plan);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var oldPlans = await _context.LichTrinhDeXuats
            .Where(item => item.MaYeuCau == request.MaYeuCau)
            .ToListAsync(cancellationToken);
        _context.LichTrinhDeXuats.RemoveRange(oldPlans);
        _context.LichTrinhDeXuats.AddRange(plans);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return plans;
    }

    private async Task<KhuVuc?> ResolveRegionAsync(string destination, CancellationToken cancellationToken)
    {
        if (destination.Length > 0)
        {
            var named = await _context.KhuVucs.AsNoTracking()
                .Where(item => item.TenKhuVuc != null && item.TenKhuVuc.Contains(destination))
                .OrderBy(item => item.MaKhuVuc)
                .FirstOrDefaultAsync(cancellationToken);
            if (named is not null)
                return named;

            var fromPoint = await _context.DiemThamQuans.AsNoTracking()
                .Include(item => item.MaKhuVucNavigation)
                .Where(item =>
                    (item.TenDiaDanh != null && item.TenDiaDanh.Contains(destination)) ||
                    (item.DiaChi != null && item.DiaChi.Contains(destination)))
                .Select(item => item.MaKhuVucNavigation)
                .FirstOrDefaultAsync(cancellationToken);
            if (fromPoint is not null)
                return fromPoint;
        }

        return await _context.KhuVucs.AsNoTracking()
            .Where(item => _context.DiemThamQuans.Any(point => point.MaKhuVuc == item.MaKhuVuc) &&
                           _context.DoiTacs.Any(partner =>
                               partner.MaKhuVuc == item.MaKhuVuc &&
                               partner.LoaiDoiTac == FixedLengthHelper.PadTo20(HotelStayRules.LoaiLuuTru)))
            .OrderBy(item => item.MaKhuVuc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static List<PointSelection> SelectPointsForPlan(
        IReadOnlyList<DiemThamQuan> points, int days, int planNumber)
    {
        if (points.Count == 0)
            return [];

        var perDay = planNumber == 3 && points.Count >= days * 2 ? 2 : 1;
        var selected = new List<PointSelection>();
        for (var day = 1; day <= days; day++)
        {
            for (var slot = 1; slot <= perDay; slot++)
            {
                var index = ((day - 1) * perDay + (slot - 1) + (planNumber - 1)) % points.Count;
                selected.Add(new PointSelection(points[index], day, slot));
            }
        }
        return selected;
    }

    private static SanPhamDoiTac SelectHotel(IReadOnlyList<SanPhamDoiTac> hotels, int planNumber)
    {
        var index = planNumber switch
        {
            1 => 0,
            2 => hotels.Count / 2,
            _ => hotels.Count - 1
        };
        return hotels[index];
    }

    private sealed record PointSelection(DiemThamQuan Point, int NgayThu, int ThuTuTrongNgay);

    private async Task<string> GeneratePlanIdAsync(CancellationToken cancellationToken)
        => await GenerateIdAsync("DX", id => _context.LichTrinhDeXuats.AnyAsync(item => item.MaDeXuat == id, cancellationToken));

    private async Task<string> GenerateDetailIdAsync(CancellationToken cancellationToken)
        => await GenerateIdAsync("DC", id => _context.LichTrinhDeXuatChiTiets.AnyAsync(item => item.MaChiTiet == id, cancellationToken));

    private static async Task<string> GenerateIdAsync(string prefix, Func<string, Task<bool>> exists)
    {
        string id;
        do
        {
            id = FixedLengthHelper.PadTo20($"{prefix}{Guid.NewGuid():N}"[..20].ToUpperInvariant());
        } while (await exists(id));
        return id;
    }
}
