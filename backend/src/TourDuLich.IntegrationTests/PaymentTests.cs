using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace TourDuLich.IntegrationTests;

public sealed class PaymentTests : ApiTestBase
{
    private async Task<(AuthResult Customer, AuthResult Sale, string Booking)> CreateBookingAsync()
    {
        var sale = await LoginSaleAsync();
        var tour = await CreateTourAsync(sale);
        var departure = await CreateDepartureAsync(sale, tour, DateTime.UtcNow.AddDays(20));
        var customer = await RegisterAsync();
        UseToken(customer);
        var response = await Client.PostAsJsonAsync("/api/DatDichVu", new { maTour = tour, maKhoiHanh = departure, slnguoiLon = 1, sltreEm = 0 });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (customer, sale, body.GetProperty("maBooking").GetString()!);
    }

    [Fact]
    public async Task CancelledBookingCannotBePaid()
    {
        var data = await CreateBookingAsync();
        UseToken(data.Customer);
        (await Client.PutAsync($"/api/DatDichVu/{data.Booking}/huy", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await Client.PostAsJsonAsync("/api/ThanhToan", new { maBooking = data.Booking, soTien = 1, phuongThuc = "Test", loaiThanhToan = "ThanhToanDu" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PaymentCannotExceedRemainingAndSaleCannotReadDetails()
    {
        var data = await CreateBookingAsync();
        UseToken(data.Customer);
        (await Client.PostAsJsonAsync("/api/ThanhToan", new { maBooking = data.Booking, soTien = 999999999, phuongThuc = "Test", loaiThanhToan = "ThanhToanDu" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        UseToken(data.Sale);
        (await Client.GetAsync($"/api/ThanhToan/theo-booking/{data.Booking}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var summary = await Client.GetAsync($"/api/ThanhToan/theo-booking/{data.Booking}/tong-hop");
        summary.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await summary.Content.ReadFromJsonAsync<JsonElement>();
        json.TryGetProperty("tongTien", out _).Should().BeTrue();
        json.TryGetProperty("daThanhToan", out _).Should().BeTrue();
        json.TryGetProperty("conLai", out _).Should().BeTrue();
    }

    [Fact]
    public async Task SameIdempotencyKey_ReturnsOriginalPaymentWithoutCreatingAnotherRow()
    {
        var data = await CreateBookingAsync();
        UseToken(data.Customer);
        const string idempotencyKey = "payment-retry-integration-test-001";

        async Task<HttpResponseMessage> SendAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/ThanhToan")
            {
                Content = JsonContent.Create(new
                {
                    maBooking = data.Booking,
                    soTien = 10_000,
                    phuongThuc = "ChuyenKhoan",
                    loaiThanhToan = "DatCoc"
                })
            };
            request.Headers.Add("Idempotency-Key", idempotencyKey);
            return await Client.SendAsync(request);
        }

        var first = await SendAsync();
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstBody = await first.Content.ReadFromJsonAsync<JsonElement>();

        var retry = await SendAsync();
        retry.StatusCode.Should().Be(HttpStatusCode.OK);
        var retryBody = await retry.Content.ReadFromJsonAsync<JsonElement>();
        retryBody.GetProperty("maTt").GetString()
            .Should().Be(firstBody.GetProperty("maTt").GetString());

        var payments = await Client.GetFromJsonAsync<JsonElement>(
            $"/api/ThanhToan/theo-booking/{data.Booking}");
        payments.GetArrayLength().Should().Be(1);
    }
}
