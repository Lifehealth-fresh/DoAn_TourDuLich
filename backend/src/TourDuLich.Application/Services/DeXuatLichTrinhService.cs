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
        var province = await ResolveProvinceAsync(destination, cancellationToken);
        var region = province is null ? await ResolveRegionAsync(destination, cancellationToken) : null;
        var provinceId = province?.MaTinh;
        var regionId = province?.MaKhuVuc ?? region?.MaKhuVuc;
        if (provinceId is null && regionId is null)
            return [];

        var active = FixedLengthHelper.PadTo20("HoatDong");
        var luuTru = FixedLengthHelper.PadTo20(HotelStayRules.LoaiLuuTru);
        var anUong = FixedLengthHelper.PadTo20(HotelStayRules.LoaiAnUong);

        var points = await _context.DiemThamQuans.AsNoTracking()
            .Where(item => provinceId != null ? item.MaTinh == provinceId : item.MaKhuVuc == regionId)
            .OrderBy(item => item.MaDthamQuan)
            .ToListAsync(cancellationToken);
        var visits = points.Where(item => (item.MaDthamQuan ?? string.Empty).StartsWith("DTV", StringComparison.OrdinalIgnoreCase)).ToList();
        if (visits.Count == 0)
            visits = points;
        var plays = points.Where(item => (item.MaDthamQuan ?? "").StartsWith("DTP") ||
                                         (item.Mota ?? "").Contains("vui chơi", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var products = await _context.SanPhamDoiTacs.AsNoTracking()
            .Include(item => item.MaDoiTacNavigation)
            .Where(item =>
                (item.TrangThai == null || item.TrangThai == active) &&
                (item.MaDoiTacNavigation.TrangThai == null || item.MaDoiTacNavigation.TrangThai == active) &&
                (provinceId != null
                    ? item.MaDoiTacNavigation.MaTinh == provinceId
                    : item.MaDoiTacNavigation.MaKhuVuc == regionId))
            .ToListAsync(cancellationToken);

        var hotels = products.Where(HotelStayRules.IsHotelProduct).OrderBy(item => item.GiaNiemYet).ToList();
        var meals = products.Where(item => HotelStayRules.IsDining(item.MaDoiTacNavigation?.LoaiDoiTac))
            .OrderBy(item => item.GiaNiemYet).ToList();
        var tickets = products.Where(item => HotelStayRules.IsActivity(item.MaDoiTacNavigation?.LoaiDoiTac)).ToList();
        if (hotels.Count == 0 || visits.Count == 0)
            return [];

        var days = Math.Clamp(request.SoNgay ?? 1, 1, 30);
        var nights = Math.Max(1, days);
        var placeName = province?.TenTinh ?? region?.TenKhuVuc ?? "điểm đến";
        var plans = new List<LichTrinhDeXuat>();

        for (var planNumber = 1; planNumber <= 3; planNumber++)
        {
            var hotel = SelectByPlan(hotels, planNumber);
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
                GhiChu = $"Lịch {placeName}. Cả tour một khách sạn. Mỗi ngày có tham quan, ăn uống và giờ giấc.",
                TrangThai = FixedLengthHelper.PadTo20("DeXuat"),
                NgayTao = DateTime.UtcNow
            };

            for (var day = 1; day <= days; day++)
            {
                var visit = visits[(day - 1 + planNumber - 1) % visits.Count];
                var play = plays.Count == 0 ? null : plays[(day + planNumber) % plays.Count];
                var meal = meals.Count == 0 ? null : meals[(day - 1 + planNumber - 1) % meals.Count];
                var ticket = tickets.FirstOrDefault(item => item.MaDthamQuan == visit.MaDthamQuan);
                var playTicket = play is null ? null : tickets.FirstOrDefault(item => item.MaDthamQuan == play.MaDthamQuan);
                var order = 1;
                if (day == 1)
                {
                    await AddLine(plan, day, order++, new TimeSpan(7, 0, 0), hotel, 0, 1,
                        $"Có mặt tại {hotel.MaDoiTacNavigation.TenDoiTac}", cancellationToken);
                    await AddLine(plan, day, order++, new TimeSpan(9, 0, 0), hotel, hotel.GiaNiemYet, nights,
                        $"Check-in phòng {hotel.TenSanPham} — {nights} đêm", cancellationToken);
                    if (meal is not null)
                        await AddLine(plan, day, order++, new TimeSpan(11, 0, 0), meal, meal.GiaNiemYet, 1,
                            $"Dùng bữa tại {meal.MaDoiTacNavigation.TenDoiTac}", cancellationToken);
                    await AddVisit(plan, day, order++, new TimeSpan(15, 0, 0), visit, ticket, cancellationToken);
                }
                else
                {
                    await AddLine(plan, day, order++, new TimeSpan(7, 0, 0), hotel, 0, 1,
                        $"Xuất phát từ {hotel.MaDoiTacNavigation.TenDoiTac}", cancellationToken);
                    if (play is not null)
                        await AddVisit(plan, day, order++, new TimeSpan(9, 0, 0), play, playTicket, cancellationToken);
                    if (meal is not null)
                        await AddLine(plan, day, order++, new TimeSpan(11, 0, 0), meal, meal.GiaNiemYet, 1,
                            $"Dùng bữa tại {meal.MaDoiTacNavigation.TenDoiTac}", cancellationToken);
                    await AddVisit(plan, day, order, new TimeSpan(15, 0, 0), visit, ticket, cancellationToken);
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

    private async Task AddVisit(LichTrinhDeXuat plan, int day, int order, TimeSpan time,
        DiemThamQuan point, SanPhamDoiTac? ticket, CancellationToken cancellationToken)
    {
        var price = ticket?.GiaNiemYet ?? 0;
        var line = new LichTrinhDeXuatChiTiet
        {
            MaChiTiet = await GenerateDetailIdAsync(cancellationToken),
            MaDeXuat = plan.MaDeXuat,
            NgayThu = day,
            ThuTuTrongNgay = order,
            MaDthamQuan = point.MaDthamQuan,
            MaSanPham = ticket?.MaSanPham,
            SoLuong = 1,
            DonGia = price,
            ThanhTien = TuThietKeTourPricing.CalculateLine(price, 1),
            GioBatDau = time,
            Mota = HotelStayRules.FormatSlot(time, $"Tham quan tại {point.TenDiaDanh}")
        };
        plan.ChiTiets.Add(line);
    }

    private async Task AddLine(LichTrinhDeXuat plan, int day, int order, TimeSpan time,
        SanPhamDoiTac product, int donGia, int soLuong, string caption, CancellationToken cancellationToken)
    {
        plan.ChiTiets.Add(new LichTrinhDeXuatChiTiet
        {
            MaChiTiet = await GenerateDetailIdAsync(cancellationToken),
            MaDeXuat = plan.MaDeXuat,
            NgayThu = day,
            ThuTuTrongNgay = order,
            MaDthamQuan = product.MaDthamQuan,
            MaSanPham = product.MaSanPham,
            SoLuong = soLuong,
            DonGia = donGia,
            ThanhTien = TuThietKeTourPricing.CalculateLine(donGia, soLuong),
            GioBatDau = time,
            Mota = HotelStayRules.FormatSlot(time, caption)
        });
    }

    private async Task<TinhThanh?> ResolveProvinceAsync(string destination, CancellationToken cancellationToken)
    {
        if (destination.Length > 0)
        {
            var named = await _context.TinhThanhs.AsNoTracking()
                .Where(item => item.TenTinh.Contains(destination))
                .OrderBy(item => item.MaTinh)
                .FirstOrDefaultAsync(cancellationToken);
            if (named is not null)
                return named;

            var fromPoint = await _context.DiemThamQuans.AsNoTracking()
                .Where(item =>
                    (item.TenDiaDanh != null && item.TenDiaDanh.Contains(destination)) ||
                    (item.DiaChi != null && item.DiaChi.Contains(destination)))
                .Select(item => item.MaTinh)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(fromPoint))
                return await _context.TinhThanhs.AsNoTracking()
                    .FirstOrDefaultAsync(item => item.MaTinh == fromPoint, cancellationToken);
        }

        return await _context.TinhThanhs.AsNoTracking()
            .Where(item => _context.DoiTacs.Any(partner =>
                partner.MaTinh == item.MaTinh &&
                partner.LoaiDoiTac == FixedLengthHelper.PadTo20(HotelStayRules.LoaiLuuTru)))
            .OrderBy(item => item.MaTinh)
            .FirstOrDefaultAsync(cancellationToken);
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
        }

        return await _context.KhuVucs.AsNoTracking()
            .Where(item => _context.DiemThamQuans.Any(point => point.MaKhuVuc == item.MaKhuVuc) &&
                           _context.DoiTacs.Any(partner =>
                               partner.MaKhuVuc == item.MaKhuVuc &&
                               partner.LoaiDoiTac == FixedLengthHelper.PadTo20(HotelStayRules.LoaiLuuTru)))
            .OrderBy(item => item.MaKhuVuc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static T SelectByPlan<T>(IReadOnlyList<T> items, int planNumber)
    {
        var index = planNumber switch
        {
            1 => 0,
            2 => items.Count / 2,
            _ => items.Count - 1
        };
        return items[index];
    }

    private async Task<string> GeneratePlanIdAsync(CancellationToken cancellationToken)
        => await GenerateIdAsync("DX", id => _context.LichTrinhDeXuats.AnyAsync(item => item.MaDeXuat == id, cancellationToken));

    private async Task<string> GenerateDetailIdAsync(CancellationToken cancellationToken)
        => await GenerateIdAsync("DC", id => _context.LichTrinhDeXuatChiTiets.AnyAsync(item => item.MaChiTiet == id, cancellationToken));

    private static async Task<string> GenerateIdAsync(string prefix, Func<string, Task<bool>> exists)
    {
        string id;
        do { id = FixedLengthHelper.PadTo20($"{prefix}{Guid.NewGuid():N}"[..20].ToUpperInvariant()); }
        while (await exists(id));
        return id;
    }
}