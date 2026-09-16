using Microsoft.EntityFrameworkCore;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.Application.Services;

public sealed class TravelLeg
{
    public string Mode { get; init; } = "XeKhach";
    public int Minutes { get; init; }
    public int? Cost { get; init; }
    public string Source { get; init; } = "estimate";
    public string Label => Mode switch
    {
        "MayBay" => "Máy bay",
        "Tau" => "Tàu hỏa",
        "XeMay" => "Xe máy",
        _ => "Xe khách"
    };
}

public interface ITravelTimeService
{
    Task<IReadOnlyList<TravelLeg>> EstimateAsync(TinhThanh origin, TinhThanh destination, CancellationToken cancellationToken = default);
    TravelLeg BestOutbound(IReadOnlyList<TravelLeg> legs, TimeSpan departTime);
}

public sealed class TravelTimeService : ITravelTimeService
{
    private readonly AppDbContext _context;

    public TravelTimeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TravelLeg>> EstimateAsync(TinhThanh origin, TinhThanh destination, CancellationToken cancellationToken = default)
    {
        if (FixedLengthHelper.TrimSafe(origin.MaTinh) == FixedLengthHelper.TrimSafe(destination.MaTinh))
            return [new TravelLeg { Mode = "XeKhach", Minutes = 35, Cost = 80000, Source = "same-city" }];

        var from = FixedLengthHelper.PadTo20(origin.MaTinh);
        var to = FixedLengthHelper.PadTo20(destination.MaTinh);
        List<MatranDiChuyen> rows;
        try
        {
            rows = await _context.MatranDiChuyens.AsNoTracking()
                .Where(item => item.MaTinhDi == from && item.MaTinhDen == to)
                .ToListAsync(cancellationToken);
        }
        catch (Exception)
        {
            rows = [];
        }

        var legs = rows.Select(item => new TravelLeg
        {
            Mode = FixedLengthHelper.TrimSafe(item.PhuongTien) ?? "XeKhach",
            Minutes = item.SoPhut,
            Cost = item.ChiPhiUocTinh,
            Source = "matrix"
        }).ToList();

        if (legs.Count == 0)
            legs.AddRange(EstimateByDistance(origin, destination));

        return legs.Where(item => item.Minutes > 0).OrderBy(item => item.Minutes).ToList();
    }

    public TravelLeg BestOutbound(IReadOnlyList<TravelLeg> legs, TimeSpan departTime)
    {
        if (legs.Count == 0)
            return new TravelLeg { Mode = "XeKhach", Minutes = 240, Source = "fallback" };
        var driving = legs.FirstOrDefault(item => item.Mode is "XeKhach" or "XeMay") ?? legs[0];
        var flight = legs.FirstOrDefault(item => item.Mode == "MayBay");
        if (flight is not null && (driving.Minutes >= 360 || departTime.TotalHours >= 16))
            return flight;
        return driving.Minutes > 0 ? driving : legs[0];
    }

    private static IEnumerable<TravelLeg> EstimateByDistance(TinhThanh origin, TinhThanh destination)
    {
        var km = RoadKm(origin, destination);
        if (km <= 0)
        {
            yield return new TravelLeg { Mode = "XeKhach", Minutes = 240, Cost = 180000, Source = "region" };
            yield break;
        }

        var bus = Minutes(km, 52, 25);
        yield return new TravelLeg { Mode = "XeKhach", Minutes = bus, Cost = CostBus(km), Source = "haversine" };

        if (km <= 450)
            yield return new TravelLeg { Mode = "XeMay", Minutes = Minutes(km, 42, 15), Source = "haversine" };

        if (km >= 80)
            yield return new TravelLeg { Mode = "Tau", Minutes = Minutes(km, 65, 40), Cost = CostTrain(km), Source = "haversine" };

        if (km >= 280 || IsIsland(destination) || IsIsland(origin) || km >= 700)
            yield return new TravelLeg
            {
                Mode = "MayBay",
                Minutes = 90 + (int)Math.Round(km / 14.0),
                Cost = CostFlight(km),
                Source = "estimate"
            };
    }

    private static bool IsIsland(TinhThanh province)
    {
        var id = FixedLengthHelper.TrimSafe(province.MaTinh);
        var name = (province.TenTinh ?? "").ToLowerInvariant();
        return id is "TN58" or "TN63" || name.Contains("phú quốc") || name.Contains("côn đảo") || name.Contains("kiên giang");
    }

    private static int Minutes(double km, double kmh, int extra)
        => Math.Max(25, extra + (int)Math.Round(km / kmh * 60));

    private static int CostBus(double km) => (int)Math.Clamp(Math.Round(km * 900), 60000, 1200000);
    private static int CostTrain(double km) => (int)Math.Clamp(Math.Round(km * 1100), 80000, 1500000);
    private static int CostFlight(double km) => (int)Math.Clamp(Math.Round(900000 + km * 1800), 900000, 3500000);

    private static double RoadKm(TinhThanh origin, TinhThanh destination)
    {
        var a = Coords.For(origin.MaTinh);
        var b = Coords.For(destination.MaTinh);
        if (a is null || b is null)
            return SameRegion(origin, destination) ? 180 : 780;
        return HaversineKm(a.Value.lat, a.Value.lng, b.Value.lat, b.Value.lng) * 1.32;
    }

    private static bool SameRegion(TinhThanh origin, TinhThanh destination)
        => FixedLengthHelper.TrimSafe(origin.MaKhuVuc) == FixedLengthHelper.TrimSafe(destination.MaKhuVuc);

    private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double earth = 6371;
        var dLat = ToRad(lat2 - lat1);
        var dLng = ToRad(lng2 - lng1);
        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return 2 * earth * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;

    private static class Coords
    {
        // Tọa độ trung tâm tỉnh/thành — không cần Google.
        private static readonly Dictionary<string, (double lat, double lng)> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["TN01"] = (21.0285, 105.8542),
            ["TN02"] = (20.8449, 106.6881),
            ["TN03"] = (20.9516, 107.0800),
            ["TN04"] = (21.1861, 106.0763),
            ["TN05"] = (20.9373, 106.3146),
            ["TN06"] = (20.6464, 106.0511),
            ["TN07"] = (21.3089, 105.6049),
            ["TN08"] = (21.5942, 105.8482),
            ["TN09"] = (21.3227, 105.4024),
            ["TN10"] = (21.2731, 106.1946),
            ["TN11"] = (21.8536, 106.7610),
            ["TN12"] = (22.6666, 106.2630),
            ["TN13"] = (22.8233, 104.9836),
            ["TN14"] = (21.8233, 105.2140),
            ["TN15"] = (22.4856, 103.9707),
            ["TN16"] = (21.7229, 104.9113),
            ["TN17"] = (21.3860, 103.0160),
            ["TN18"] = (22.3964, 103.4586),
            ["TN19"] = (21.3256, 103.9188),
            ["TN20"] = (20.8133, 105.3383),
            ["TN21"] = (20.2506, 105.9744),
            ["TN22"] = (20.4389, 106.1621),
            ["TN23"] = (20.4463, 106.3369),
            ["TN24"] = (20.5411, 105.9139),
            ["TN25"] = (22.1470, 105.8348),
            ["TN26"] = (19.8067, 105.7852),
            ["TN27"] = (18.6796, 105.6813),
            ["TN28"] = (18.3428, 105.9057),
            ["TN29"] = (17.4687, 106.5983),
            ["TN30"] = (16.8163, 107.1005),
            ["TN31"] = (16.4637, 107.5909),
            ["TN32"] = (16.0544, 108.2022),
            ["TN33"] = (15.8794, 108.3350),
            ["TN34"] = (15.1214, 108.8044),
            ["TN35"] = (13.7820, 109.2196),
            ["TN36"] = (13.0955, 109.3210),
            ["TN37"] = (12.2388, 109.1967),
            ["TN38"] = (11.5643, 108.9886),
            ["TN39"] = (10.9280, 108.1020),
            ["TN40"] = (14.3497, 108.0005),
            ["TN41"] = (13.9833, 108.0000),
            ["TN42"] = (12.6667, 108.0500),
            ["TN43"] = (12.0042, 107.6907),
            ["TN44"] = (11.9404, 108.4583),
            ["TN45"] = (10.8231, 106.6297),
            ["TN46"] = (10.9574, 106.8426),
            ["TN47"] = (10.9804, 106.6519),
            ["TN48"] = (10.3460, 107.0843),
            ["TN49"] = (11.3352, 106.1099),
            ["TN50"] = (11.5349, 106.8832),
            ["TN51"] = (10.5359, 106.4137),
            ["TN52"] = (10.3600, 106.3597),
            ["TN53"] = (10.2415, 106.3759),
            ["TN54"] = (10.2397, 105.9571),
            ["TN55"] = (9.9347, 106.3453),
            ["TN56"] = (10.4672, 105.6320),
            ["TN57"] = (10.3864, 105.4352),
            ["TN58"] = (10.2270, 103.9600),
            ["TN59"] = (10.0452, 105.7469),
            ["TN60"] = (9.7840, 105.4701),
            ["TN61"] = (9.6037, 105.9800),
            ["TN62"] = (9.2941, 105.7278),
            ["TN63"] = (9.1767, 105.1524),
        };

        public static (double lat, double lng)? For(string? maTinh)
        {
            var key = FixedLengthHelper.TrimSafe(maTinh);
            if (string.IsNullOrWhiteSpace(key))
                return null;
            return Map.TryGetValue(key, out var value) ? value : null;
        }
    }
}
