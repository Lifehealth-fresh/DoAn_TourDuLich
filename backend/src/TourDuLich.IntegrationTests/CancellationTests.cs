using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace TourDuLich.IntegrationTests;

public sealed class CancellationTests : ApiTestBase
{
    private async Task<(AuthResult Customer, AuthResult Sale, string Booking, int Total)> CreateBookingAsync(double daysUntilDeparture)
    {
        var sale = await LoginSaleAsync();
        var tour = await CreateTourAsync(sale, capacity: 20);
        var departure = await CreateDepartureAsync(sale, tour, DateTime.UtcNow.AddDays(daysUntilDeparture));
        var customer = await RegisterAsync();
        UseToken(customer);
        var response = await Client.PostAsJsonAsync("/api/DatDichVu", new
        {
            maTour = tour, maKhoiHanh = departure, slnguoiLon = 1, sltreEm = 0
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (customer, sale, body.GetProperty("maBooking").GetString()!, body.GetProperty("thanhTien").GetInt32());
    }

    [Theory]
    [InlineData(15, 0)]
    [InlineData(9, 30)]
    [InlineData(6, 50)]
    [InlineData(3, 70)]
    [InlineData(0.5, 100)]
    public async Task CancellationCalculatesRateFromDepartureNotice(double days, int expectedRate)
    {
        var data = await CreateBookingAsync(days);
        UseToken(data.Customer);
        var response = await Client.PutAsync($"/api/DatDichVu/{data.Booking}/huy", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("tyLePhatHuy").GetInt32().Should().Be(expectedRate);
    }

    [Fact]
    public async Task CancellationPenaltyCannotExceedAmountCollected()
    {
        var data = await CreateBookingAsync(3);
        UseToken(data.Customer);
        var payment = await Client.PostAsJsonAsync("/api/ThanhToan", new
        {
            maBooking = data.Booking, soTien = 1000,
            phuongThuc = "Test", loaiThanhToan = "DatCoc"
        });
        payment.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await Client.PutAsync($"/api/DatDichVu/{data.Booking}/huy", null);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("soTienPhatHuy").GetInt32().Should().Be(1000);
    }

    [Fact]
    public async Task DaThanhToanBookingCanStillBeCancelled()
    {
        var data = await CreateBookingAsync(15);
        UseToken(data.Sale);
        (await Client.PutAsJsonAsync($"/api/DatDichVu/{data.Booking}/trang-thai", new { trangThai = "DaXacNhan" })).StatusCode.Should().Be(HttpStatusCode.OK);
        UseToken(data.Customer);
        (await Client.PostAsJsonAsync("/api/ThanhToan", new
        {
            maBooking = data.Booking, soTien = data.Total,
            phuongThuc = "Test", loaiThanhToan = "ThanhToanDu"
        })).StatusCode.Should().Be(HttpStatusCode.Created);
        UseToken(data.Sale);
        (await Client.PutAsJsonAsync($"/api/DatDichVu/{data.Booking}/trang-thai", new { trangThai = "DaThanhToan" })).StatusCode.Should().Be(HttpStatusCode.OK);
        UseToken(data.Customer);
        var response = await Client.PutAsync($"/api/DatDichVu/{data.Booking}/huy", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CompletedBookingCannotBeCancelled()
    {
        var data = await CreateBookingAsync(15);
        UseToken(data.Sale);
        (await Client.PutAsJsonAsync($"/api/DatDichVu/{data.Booking}/trang-thai", new { trangThai = "DaXacNhan" })).StatusCode.Should().Be(HttpStatusCode.OK);
        UseToken(data.Customer);
        (await Client.PostAsJsonAsync("/api/ThanhToan", new
        {
            maBooking = data.Booking, soTien = data.Total,
            phuongThuc = "Test", loaiThanhToan = "ThanhToanDu"
        })).StatusCode.Should().Be(HttpStatusCode.Created);
        UseToken(data.Sale);
        (await Client.PutAsJsonAsync($"/api/DatDichVu/{data.Booking}/trang-thai", new { trangThai = "DaThanhToan" })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await Client.PutAsJsonAsync($"/api/DatDichVu/{data.Booking}/trang-thai", new { trangThai = "HoanThanh" })).StatusCode.Should().Be(HttpStatusCode.OK);
        UseToken(data.Customer);
        var response = await Client.PutAsync($"/api/DatDichVu/{data.Booking}/huy", null);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
