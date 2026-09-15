using TourDuLich.Application.Helpers;
using TourDuLich.Application.Services;
using TourDuLich.Infrastructure.Entities;
using Xunit;

namespace TourDuLich.IntegrationTests;

public sealed class HotelStayRulesTests
{
    [Fact]
    public void RegionMismatch_SameProvince_DoesNotCompareKhuVucToTinh()
    {
        var hotel = Hotel("TN01", "KV001");
        var error = HotelStayRules.RegionMismatchMessage(new[]
        {
            (1, "TN01", "KV001", hotel)
        });
        Assert.Null(error);
    }

    [Fact]
    public void RegionMismatch_HotelHaNoi_PointNhaTrang_Fails()
    {
        var hotel = Hotel("TN01", "KV001");
        var error = HotelStayRules.RegionMismatchMessage(new[]
        {
            (1, "TN37", "KV002", hotel)
        });
        Assert.Equal("Khách sạn phải cùng tỉnh với điểm tham quan.", error);
    }

    private static SanPhamDoiTac Hotel(string maTinh, string maKhuVuc) => new()
    {
        MaSanPham = "SP1",
        MaDoiTac = "DT1",
        TenSanPham = "Deluxe",
        MaDoiTacNavigation = new DoiTac
        {
            MaDoiTac = "DT1",
            TenDoiTac = "KS",
            LoaiDoiTac = FixedLengthHelper.PadTo20("LuuTru"),
            MaTinh = FixedLengthHelper.PadTo20(maTinh),
            MaKhuVuc = FixedLengthHelper.PadTo20(maKhuVuc)
        }
    };
}
