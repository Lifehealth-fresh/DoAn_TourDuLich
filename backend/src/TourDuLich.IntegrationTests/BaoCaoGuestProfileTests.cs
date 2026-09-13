using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace TourDuLich.IntegrationTests;

public sealed class BaoCaoGuestProfileTests : ApiTestBase
{
    [Fact]
    public async Task Customer_CannotReadOverviewReport()
    {
        SkipIfNoConnection();
        UseToken(await RegisterAsync());
        Assert.Equal(HttpStatusCode.Forbidden, (await Client.GetAsync("/api/BaoCao/tong-quan")).StatusCode);
    }

    [Fact]
    public async Task Staff_ReadsOverviewReportShape()
    {
        SkipIfNoConnection();
        UseToken(await LoginAdminAsync());
        var response = await Client.GetAsync("/api/BaoCao/tong-quan");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("theoTrangThai", out _));
        Assert.True(json.TryGetProperty("doanhThuTheoThang", out var months));
        Assert.Equal(6, months.GetArrayLength());
        Assert.True(json.TryGetProperty("topTour", out _));
        Assert.True(json.TryGetProperty("lichSapKhoiHanh", out _));
        Assert.True(json.TryGetProperty("daThu", out _));
    }

    [Fact]
    public async Task Staff_CanEditGuestProfileAndDocumentsOnPaidOrHeldBooking()
    {
        SkipIfNoConnection();
        var sale = await LoginSaleAsync();
        var tour = await CreateTourAsync(sale);
        var departure = await CreateDepartureAsync(sale, tour, DateTime.UtcNow.AddDays(21));
        var customer = await RegisterAsync();
        UseToken(customer);
        var created = await Client.PostAsJsonAsync("/api/DatDichVu", new
        {
            maTour = tour, maKhoiHanh = departure, slnguoiLon = 1, sltreEm = 0
        });
        created.EnsureSuccessStatusCode();
        var bookingId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maBooking").GetString();
        UseToken(sale);
        var saved = await Client.PutAsJsonAsync($"/api/DatDichVu/{bookingId}/ho-so-khach", new
        {
            ho = "Nguyen", ten = "An", quocTich = "Việt Nam", email = "an@example.invalid",
            ngaySinh = "1995-05-01", danhXung = "Anh", gioiTinh = "Nam"
        });
        saved.EnsureSuccessStatusCode();
        var profile = await saved.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("An", profile.GetProperty("ten").GetString());
        Assert.False(string.IsNullOrWhiteSpace(profile.GetProperty("maKhachHang").GetString()));

        var added = await Client.PostAsJsonAsync($"/api/DatDichVu/{bookingId}/ho-so-khach/giay-to", new
        {
            loaiGiayTo = "CCCD", soTrenGiayTo = "079095000001",
            ngayCap = "2020-01-01", ngayHetHan = "2035-01-01", noiCap = "CA TPHCM"
        });
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        var withDoc = await added.Content.ReadFromJsonAsync<JsonElement>();
        var maGiayTo = withDoc.GetProperty("giayTo")[0].GetProperty("maGiayTo").GetString();
        Assert.Equal("CCCD", withDoc.GetProperty("giayTo")[0].GetProperty("loaiGiayTo").GetString());

        var updated = await Client.PutAsJsonAsync($"/api/DatDichVu/{bookingId}/ho-so-khach/giay-to/{maGiayTo}", new
        {
            loaiGiayTo = "Passport", soTrenGiayTo = "B12345678",
            ngayCap = "2021-02-02", ngayHetHan = "2031-02-02", noiCap = "Cục QLHC"
        });
        updated.EnsureSuccessStatusCode();
        Assert.Equal("Passport", (await updated.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("giayTo")[0].GetProperty("loaiGiayTo").GetString());

        UseToken(customer);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Client.PutAsJsonAsync($"/api/DatDichVu/{bookingId}/ho-so-khach", new { ho = "X", ten = "Y" })).StatusCode);
    }
}
