using TourDuLich.Application.Helpers;

namespace TourDuLich.API.Services;

public readonly record struct ReviewFact(int Sao, DateTime ThoiGian, string MaTour, bool CongKhai);

public sealed class ReviewDashboardDto
{
    public int TongSo { get; set; }
    public double? DiemTrungBinh { get; set; }
    public double? DiemTrungVi { get; set; }
    public double TyLeTichCuc { get; set; }
    public double TyLeTieuCuc { get; set; }
    public double? Diem30Ngay { get; set; }
    public double? Diem90Ngay { get; set; }
    public int SoMoiHomNay { get; set; }
    public int SoMoiTuan { get; set; }
    public int SoMoiThang { get; set; }
    public double TocDoTang { get; set; }
    public int NoiBo { get; set; }
    public IReadOnlyList<object> PhanBoSao { get; set; } = [];
    public IReadOnlyList<object> XuHuong { get; set; } = [];
    public IReadOnlyList<object> TheoTour { get; set; } = [];
}

public static class ReviewDashboardBuilder
{
    public static bool IsSelfDesigned(string? loaiTour) =>
        string.Equals((loaiTour ?? string.Empty).Trim(), "TuThietKe", StringComparison.OrdinalIgnoreCase);

    public static string GuestName(string? ho, string? ten)
    {
        var name = $"{ho} {ten}".Trim();
        return string.IsNullOrWhiteSpace(name) ? "Khách ANAM" : name;
    }

    public static ReviewDashboardDto Build(
        IReadOnlyList<ReviewFact> rows,
        IReadOnlyDictionary<string, string> tourNames,
        DateTime now)
    {
        var publicRows = rows.Where(r => r.CongKhai).ToList();
        var stars = publicRows.Select(r => r.Sao).ToList();
        var day0 = now.Date;
        var d30 = now.AddDays(-30);
        var d90 = now.AddDays(-90);
        var week = now.AddDays(-7);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var prevStart = monthStart.AddMonths(-1);
        var thisMonth = publicRows.Count(r => r.ThoiGian >= monthStart);
        var lastMonth = publicRows.Count(r => r.ThoiGian >= prevStart && r.ThoiGian < monthStart);
        var positive = publicRows.Count(r => r.Sao >= 4);
        var negative = publicRows.Count(r => r.Sao <= 2);

        var phanBo = Enumerable.Range(1, 5).Reverse().Select(sao =>
        {
            var so = publicRows.Count(r => r.Sao == sao);
            return (object)new
            {
                sao,
                soLuong = so,
                tyLe = publicRows.Count == 0 ? 0 : Math.Round(so / (double)publicRows.Count, 4)
            };
        }).ToList();

        var fromMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
        var xuHuong = Enumerable.Range(0, 6).Select(offset =>
        {
            var month = fromMonth.AddMonths(offset);
            var next = month.AddMonths(1);
            var slice = publicRows.Where(r => r.ThoiGian >= month && r.ThoiGian < next).ToList();
            return (object)new
            {
                nam = month.Year,
                thang = month.Month,
                nhan = $"T{month.Month}/{month.Year}",
                diemTb = Average(slice.Select(r => r.Sao)),
                soDanhGia = slice.Count,
                soTieuCuc = slice.Count(r => r.Sao <= 2),
                tyLeTichCuc = slice.Count == 0 ? 0 : Math.Round(slice.Count(r => r.Sao >= 4) / (double)slice.Count, 4)
            };
        }).ToList();

        var theoTour = publicRows
            .GroupBy(r => r.MaTour)
            .Select(g =>
            {
                var ma = FixedLengthHelper.TrimSafe(g.Key) ?? g.Key;
                tourNames.TryGetValue(g.Key, out var ten);
                if (string.IsNullOrWhiteSpace(ten))
                    tourNames.TryGetValue(ma, out ten);
                var n = g.Count();
                return new
                {
                    maTour = ma,
                    tenTour = ten ?? ma,
                    diemTb = Average(g.Select(r => r.Sao)),
                    soDanhGia = n,
                    tyLeTieuCuc = Math.Round(g.Count(r => r.Sao <= 2) / (double)n, 4),
                    tyLeTichCuc = Math.Round(g.Count(r => r.Sao >= 4) / (double)n, 4)
                };
            })
            .OrderByDescending(x => x.soDanhGia)
            .ThenByDescending(x => x.diemTb)
            .Take(12)
            .Cast<object>()
            .ToList();

        return new ReviewDashboardDto
        {
            TongSo = publicRows.Count,
            DiemTrungBinh = Average(stars),
            DiemTrungVi = Median(stars),
            TyLeTichCuc = publicRows.Count == 0 ? 0 : Math.Round(positive / (double)publicRows.Count, 4),
            TyLeTieuCuc = publicRows.Count == 0 ? 0 : Math.Round(negative / (double)publicRows.Count, 4),
            Diem30Ngay = Average(publicRows.Where(r => r.ThoiGian >= d30).Select(r => r.Sao)),
            Diem90Ngay = Average(publicRows.Where(r => r.ThoiGian >= d90).Select(r => r.Sao)),
            SoMoiHomNay = publicRows.Count(r => r.ThoiGian >= day0),
            SoMoiTuan = publicRows.Count(r => r.ThoiGian >= week),
            SoMoiThang = thisMonth,
            TocDoTang = lastMonth == 0
                ? (thisMonth == 0 ? 0 : 1)
                : Math.Round((thisMonth - lastMonth) / (double)lastMonth, 4),
            NoiBo = rows.Count(r => !r.CongKhai),
            PhanBoSao = phanBo,
            XuHuong = xuHuong,
            TheoTour = theoTour
        };
    }

    private static double? Average(IEnumerable<int> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? null : Math.Round(list.Average(), 2);
    }

    private static double? Median(List<int> values)
    {
        if (values.Count == 0) return null;
        var ordered = values.OrderBy(v => v).ToList();
        var mid = ordered.Count / 2;
        return ordered.Count % 2 == 1
            ? ordered[mid]
            : Math.Round((ordered[mid - 1] + ordered[mid]) / 2.0, 2);
    }
}
