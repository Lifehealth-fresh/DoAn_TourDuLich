using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace TourDuLich.IntegrationTests;

public sealed class RbacTests : ApiTestBase
{
    [Fact]
    public async Task TourWrite_IsPubliclyBlocked_KhachHangSaleAndAnonymous()
    {
        var customer = await RegisterAsync();
        UseToken(customer);
        var payload = new { maTour = Unique("IT"), tenTour = "RBAC", giaTour = 1, slkhach = 2, loaiTour = "Chuan", trangThai = "HoatDong" };
        (await Client.PostAsJsonAsync("/api/Tour", payload)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var sale = await LoginSaleAsync();
        UseToken(sale);
        (await Client.PostAsJsonAsync("/api/Tour", payload)).StatusCode.Should().Be(HttpStatusCode.Created);

        Client.DefaultRequestHeaders.Authorization = null;
        (await Client.PostAsJsonAsync("/api/Tour", new
        {
            maTour = Unique("IT"), tenTour = "RBAC", giaTour = 1,
            slkhach = 2, loaiTour = "Chuan", trangThai = "HoatDong"
        })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminAccounts_RequireAdminRole()
    {
        var sale = await LoginSaleAsync();
        UseToken(sale);
        (await Client.GetAsync("/api/Admin/tai-khoan")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var customer = await RegisterAsync();
        UseToken(customer);
        (await Client.GetAsync("/api/Admin/tai-khoan")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        UseToken(await LoginAdminAsync());
        (await Client.GetAsync("/api/Admin/tai-khoan")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SubmitForApproval_RequiresSaleOrAdmin()
    {
        UseToken(await RegisterAsync());
        (await Client.PutAsJsonAsync("/api/YeuCauThietKe/UNKNOWN/gui-duyet", new { })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
