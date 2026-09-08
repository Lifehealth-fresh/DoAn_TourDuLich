using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace TourDuLich.IntegrationTests;

public sealed class PromotionTests : ApiTestBase
{
    [Fact]
    public async Task ValidPromotionReducesTotalAndDuplicateReturnsConflict()
    {
        var sale = await LoginSaleAsync();
        var tour = await CreateTourAsync(sale, capacity: 20);
        var departure = await CreateDepartureAsync(sale, tour, DateTime.UtcNow.AddDays(20));
        var customer = await RegisterAsync();
        UseToken(customer);
        var bookingResponse = await Client.PostAsJsonAsync("/api/DatDichVu", new { maTour = tour, maKhoiHanh = departure, slnguoiLon = 10, sltreEm = 0 });
        var booking = await bookingResponse.Content.ReadFromJsonAsync<JsonElement>();
        var bookingId = booking.GetProperty("maBooking").GetString();

        var apply = await Client.PostAsJsonAsync("/api/KhuyenMai/ap-dung", new { maBooking = bookingId, maCode = "SUMMER001" });
        if (apply.StatusCode == HttpStatusCode.BadRequest)
            throw Xunit.Sdk.SkipException.ForSkip("Database chưa seed mã SUMMER001 hoặc điều kiện khuyến mãi tương ứng.");
        apply.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await apply.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("soTienGiam").GetInt32().Should().BeGreaterThan(0);
        result.GetProperty("thanhTienMoi").GetInt32().Should().BeLessThan(booking.GetProperty("thanhTien").GetInt32());
        (await Client.PostAsJsonAsync("/api/KhuyenMai/ap-dung", new { maBooking = bookingId, maCode = "SUMMER001" })).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
