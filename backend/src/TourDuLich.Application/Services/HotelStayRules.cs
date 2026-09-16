using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.Application.Services;

public static class HotelStayRules
{
    public const string LoaiLuuTru = "LuuTru";
    public const string LoaiAnUong = "AnUong";
    public const string LoaiHoatDong = "HoatDong";
    public static readonly TimeSpan LastActivity = new(20, 30, 0);

    public static bool IsHotelPartner(string? loaiDoiTac) =>
        string.Equals(FixedLengthHelper.TrimSafe(loaiDoiTac), LoaiLuuTru, StringComparison.OrdinalIgnoreCase);

    public static bool IsDining(string? loaiDoiTac) =>
        string.Equals(FixedLengthHelper.TrimSafe(loaiDoiTac), LoaiAnUong, StringComparison.OrdinalIgnoreCase);

    public static bool IsActivity(string? loaiDoiTac) =>
        string.Equals(FixedLengthHelper.TrimSafe(loaiDoiTac), LoaiHoatDong, StringComparison.OrdinalIgnoreCase);

    public static bool IsHotelProduct(SanPhamDoiTac? product) =>
        product is not null && IsHotelPartner(product.MaDoiTacNavigation?.LoaiDoiTac);

    public static string HotelCaption(SanPhamDoiTac product)
    {
        var hotel = product.MaDoiTacNavigation?.TenDoiTac?.Trim() ?? "Khách sạn";
        var room = product.TenSanPham.Trim();
        return $"{hotel} · {room} ({product.GiaNiemYet:N0} đ/đêm)";
    }

    public static string FormatSlot(TimeSpan time, string text) =>
        $"{time:hh\\:mm} · {text}";

    public static string FormatRange(TimeSpan start, TimeSpan end, string text) =>
        end <= start ? FormatSlot(start, text) : $"{start:hh\\:mm}–{end:hh\\:mm} · {text}";

    public static string? ItineraryStructureMessage(
        IEnumerable<(int NgayThu, int ThuTu, string? MaDiem, SanPhamDoiTac? Product)> lines)
        => ItineraryStructureMessage(lines.Select(item =>
            (item.NgayThu, item.ThuTu, item.MaDiem, item.Product, (string?)null)));

    public static string? ItineraryStructureMessage(
        IEnumerable<(int NgayThu, int ThuTu, string? MaDiem, SanPhamDoiTac? Product, string? LoaiDong)> lines)
    {
        var list = lines.ToList();
        if (list.Count == 0)
            return "Lịch trình phải có ít nhất một ngày.";

        var hotels = list.Where(item => IsHotelProduct(item.Product))
            .Select(item => FixedLengthHelper.TrimSafe(item.Product!.MaDoiTac))
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (hotels.Count == 0)
            return "Cả lịch trình chỉ dùng một khách sạn lưu trú.";
        if (hotels.Count > 1)
            return "Cả lịch trình chỉ dùng một khách sạn.";

        var minDay = list.Min(item => item.NgayThu);
        var maxDay = list.Max(item => item.NgayThu);
        foreach (var day in list.GroupBy(item => item.NgayThu).OrderBy(item => item.Key))
        {
            var kinds = day.Select(item =>
            {
                var dining = IsDining(item.Product?.MaDoiTacNavigation?.LoaiDoiTac);
                var hotel = IsHotelProduct(item.Product);
                return string.IsNullOrWhiteSpace(item.LoaiDong)
                    ? ItineraryKinds.Infer(null, item.MaDiem, hotel, dining)
                    : item.LoaiDong.Trim();
            }).ToList();

            var hasStay = day.Any(item => IsHotelProduct(item.Product));
            if (!hasStay)
                return $"Ngày {day.Key} phải ghi nhận khách sạn đã chọn (cùng một nơi lưu trú).";

            var visitCount = day.Count(item =>
            {
                var kind = string.IsNullOrWhiteSpace(item.LoaiDong)
                    ? ItineraryKinds.Infer(null, item.MaDiem, false, false)
                    : item.LoaiDong.Trim();
                return ItineraryKinds.IsVisit(kind);
            });
            var meals = day.Count(item =>
            {
                var kind = string.IsNullOrWhiteSpace(item.LoaiDong)
                    ? ItineraryKinds.Infer(null, item.MaDiem, false, IsDining(item.Product?.MaDoiTacNavigation?.LoaiDoiTac))
                    : item.LoaiDong.Trim();
                return ItineraryKinds.IsMeal(kind);
            });

            if (day.Key == minDay)
            {
                if (!kinds.Contains(ItineraryKinds.CheckIn))
                    return $"Ngày {day.Key} (ngày đầu) phải có check-in tại khách sạn.";
                if (minDay != maxDay)
                    continue;
            }

            if (day.Key == maxDay)
            {
                var ordered = day.OrderBy(item => item.ThuTu).ThenBy(item => item.NgayThu).ToList();
                var lastKind = string.IsNullOrWhiteSpace(ordered[^1].LoaiDong)
                    ? ItineraryKinds.Infer(null, ordered[^1].MaDiem, IsHotelProduct(ordered[^1].Product), false)
                    : ordered[^1].LoaiDong.Trim();
                if (lastKind != ItineraryKinds.CheckOut && !kinds.Contains(ItineraryKinds.CheckOut))
                    return $"Ngày {day.Key} (ngày cuối) sự kiện cuối phải là check-out.";
                if (visitCount > 2)
                    return $"Ngày {day.Key} chỉ bố trí 1–2 điểm tham quan.";
                continue;
            }

            if (visitCount < 1)
                return $"Ngày {day.Key} phải có 1–2 địa điểm tham quan.";
            if (visitCount > 2)
                return $"Ngày {day.Key} chỉ bố trí 1–2 địa điểm tham quan.";
            if (meals < 1)
                return $"Ngày {day.Key} phải có ít nhất một bữa ăn.";
        }

        return null;
    }

    public static string? MissingHotelMessage(
        IEnumerable<(int NgayThu, int ThuTu, SanPhamDoiTac? Product)> lines)
        => ItineraryStructureMessage(lines.Select(item => (item.NgayThu, item.ThuTu, (string?)null, item.Product)));

    public static string? RegionMismatchMessage(
        IEnumerable<(int NgayThu, string? PointProvince, string? PointRegion, SanPhamDoiTac? Product)> lines)
    {
        foreach (var item in lines)
        {
            if (!IsHotelProduct(item.Product))
                continue;
            var hotelTinh = FixedLengthHelper.TrimSafe(item.Product!.MaDoiTacNavigation?.MaTinh);
            var hotelKv = FixedLengthHelper.TrimSafe(item.Product.MaDoiTacNavigation?.MaKhuVuc);
            var pointTinh = FixedLengthHelper.TrimSafe(item.PointProvince);
            var pointKv = FixedLengthHelper.TrimSafe(item.PointRegion);
            if (!string.IsNullOrWhiteSpace(hotelTinh) && !string.IsNullOrWhiteSpace(pointTinh))
            {
                if (!hotelTinh.Equals(pointTinh, StringComparison.OrdinalIgnoreCase))
                    return "Khách sạn phải cùng tỉnh với điểm tham quan.";
                continue;
            }
            if (!string.IsNullOrWhiteSpace(hotelKv) && !string.IsNullOrWhiteSpace(pointKv) &&
                !hotelKv.Equals(pointKv, StringComparison.OrdinalIgnoreCase))
                return "Khách sạn phải cùng tỉnh/khu vực với điểm tham quan.";
        }
        return null;
    }
}
