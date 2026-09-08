using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace TourDuLich.IntegrationTests;

public sealed class OwnershipTests : ApiTestBase
{
    [Fact]
    public async Task CustomerCannotReadUpdateOrDeleteAnotherCustomerProfile()
    {
        var a = await RegisterAsync();
        UseToken(a);
        var create = await Client.PostAsJsonAsync("/api/KhachHang", new { ho = "A", ten = "Owner", email = "a@test.local" });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var profile = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = profile.GetProperty("maKhachHang").GetString();
        (await Client.PostAsJsonAsync($"/api/KhachHang/{id}/giay-to", new { loaiGiayTo = "CCCD", soTrenGiayTo = "IT-001", ngayCap = "2020-01-01", ngayHetHan = "2035-01-01", noiCap = "Test" })).StatusCode.Should().Be(HttpStatusCode.Created);

        UseToken(await RegisterAsync());
        (await Client.GetAsync($"/api/KhachHang/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var update = new { ho = "B", ten = "NotOwner" };
        (await Client.PutAsJsonAsync($"/api/KhachHang/{id}", update)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Client.DeleteAsync($"/api/KhachHang/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CustomerCannotReadAnotherCustomerBooking()
    {
        var a = await RegisterAsync();
        var sale = await LoginSaleAsync();
        var tour = await CreateTourAsync(sale);
        var departure = await CreateDepartureAsync(sale, tour, DateTime.UtcNow.AddDays(20));
        UseToken(a);
        var bookingResponse = await Client.PostAsJsonAsync("/api/DatDichVu", new { maTour = tour, maKhoiHanh = departure, slnguoiLon = 1, sltreEm = 0 });
        bookingResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var booking = await bookingResponse.Content.ReadFromJsonAsync<JsonElement>();
        var bookingId = booking.GetProperty("maBooking").GetString();

        UseToken(await RegisterAsync());
        (await Client.GetAsync($"/api/DatDichVu/{bookingId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SaleCanOnlyReadOwnWishlist()
    {
        UseToken(await LoginSaleAsync());
        var response = await Client.GetAsync("/api/DanhSachYeuThich/cua-toi");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<JsonElement>();
        items.GetArrayLength().Should().Be(0);
    }
}
