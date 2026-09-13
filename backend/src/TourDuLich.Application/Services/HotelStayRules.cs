using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.Application.Services;

public static class HotelStayRules
{
    public const string LoaiLuuTru = "LuuTru";
    public const string LoaiAnUong = "AnUong";
    public const string LoaiHoatDong = "HoatDong";

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

    public static string? ItineraryStructureMessage(
        IEnumerable<(int NgayThu, int ThuTu, string? MaDiem, SanPhamDoiTac? Product)> lines)
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

        foreach (var day in list.GroupBy(item => item.NgayThu).OrderBy(item => item.Key))
        {
            var hasVisit = day.Any(item =>
                !string.IsNullOrWhiteSpace(item.MaDiem) || IsActivity(item.Product?.MaDoiTacNavigation?.LoaiDoiTac));
            var hasMeal = day.Any(item => IsDining(item.Product?.MaDoiTacNavigation?.LoaiDoiTac));
            var hasStay = day.Any(item => IsHotelProduct(item.Product));
            if (!hasVisit)
                return $"Ngày {day.Key} phải có địa điểm tham quan hoặc khu vui chơi.";
            if (!hasMeal)
                return $"Ngày {day.Key} phải có điểm ăn uống.";
            if (!hasStay)
                return $"Ngày {day.Key} phải ghi nhận khách sạn đã chọn (cùng một nơi lưu trú).";
        }
        return null;
    }

    public static string? MissingHotelMessage(
        IEnumerable<(int NgayThu, int ThuTu, SanPhamDoiTac? Product)> lines)
        => ItineraryStructureMessage(lines.Select(item => (item.NgayThu, item.ThuTu, (string?)null, item.Product)));

    public static string? RegionMismatchMessage(
        IEnumerable<(int NgayThu, string? PointRegion, SanPhamDoiTac? Product)> lines)
    {
        var pointRegions = lines
            .Select(item => FixedLengthHelper.TrimSafe(item.PointRegion))
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var hotelRegions = lines
            .Where(item => IsHotelProduct(item.Product))
            .Select(item => FixedLengthHelper.TrimSafe(item.Product!.MaDoiTacNavigation?.MaTinh)
                            ?? FixedLengthHelper.TrimSafe(item.Product!.MaDoiTacNavigation?.MaKhuVuc))
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (pointRegions.Count > 0 && hotelRegions.Any(region =>
                !pointRegions.Contains(region, StringComparer.OrdinalIgnoreCase)))
            return "Khách sạn phải cùng tỉnh/khu vực với điểm tham quan.";
        return null;
    }
}