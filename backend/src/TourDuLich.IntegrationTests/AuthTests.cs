using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace TourDuLich.IntegrationTests;

public sealed class AuthTests : ApiTestBase
{
    [Fact]
    public async Task Register_IgnoresClientRole_AndLoginReturnsExpectedClaims()
    {
        SkipIfNoConnection();
        var phone = "0777" + Random.Shared.Next(100000, 999999).ToString("D6");
        var register = await Client.PostAsJsonAsync("/api/Auth/register", new
        {
            soDienThoai = phone, matKhau = "Test@123456", maVaiTro = 2
        });
        register.StatusCode.Should().Be(HttpStatusCode.Created);
        var registered = await register.Content.ReadFromJsonAsync<JsonElement>();
        registered.GetProperty("refreshToken").GetString().Should().NotBeNullOrWhiteSpace();
        registered.GetProperty("expiresIn").GetInt32().Should().BeGreaterThan(0);

        var wrong = await Client.PostAsJsonAsync("/api/Auth/login", new { soDienThoai = phone, matKhau = "wrong" });
        wrong.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var weak = await Client.PostAsJsonAsync("/api/Auth/register", new
        {
            soDienThoai = "0777" + Random.Shared.Next(100000, 999999).ToString("D6"),
            matKhau = "123"
        });
        weak.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var badPhone = await Client.PostAsJsonAsync("/api/Auth/register", new
        {
            soDienThoai = "077712345678", matKhau = "Test@123456"
        });
        badPhone.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var login = await Client.PostAsJsonAsync("/api/Auth/login", new { soDienThoai = phone, matKhau = "Test@123456" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = new JwtSecurityTokenHandler().ReadJwtToken(body.GetProperty("token").GetString());
        token.Claims.Single(x => x.Type == "MaUser").Value.Should().Be(body.GetProperty("maUser").GetString());
        token.Claims.Single(x => x.Type == "MaVaiTro").Value.Should().NotBeNullOrWhiteSpace();
        token.Claims.Single(x => x.Type == "role").Value.Should().Be("KhachHang");

        var refresh = await Client.PostAsJsonAsync("/api/Auth/refresh", new
        {
            refreshToken = body.GetProperty("refreshToken").GetString()
        });
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await refresh.Content.ReadFromJsonAsync<JsonElement>();
        refreshed.GetProperty("token").GetString().Should().NotBe(body.GetProperty("token").GetString());

        var reused = await Client.PostAsJsonAsync("/api/Auth/refresh", new
        {
            refreshToken = body.GetProperty("refreshToken").GetString()
        });
        reused.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", refreshed.GetProperty("token").GetString());
        var logout = await Client.PostAsJsonAsync("/api/Auth/logout", new
        {
            refreshToken = refreshed.GetProperty("refreshToken").GetString()
        });
        logout.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
