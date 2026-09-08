using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace TourDuLich.IntegrationTests;

public sealed class SensitiveEndpointRbacTests : ApiTestBase
{
    [Fact]
    public async Task Customer_CannotCallSaleOrAdminWriteEndpoints()
    {
        SkipIfNoConnection();
        UseToken(await RegisterAsync());

        var requests = new[]
        {
            Client.PostAsJsonAsync("/api/Tour", new { maTour = Unique("RB"), tenTour = "blocked", giaTour = 1, slkhach = 1, loaiTour = "Chuan" }),
            Client.PostAsJsonAsync("/api/DiemThamQuan", new { maDthamQuan = Unique("RB"), tenDiaDanh = "blocked" }),
            Client.PostAsJsonAsync("/api/KhuVuc", new { maKhuVuc = Unique("RB"), tenKhuVuc = "blocked" }),
            Client.PostAsJsonAsync("/api/DoiTac", new { tenDoiTac = "blocked", loaiDoiTac = "LuuTru" }),
            Client.PostAsJsonAsync("/api/KhuyenMai", new { tenKm = "blocked" }),
            Client.PostAsJsonAsync("/api/AnhTour", new { maTour = "UNKNOWN", url = "https://example.com/a.jpg", loaiMedia = "Anh" }),
            Client.PutAsJsonAsync("/api/DatDichVu/UNKNOWN/trang-thai", new { trangThai = "DaXacNhan" }),
            Client.PostAsJsonAsync("/api/HopDong", new { maBooking = "UNKNOWN", dieuKhoanCamKet = "blocked" }),
            Client.PostAsJsonAsync("/api/YeuCauThietKe/UNKNOWN/sinh-de-xuat", new { }),
            Client.PutAsJsonAsync("/api/YeuCauThietKe/UNKNOWN/tu-choi-boi-sale", new { lyDoTuChoi = "blocked" }),
            Client.PutAsJsonAsync("/api/YeuCauThietKe/UNKNOWN/sua-lich-trinh", new { chiTiets = Array.Empty<object>() })
        };

        var responses = await Task.WhenAll(requests);
        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Sale_CannotCallAdminOnlyEndpoint_ButAdminCan()
    {
        SkipIfNoConnection();
        UseToken(await LoginSaleAsync());
        (await Client.GetAsync("/api/Admin/tai-khoan")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        UseToken(await LoginAdminAsync());
        (await Client.GetAsync("/api/Admin/tai-khoan")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MissingToken_Returns401_WhileCustomerTokenReturns403()
    {
        SkipIfNoConnection();
        Client.DefaultRequestHeaders.Authorization = null;
        (await Client.GetAsync("/api/Admin/tai-khoan")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        UseToken(await RegisterAsync());
        (await Client.GetAsync("/api/Admin/tai-khoan")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
