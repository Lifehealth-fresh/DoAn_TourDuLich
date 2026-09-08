using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace TourDuLich.IntegrationTests;

public sealed class SelfDesignedTourPrivacyTests : ApiTestBase
{
    [Fact]
    public async Task SelfDesignedTourIsVisibleOnlyToOwner()
    {
        var owner = await RegisterAsync();
        UseToken(owner);
        var requestResponse = await Client.PostAsJsonAsync("/api/YeuCauThietKe", new
        {
            diemDenMongMuon = "Hà Nội", soNgay = 2, soNguoiLon = 1, soTreEm = 0
        });
        requestResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var request = await requestResponse.Content.ReadFromJsonAsync<JsonElement>();
        var requestId = request.GetProperty("maYeuCau").GetString();
        var tourResponse = await Client.PostAsJsonAsync("/api/Tour/tu-thiet-ke", new { maYeuCau = requestId });
        tourResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var tour = await tourResponse.Content.ReadFromJsonAsync<JsonElement>();
        var tourId = tour.GetProperty("maTour").GetString();

        UseToken(await RegisterAsync());
        (await Client.GetAsync($"/api/Tour/{tourId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var list = await Client.GetAsync("/api/Tour?loaiTour=TuThietKe");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        (await list.Content.ReadAsStringAsync()).Should().NotContain(tourId);

        UseToken(owner);
        (await Client.GetAsync($"/api/Tour/{tourId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        var ownerList = await Client.GetAsync("/api/Tour?loaiTour=TuThietKe");
        (await ownerList.Content.ReadAsStringAsync()).Should().Contain(tourId);
    }
}
