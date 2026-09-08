using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace TourDuLich.IntegrationTests;

public sealed class AuthorizationTests : ApiTestBase
{
    [Fact]
    public async Task PublicCatalog_IsAvailableWithoutToken_AndProtectedEndpointsFailClosed()
    {
        SkipIfNoConnection();
        Client.DefaultRequestHeaders.Authorization = null;

        (await Client.GetAsync("/api/Tour")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await Client.GetAsync("/api/DatDichVu/cua-toi")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RoleBoundEndpoints_RejectAuthenticatedUsersWithWrongRole()
    {
        SkipIfNoConnection();
        var customer = await RegisterAsync();
        UseToken(customer);
        (await Client.GetAsync("/api/Admin/tai-khoan")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var sale = await LoginSaleAsync();
        UseToken(sale);
        (await Client.GetAsync("/api/Admin/tai-khoan")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Client.GetAsync("/api/DatDichVu/cua-toi")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UiTelemetry_UsesJwtUser_AndRejectsForgedBusinessEvents()
    {
        SkipIfNoConnection();
        var sale = await LoginSaleAsync();
        var tour = await CreateTourAsync(sale);
        var customer = await RegisterAsync();
        UseToken(customer);

        (await Client.PostAsJsonAsync("/api/HanhViKhachHang", new
        {
            maTour = tour, hanhDong = "DatTour"
        })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var response = await Client.PostAsJsonAsync("/api/HanhViKhachHang", new
        {
            maTour = tour, hanhDong = "Xem", maUser = "USRADMIN001"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("maUser").GetString().Should().Be(customer.MaUser);
    }
}
