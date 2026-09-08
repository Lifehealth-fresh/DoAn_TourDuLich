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
        var phone = "0777" + Random.Shared.Next(10000000, 99999999);
        var register = await Client.PostAsJsonAsync("/api/Auth/register", new
        {
            soDienThoai = phone, matKhau = "Test@123456", maVaiTro = 2
        });
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        var wrong = await Client.PostAsJsonAsync("/api/Auth/login", new { soDienThoai = phone, matKhau = "wrong" });
        wrong.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var login = await Client.PostAsJsonAsync("/api/Auth/login", new { soDienThoai = phone, matKhau = "Test@123456" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = new JwtSecurityTokenHandler().ReadJwtToken(body.GetProperty("token").GetString());
        token.Claims.Single(x => x.Type == "MaUser").Value.Should().Be(body.GetProperty("maUser").GetString());
        token.Claims.Single(x => x.Type == "MaVaiTro").Value.Should().NotBeNullOrWhiteSpace();
        token.Claims.Single(x => x.Type == "role").Value.Should().Be("KhachHang");
    }
}
