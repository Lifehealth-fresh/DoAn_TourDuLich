using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.Application.Services;

public static class HotelStayRules
{
    public const string LoaiLuuTru = "LuuTru";

    public static bool IsHotelPartner(string? loaiDoiTac) =>
        string.Equals(FixedLengthHelper.TrimSafe(loaiDoiTac), LoaiLuuTru, StringComparison.OrdinalIgnoreCase);

    public static bool IsHotelProduct(SanPhamDoiTac? product) =>
        product is not null && IsHotelPartner(product.MaDoiTacNavigation?.LoaiDoiTac);

    public static string HotelCaption(SanPhamDoiTac product)
    {
        var hotel = product.MaDoiTacNavigation?.TenDoiTac?.Trim() ?? "Khách sạn";
        var room = product.TenSanPham.Trim();
        return $"Nghỉ đêm: {hotel} · {room} ({product.GiaNiemYet:N0} đ/đêm)";
    }

    public static string? MissingHotelMessage(
        IEnumerable<(int NgayThu, int ThuTu, SanPhamDoiTac? Product)> lines)
    {
        var days = lines.GroupBy(item => item.NgayThu).ToList();
        if (days.Count == 0)
            return "Lịch trình phải có ít nhất một ngày.";
        foreach (var day in days.OrderBy(item => item.Key))
        {
            var last = day.OrderBy(item => item.ThuTu).Last();
            if (!IsHotelProduct(last.Product))
                return "Mỗi ngày phải kết thúc bằng khách sạn.";
        }
        return null;
    }

    public static string? RegionMismatchMessage(
        IEnumerable<(int NgayThu, string? PointRegion, SanPhamDoiTac? Product)> lines)
    {
        foreach (var day in lines.GroupBy(item => item.NgayThu))
        {
            var pointRegions = day
                .Select(item => FixedLengthHelper.TrimSafe(item.PointRegion))
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var hotelRegions = day
                .Where(item => IsHotelProduct(item.Product))
                .Select(item => FixedLengthHelper.TrimSafe(item.Product!.MaDoiTacNavigation?.MaKhuVuc))
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (pointRegions.Count > 0 && hotelRegions.Any(region => !pointRegions.Contains(region, StringComparer.OrdinalIgnoreCase)))
                return "Khách sạn phải cùng khu vực với điểm tham quan trong ngày.";
        }
        return null;
    }
}