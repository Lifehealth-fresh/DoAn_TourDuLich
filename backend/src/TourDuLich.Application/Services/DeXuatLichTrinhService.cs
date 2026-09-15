using Microsoft.EntityFrameworkCore;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.Application.Services;

public sealed class DeXuatLichTrinhService : IDeXuatLichTrinhService
{
    private readonly AppDbContext _context;
    private readonly IDestinationResolver _destinations;
    private readonly ITravelTimeService _travel;

    public DeXuatLichTrinhService(AppDbContext context, IDestinationResolver destinations, ITravelTimeService travel)
    {
        _context = context;
        _destinations = destinations;
        _travel = travel;
    }

    public async Task<IReadOnlyList<LichTrinhDeXuat>> GenerateAsync(
        YeuCauThietKe request, DesignPlannerContext? extras = null, CancellationToken cancellationToken = default)
    {
        extras ??= DesignPlannerContext.From(request);
        var match = await _destinations.ResolveAsync(request.DiemDenMongMuon, extras.MaTinhDen ?? request.MaTinhDen, cancellationToken);
        var provinceId = match?.Province?.MaTinh;
        var regionId = match?.Province?.MaKhuVuc ?? match?.Region?.MaKhuVuc;
        if (provinceId is null && regionId is null)
            return [];

        var active = FixedLengthHelper.PadTo20("HoatDong");
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
        if (request.NgayDuKienDi is { } start && extras?.NgayKetThuc is { } end && end >= start)
            days = Math.Clamp(end.DayNumber - start.DayNumber + 1, 1, 30);
        var nights = Math.Max(1, days);
        var placeName = match?.Label ?? "điểm đến";
        var guests = Math.Max(1, (request.SoNguoiLon ?? 0) + (request.SoTreEm ?? 0));
        var eventsWanted = Math.Clamp(extras?.SoSuKienMoiNgay ?? 3, 1, 6);
        TinhThanh? origin = null;
        if (!string.IsNullOrWhiteSpace(extras?.MaTinhXuatPhat))
            origin = await _context.TinhThanhs.AsNoTracking()
                .FirstOrDefaultAsync(item => item.MaTinh == FixedLengthHelper.PadTo20(extras.MaTinhXuatPhat), cancellationToken);
        var destProvince = match?.Province;
        IReadOnlyList<TravelLeg> outbound = [];
        IReadOnlyList<TravelLeg> inbound = [];
        if (origin is not null && destProvince is not null)
        {
            outbound = await _travel.EstimateAsync(origin, destProvince, cancellationToken);
            inbound = await _travel.EstimateAsync(destProvince, origin, cancellationToken);
        }
        var departTime = extras?.GioKhoiHanh ?? new TimeSpan(7, 0, 0);
        var returnBy = extras?.GioKetThuc ?? new TimeSpan(20, 0, 0);
        var goLeg = outbound.Count == 0 ? null : _travel.BestOutbound(outbound, departTime);
        var backLeg = inbound.Count == 0 ? null : _travel.BestOutbound(inbound, new TimeSpan(8, 0, 0));

        var budget = request.NganSachDuKien;
        var maxPlans = extras?.MaxPlans is > 0 and <= 3 ? extras.MaxPlans : 3;
        var plans = new List<LichTrinhDeXuat>();
        for (var planNumber = 1; planNumber <= maxPlans; planNumber++)
        {
            var hotelPlan = maxPlans == 1 ? 2 : planNumber;
            var hotel = SelectHotel(hotels, hotelPlan, budget, nights, guests);
            var ratio = maxPlans == 1 ? 1.0 : planNumber switch { 1 => 0.88, 2 => 1.0, _ => 1.12 };
            var plan = new LichTrinhDeXuat
            {
                MaDeXuat = await GeneratePlanIdAsync(cancellationToken),
                MaYeuCau = request.MaYeuCau,
                ThuTuPhuongAn = planNumber,
                TenPhuongAn = maxPlans == 1
                    ? "Phương án đề xuất"
                    : planNumber switch
                    {
                        1 => "Phương án tiết kiệm",
                        2 => "Phương án cân bằng",
                        _ => "Phương án cao cấp"
                    },
                GhiChu = BudgetNote(placeName, hotel, goLeg, budget, ratio),
                TrangThai = FixedLengthHelper.PadTo20("DeXuat"),
                NgayTao = DateTime.UtcNow
            };

            var smart = extras?.GioKhoiHanh is not null || origin is not null;
            for (var day = 1; day <= days; day++)
            {
                var visit = visits[(day - 1 + hotelPlan - 1) % visits.Count];
                var play = plays.Count == 0 ? null : plays[(day + hotelPlan) % plays.Count];
                var meal = meals.Count == 0 ? null : meals[(day - 1 + hotelPlan - 1) % meals.Count];
                var ticket = tickets.FirstOrDefault(item => item.MaDthamQuan == visit.MaDthamQuan);
                var playTicket = play is null ? null : tickets.FirstOrDefault(item => item.MaDthamQuan == play.MaDthamQuan);
                var order = 1;
                if (!smart)
                {
                    if (day == 1)
                    {
                        await AddLine(plan, day, order++, new TimeSpan(7, 0, 0), hotel, 0, 1,
                            $"Có mặt tại {hotel.MaDoiTacNavigation.TenDoiTac}", cancellationToken);
                        await AddLine(plan, day, order++, new TimeSpan(9, 0, 0), hotel, hotel.GiaNiemYet, nights,
                            $"Check-in phòng {hotel.TenSanPham} — {nights} đêm", cancellationToken);
                        if (meal is not null)
                            await AddLine(plan, day, order++, new TimeSpan(11, 0, 0), meal, meal.GiaNiemYet, guests,
                                $"Dùng bữa tại {meal.MaDoiTacNavigation.TenDoiTac}", cancellationToken);
                        await AddVisit(plan, day, order++, new TimeSpan(15, 0, 0), visit, ticket, guests, cancellationToken);
                    }
                    else
                    {
                        await AddLine(plan, day, order++, new TimeSpan(7, 0, 0), hotel, 0, 1,
                            $"Xuất phát từ {hotel.MaDoiTacNavigation.TenDoiTac}", cancellationToken);
                        if (play is not null)
                            await AddVisit(plan, day, order++, new TimeSpan(9, 0, 0), play, playTicket, guests, cancellationToken);
                        if (meal is not null)
                            await AddLine(plan, day, order++, new TimeSpan(11, 0, 0), meal, meal.GiaNiemYet, guests,
                                $"Dùng bữa tại {meal.MaDoiTacNavigation.TenDoiTac}", cancellationToken);
                        await AddVisit(plan, day, order, new TimeSpan(15, 0, 0), visit, ticket, guests, cancellationToken);
                    }
                    continue;
                }

                if (day == 1)
                {
                    var arrive = departTime.Add(TimeSpan.FromMinutes(goLeg?.Minutes ?? 0));
                    if (arrive.TotalHours >= 24)
                        arrive = new TimeSpan(23, 0, 0);
                    await AddLine(plan, day, order++, departTime, hotel, 0, 1,
                        $"Khởi hành {origin?.TenTinh ?? "điểm xuất phát"} → {placeName} bằng {goLeg?.Label ?? "xe"} (~{goLeg?.Minutes ?? 0} phút)",
                        cancellationToken, visit);
                    await AddLine(plan, day, order++, ClampTime(arrive), hotel, hotel.GiaNiemYet, nights,
                        $"Check-in {hotel.MaDoiTacNavigation.TenDoiTac} · {hotel.TenSanPham} — {nights} đêm. Địa chỉ khu vực {placeName}.",
                        cancellationToken);
                    if (arrive < HotelStayRules.LastActivity.Add(TimeSpan.FromHours(-2)) && meal is not null)
                        await AddLine(plan, day, order++, ClampTime(arrive.Add(TimeSpan.FromHours(1))), meal, meal.GiaNiemYet, guests,
                            $"Dùng bữa tại {meal.MaDoiTacNavigation.TenDoiTac}", cancellationToken);
                    if (arrive < new TimeSpan(18, 0, 0))
                        await AddVisit(plan, day, order++, new TimeSpan(15, 30, 0), visit, ticket, guests, cancellationToken);
                }
                else if (day == days && backLeg is not null)
                {
                    var leave = returnBy.Subtract(TimeSpan.FromMinutes(backLeg.Minutes));
                    if (leave < TimeSpan.Zero)
                        leave = new TimeSpan(8, 0, 0);
                    await AddLine(plan, day, order++, new TimeSpan(7, 30, 0), hotel, 0, 1,
                        $"Xuất phát từ {hotel.MaDoiTacNavigation.TenDoiTac}", cancellationToken);
                    if (leave >= new TimeSpan(11, 0, 0) && meal is not null)
                        await AddLine(plan, day, order++, new TimeSpan(8, 30, 0), meal, meal.GiaNiemYet, guests,
                            $"Dùng bữa tại {meal.MaDoiTacNavigation.TenDoiTac}", cancellationToken);
                    if (leave >= new TimeSpan(12, 0, 0))
                        await AddVisit(plan, day, order++, new TimeSpan(9, 30, 0), visit, ticket, guests, cancellationToken);
                    await AddLine(plan, day, order, ClampTime(leave), hotel, 0, 1,
                        $"Về {origin?.TenTinh ?? "điểm xuất phát"} bằng {backLeg.Label} (~{backLeg.Minutes} phút), có mặt trước {returnBy:hh\\:mm}.",
                        cancellationToken);
                }
                else
                {
                    var slots = new List<TimeSpan> { new(8, 0, 0), new(11, 0, 0), new(14, 0, 0), new(16, 30, 0) }
                        .Take(eventsWanted).ToList();
                    await AddLine(plan, day, order++, slots[0], hotel, 0, 1,
                        $"Xuất phát từ {hotel.MaDoiTacNavigation.TenDoiTac}", cancellationToken);
                    var activityIndex = 0;
                    if (play is not null && slots.Count > 1)
                    {
                        await AddVisit(plan, day, order++, slots[Math.Min(1, slots.Count - 1)], play, playTicket, guests, cancellationToken);
                        activityIndex++;
                    }
                    if (meal is not null)
                        await AddLine(plan, day, order++, new TimeSpan(11, 30, 0), meal, meal.GiaNiemYet, guests,
                            $"Dùng bữa tại {meal.MaDoiTacNavigation.TenDoiTac}", cancellationToken);
                    if (activityIndex < eventsWanted)
                        await AddVisit(plan, day, order, new TimeSpan(15, 0, 0), visit, ticket, guests, cancellationToken);
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

    private static int DaysFromRange(YeuCauThietKe request) => 1;

    private static TimeSpan ClampTime(TimeSpan time)
    {
        if (time < TimeSpan.Zero) return TimeSpan.Zero;
        if (time >= HotelStayRules.LastActivity) return HotelStayRules.LastActivity;
        return new TimeSpan(time.Hours, time.Minutes, 0);
    }

    private static string BudgetNote(string place, SanPhamDoiTac hotel, TravelLeg? go, int? budget, double ratio)
    {
        var target = budget is > 0 ? $" Mục tiêu khoảng {ratio:P0} ngân sách ({budget * ratio:N0} đ, sai số ±12%)." : "";
        var ride = go is null ? "" : $" Chiều đi: {go.Label} ~{go.Minutes} phút ({go.Source}).";
        return $"Lịch {place}. Một khách sạn: {hotel.MaDoiTacNavigation.TenDoiTac}.{ride}{target}";
    }

    private static SanPhamDoiTac SelectHotel(IReadOnlyList<SanPhamDoiTac> hotels, int planNumber, int? budget, int nights, int guests)
    {
        if (hotels.Count == 1)
            return hotels[0];
        if (budget is > 0)
        {
            var ratio = planNumber switch { 1 => 0.88, 2 => 1.0, _ => 1.12 };
            var roomBudget = Math.Max(1, (int)(budget.Value * ratio * 0.45) / Math.Max(1, nights));
            return hotels.OrderBy(item => Math.Abs(item.GiaNiemYet - roomBudget)).First();
        }
        var index = planNumber switch { 1 => 0, 2 => hotels.Count / 2, _ => hotels.Count - 1 };
        return hotels[index];
    }

    private async Task AddVisit(LichTrinhDeXuat plan, int day, int order, TimeSpan time,
        DiemThamQuan point, SanPhamDoiTac? ticket, int guests, CancellationToken cancellationToken)
    {
        var qty = Math.Max(1, guests);
        var price = ticket?.GiaNiemYet ?? 0;
        var address = string.IsNullOrWhiteSpace(point.DiaChi) ? point.TenDiaDanh : point.DiaChi.Trim();
        var line = new LichTrinhDeXuatChiTiet
        {
            MaChiTiet = await GenerateDetailIdAsync(cancellationToken),
            MaDeXuat = plan.MaDeXuat,
            NgayThu = day,
            ThuTuTrongNgay = order,
            MaDthamQuan = point.MaDthamQuan,
            MaSanPham = ticket?.MaSanPham,
            SoLuong = qty,
            DonGia = price,
            ThanhTien = TuThietKeTourPricing.CalculateLine(price, qty),
            GioBatDau = time,
            Mota = HotelStayRules.FormatSlot(time, $"Tham quan tại {point.TenDiaDanh} — {address}")
        };
        plan.ChiTiets.Add(line);
    }

    private async Task AddLine(LichTrinhDeXuat plan, int day, int order, TimeSpan time,
        SanPhamDoiTac product, int donGia, int soLuong, string caption, CancellationToken cancellationToken,
        DiemThamQuan? point = null)
    {
        plan.ChiTiets.Add(new LichTrinhDeXuatChiTiet
        {
            MaChiTiet = await GenerateDetailIdAsync(cancellationToken),
            MaDeXuat = plan.MaDeXuat,
            NgayThu = day,
            ThuTuTrongNgay = order,
            MaDthamQuan = point?.MaDthamQuan ?? product.MaDthamQuan,
            MaSanPham = product.MaSanPham,
            SoLuong = soLuong,
            DonGia = donGia,
            ThanhTien = TuThietKeTourPricing.CalculateLine(donGia, soLuong),
            GioBatDau = time,
            Mota = HotelStayRules.FormatSlot(time, caption)
        });
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
