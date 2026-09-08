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
        var directPoints = await _context.DiemThamQuans
            .Include(item => item.MaKhuVucNavigation)
            .Where(item => destination == string.Empty ||
                (item.TenDiaDanh != null && item.TenDiaDanh.Contains(destination)) ||
                (item.DiaChi != null && item.DiaChi.Contains(destination)) ||
                (item.MaKhuVucNavigation != null && item.MaKhuVucNavigation.TenKhuVuc != null &&
                 item.MaKhuVucNavigation.TenKhuVuc.Contains(destination)))
            .OrderBy(item => item.MaDthamQuan)
            .ToListAsync(cancellationToken);

        var matchedRegionIds = directPoints
            .Where(item => item.MaKhuVuc != null)
            .Select(item => item.MaKhuVuc!)
            .Distinct()
            .ToList();
        var directPointIds = directPoints.Select(item => item.MaDthamQuan).ToList();

        var matchedPoints = await _context.DiemThamQuans
            .Where(item => directPointIds.Contains(item.MaDthamQuan) ||
                           (item.MaKhuVuc != null && matchedRegionIds.Contains(item.MaKhuVuc)))
            .OrderBy(item => item.MaDthamQuan)
            .ToListAsync(cancellationToken);

        if (matchedPoints.Count == 0)
        {
            matchedPoints = await _context.DiemThamQuans
                .OrderBy(item => item.MaDthamQuan)
                .ToListAsync(cancellationToken);
        }

        if (matchedPoints.Count == 0)
            return [];

        var products = await _context.SanPhamDoiTacs
            .Include(item => item.MaDoiTacNavigation)
            .Where(item => item.TrangThai == null || item.TrangThai == FixedLengthHelper.PadTo20("HoatDong"))
            .OrderBy(item => item.GiaNiemYet)
            .ToListAsync(cancellationToken);

        var days = Math.Clamp(request.SoNgay ?? 1, 1, 30);
        var budget = request.NganSachDuKien;
        var plans = new List<LichTrinhDeXuat>();

        // Heuristic tạm thời: chọn điểm/sản phẩm theo mức giá, không phải ML và độc lập với ai-service.
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
                GhiChu = matchedPoints.Count < days * 3
                    ? "Số điểm tham quan phù hợp với khu vực này còn hạn chế, một số điểm có thể lặp lại giữa các phương án."
                    : "Đề xuất heuristic theo điểm tham quan, sản phẩm và ngân sách; chưa sử dụng ML.",
                TrangThai = FixedLengthHelper.PadTo20("DeXuat"),
                NgayTao = DateTime.UtcNow
            };

            var selectedPoints = SelectPointsForPlan(matchedPoints, days, planNumber);
            for (var index = 0; index < selectedPoints.Count; index++)
            {
                var selection = selectedPoints[index];
                var point = selection.Point;
                var pointProducts = products
                    .Where(item => item.MaDthamQuan == point.MaDthamQuan ||
                        (item.MaDoiTacNavigation.MaKhuVuc != null &&
                         item.MaDoiTacNavigation.MaKhuVuc == point.MaKhuVuc))
                    .ToList();
                var product = SelectProduct(pointProducts, planNumber);
                plan.ChiTiets.Add(new LichTrinhDeXuatChiTiet
                {
                    MaChiTiet = await GenerateDetailIdAsync(cancellationToken),
                    MaDeXuat = plan.MaDeXuat,
                    NgayThu = selection.NgayThu,
                    ThuTuTrongNgay = selection.ThuTuTrongNgay,
                    MaDthamQuan = point.MaDthamQuan,
                    MaSanPham = product?.MaSanPham,
                    SoLuong = 1,
                    DonGia = product?.GiaNiemYet ?? 0,
                    ThanhTien = TuThietKeTourPricing.CalculateLine(product?.GiaNiemYet ?? 0, 1),
                    Mota = point.TenDiaDanh
                });
            }

            if (budget.HasValue && TuThietKeTourPricing.CalculateTotal(plan.ChiTiets.Select(item => item.ThanhTien)) > budget.Value)
            {
                foreach (var detail in plan.ChiTiets.OrderByDescending(item => item.ThanhTien))
                {
                    detail.MaSanPham = null;
                    detail.DonGia = 0;
                    detail.ThanhTien = 0;
                    if (TuThietKeTourPricing.CalculateTotal(plan.ChiTiets.Select(item => item.ThanhTien)) <= budget.Value)
                        break;
                }
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

    private static List<PointSelection> SelectPointsForPlan(
        IReadOnlyList<DiemThamQuan> points, int days, int planNumber)
    {
        if (points.Count == 0)
            return [];

        var targetCount = planNumber == 3 && points.Count >= days * 2 ? days * 2 : days;
        var ownGroup = points.Where((_, index) => index % 3 == planNumber - 1).ToList();
        var selected = ownGroup.Take(targetCount).ToList();

        // Bổ sung điểm chưa dùng trước khi cho phép lặp lại trong cùng phương án.
        foreach (var point in points)
        {
            if (selected.Count >= targetCount)
                break;
            if (!selected.Contains(point))
                selected.Add(point);
        }

        for (var index = 0; selected.Count < targetCount; index++)
            selected.Add(points[index % points.Count]);

        var dense = planNumber == 3 && targetCount == days * 2;
        return selected.Select((point, index) => new PointSelection(
            point,
            dense ? index / 2 + 1 : index + 1,
            dense ? index % 2 + 1 : 1)).ToList();
    }

    private static SanPhamDoiTac? SelectProduct(IReadOnlyList<SanPhamDoiTac> products, int planNumber)
    {
        if (products.Count == 0)
            return null;
        var index = planNumber switch
        {
            1 => 0,
            2 => products.Count / 2,
            _ => products.Count - 1
        };
        return products[index];
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
