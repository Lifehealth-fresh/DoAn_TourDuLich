using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace TourDuLich.IntegrationTests;

public sealed class HopDongTests : ApiTestBase
{
    [Fact]
    public async Task BookingCreatesContractWithSelectedProfileSnapshot()
    {
        var sale = await LoginSaleAsync();
        var tour = await CreateTourAsync(sale, capacity: 10);
        var departure = await CreateDepartureAsync(sale, tour, DateTime.UtcNow.AddDays(15));
        var customer = await RegisterAsync();
        UseToken(customer);

        var profileResponse = await Client.PostAsJsonAsync("/api/KhachHang", new
        {
            ho = "Nguyen", ten = "Contract", email = "contract@test.local"
        });
        var profile = await profileResponse.Content.ReadFromJsonAsync<JsonElement>();
        var profileId = profile.GetProperty("maKhachHang").GetString();
        await Client.PostAsJsonAsync($"/api/KhachHang/{profileId}/giay-to", new
        {
            loaiGiayTo = "CCCD", soTrenGiayTo = "IT-CONTRACT-001",
            ngayCap = "2020-01-01", ngayHetHan = "2035-01-01", noiCap = "Test"
        });

        var bookingResponse = await Client.PostAsJsonAsync("/api/DatDichVu", new
        {
            maTour = tour, maKhoiHanh = departure, maKhachHang = profileId,
            slnguoiLon = 1, sltreEm = 0
        });
        bookingResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var booking = await bookingResponse.Content.ReadFromJsonAsync<JsonElement>();
        var bookingId = booking.GetProperty("maBooking").GetString();
        booking.GetProperty("maHopDong").GetString().Should().NotBeNullOrWhiteSpace();

        var contractResponse = await Client.GetAsync($"/api/HopDong/theo-booking/{bookingId}");
        contractResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var contract = await contractResponse.Content.ReadFromJsonAsync<JsonElement>();
        contract.GetProperty("hoTenKhach").GetString().Should().Be("Nguyen Contract");
        contract.GetProperty("loaiGiayTo").GetString().Should().Be("CCCD");
        contract.GetProperty("soGiayTo").GetString().Should().Be("IT-CONTRACT-001");
        contract.GetProperty("ngayKy").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task CustomerCannotCreateOrSignContract_ButSaleCanSign()
    {
        var sale = await LoginSaleAsync();
        var tour = await CreateTourAsync(sale);
        var departure = await CreateDepartureAsync(sale, tour, DateTime.UtcNow.AddDays(15));
        var customer = await RegisterAsync();
        UseToken(customer);
        var bookingResponse = await Client.PostAsJsonAsync("/api/DatDichVu", new
        {
            maTour = tour, maKhoiHanh = departure, slnguoiLon = 1, sltreEm = 0
        });
        var booking = await bookingResponse.Content.ReadFromJsonAsync<JsonElement>();
        var bookingId = booking.GetProperty("maBooking").GetString();
        (await Client.PostAsJsonAsync("/api/HopDong", new { maBooking = bookingId })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var contract = await Client.GetAsync($"/api/HopDong/theo-booking/{bookingId}");
        var contractBody = await contract.Content.ReadFromJsonAsync<JsonElement>();
        var contractId = contractBody.GetProperty("maHopDong").GetString();
        (await Client.PutAsync($"/api/HopDong/{contractId}/ky", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        UseToken(sale);
        (await Client.GetAsync($"/api/HopDong/theo-booking/{bookingId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        var sign = await Client.PutAsync($"/api/HopDong/{contractId}/ky", null);
        sign.StatusCode.Should().Be(HttpStatusCode.OK);
        var signed = await sign.Content.ReadFromJsonAsync<JsonElement>();
        signed.GetProperty("trangThai").GetString().Should().Be("DaKy");
    }

    [Fact]
    public async Task BookingWithAnotherUsersProfileIsRejected()
    {
        var sale = await LoginSaleAsync();
        var tour = await CreateTourAsync(sale);
        var departure = await CreateDepartureAsync(sale, tour, DateTime.UtcNow.AddDays(15));
        var owner = await RegisterAsync();
        UseToken(owner);
        var profile = await Client.PostAsJsonAsync("/api/KhachHang", new { ho = "Owner", ten = "Profile" });
        var profileBody = await profile.Content.ReadFromJsonAsync<JsonElement>();
        var profileId = profileBody.GetProperty("maKhachHang").GetString();

        UseToken(await RegisterAsync());
        var booking = await Client.PostAsJsonAsync("/api/DatDichVu", new
        {
            maTour = tour, maKhoiHanh = departure, maKhachHang = profileId,
            slnguoiLon = 1, sltreEm = 0
        });
        booking.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
