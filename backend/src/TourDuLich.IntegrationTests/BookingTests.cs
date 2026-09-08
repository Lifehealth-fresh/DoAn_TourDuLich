using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace TourDuLich.IntegrationTests;

public sealed class BookingTests : ApiTestBase
{
    [Fact]
    public async Task BookingRejectsPastDepartureAndInactiveTour()
    {
        var sale = await LoginSaleAsync();
        var pastTour = await CreateTourAsync(sale);
        var past = await CreateDepartureAsync(sale, pastTour, DateTime.UtcNow.AddDays(-2));
        UseToken(await RegisterAsync());
        (await Client.PostAsJsonAsync("/api/DatDichVu", new { maTour = pastTour, maKhoiHanh = past, slnguoiLon = 1, sltreEm = 0 })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var inactiveTour = await CreateTourAsync(sale, "An");
        var future = await CreateDepartureAsync(sale, inactiveTour, DateTime.UtcNow.AddDays(10));
        (await Client.PostAsJsonAsync("/api/DatDichVu", new { maTour = inactiveTour, maKhoiHanh = future, slnguoiLon = 1, sltreEm = 0 })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BookingRejectsWhenDepartureCapacityIsExceeded()
    {
        var sale = await LoginSaleAsync();
        var tour = await CreateTourAsync(sale, capacity: 2);
        var departure = await CreateDepartureAsync(sale, tour, DateTime.UtcNow.AddDays(10));
        UseToken(await RegisterAsync());
        (await Client.PostAsJsonAsync("/api/DatDichVu", new { maTour = tour, maKhoiHanh = departure, slnguoiLon = 2, sltreEm = 0 })).StatusCode.Should().Be(HttpStatusCode.Created);
        (await Client.PostAsJsonAsync("/api/DatDichVu", new { maTour = tour, maKhoiHanh = departure, slnguoiLon = 1, sltreEm = 0 })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaleCanMoveBookingOnlyThroughValidStates()
    {
        var sale = await LoginSaleAsync();
        var tour = await CreateTourAsync(sale);
        var departure = await CreateDepartureAsync(sale, tour, DateTime.UtcNow.AddDays(10));
        var customer = await RegisterAsync();
        UseToken(customer);
        var bookingResponse = await Client.PostAsJsonAsync("/api/DatDichVu", new { maTour = tour, maKhoiHanh = departure, slnguoiLon = 1, sltreEm = 0 });
        var booking = await bookingResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = booking.GetProperty("maBooking").GetString();
        UseToken(sale);
        (await Client.PutAsJsonAsync($"/api/DatDichVu/{id}/trang-thai", new { trangThai = "DaXacNhan" })).StatusCode.Should().Be(HttpStatusCode.OK);
        UseToken(customer);
        var payment = await Client.PostAsJsonAsync("/api/ThanhToan", new
        {
            maBooking = id,
            soTien = booking.GetProperty("thanhTien").GetInt32(),
            phuongThuc = "Test",
            loaiThanhToan = "ThanhToanDu"
        });
        payment.StatusCode.Should().Be(HttpStatusCode.Created);
        UseToken(sale);
        foreach (var status in new[] { "DaThanhToan", "HoanThanh" })
            (await Client.PutAsJsonAsync($"/api/DatDichVu/{id}/trang-thai", new { trangThai = status })).StatusCode.Should().Be(HttpStatusCode.OK);

        var secondCustomer = await RegisterAsync();
        UseToken(secondCustomer);
        var secondBookingResponse = await Client.PostAsJsonAsync("/api/DatDichVu", new { maTour = tour, maKhoiHanh = departure, slnguoiLon = 1, sltreEm = 0 });
        var secondBooking = await secondBookingResponse.Content.ReadFromJsonAsync<JsonElement>();
        UseToken(sale);
        (await Client.PutAsJsonAsync($"/api/DatDichVu/{secondBooking.GetProperty("maBooking").GetString()}/trang-thai", new { trangThai = "HoanThanh" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
