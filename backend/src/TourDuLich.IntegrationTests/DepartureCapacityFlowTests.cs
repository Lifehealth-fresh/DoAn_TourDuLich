using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.IntegrationTests;

// The existing API fixture deletes test-shaped records on cleanup. Never point it at application data.
public abstract class IsolatedApiTestBase : ApiTestBase
{
    private bool initialized;
    public override async Task InitializeAsync()
    {
        var connection = Environment.GetEnvironmentVariable("TOURDULICH_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection) ||
            !new SqlConnectionStringBuilder(connection).InitialCatalog.StartsWith("TourDuLich_Test_", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Set TOURDULICH_TEST_CONNECTION to a separately seeded TourDuLich_Test_* database. Application databases are forbidden.");
        await base.InitializeAsync();
        initialized = true;
    }
    public override Task DisposeAsync() => initialized ? base.DisposeAsync() : Task.CompletedTask;
}

public sealed class DepartureCapacityFlowTests : IsolatedApiTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TenSeats_FourPlusFourPlusTwo_CancelReleasesFour_AndProfilesIncludePapers(bool paid)
    {
        var sale = await LoginSaleAsync();
        var tour = await CreateTourAsync(sale, capacity: 30);
        var departure = await Departure(sale, tour, 10);
        var users = new[] { await RegisterAsync(), await RegisterAsync(), await RegisterAsync() };
        var bookings = new List<string>();
        for (var index = 0; index < users.Length; index++)
        {
            UseToken(users[index]);
            var result = await Client.PostAsJsonAsync("/api/DatDichVu", new { maTour = tour, maKhoiHanh = departure,
                slnguoiLon = index == 2 ? 1 : 3, sltreEm = 1 });
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
            bookings.Add((await result.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("maBooking").GetString()!);
        }
        UseToken(users[0]);
        var excess = await Client.PostAsJsonAsync("/api/DatDichVu", new { maTour = tour, maKhoiHanh = departure, slnguoiLon = 1, sltreEm = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, excess.StatusCode);
        Assert.Equal("Lịch khởi hành không đủ chỗ trống.", (await excess.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
        UseToken(sale);
        var full = await Client.GetFromJsonAsync<JsonElement>($"/api/LichKhoiHanh/{departure}/khach");
        Assert.Equal(10, full.GetProperty("sucChua").GetInt32());
        Assert.Equal(10, full.GetProperty("daDat").GetInt64());
        Assert.Equal(0, full.GetProperty("conTrong").GetInt64());
        Assert.Equal(3, full.GetProperty("soTaiKhoan").GetInt32());
        Assert.Equal(3, full.GetProperty("bookings").GetArrayLength());
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PutAsJsonAsync($"/api/LichKhoiHanh/{departure}",
            new { soCho = 9, ngayKhoiHanh = DateTime.UtcNow.AddDays(30) })).StatusCode);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var profile = new KhachHang { MaKhachHang = Unique("PK"), MaUser = FixedLengthHelper.PadTo20(users[1].MaUser),
                Ho = "Khách", Ten = "Kiểm thử", SoDienThoai = users[1].Phone, Email = "test@example.invalid",
                NgaySinh = new DateOnly(1990, 1, 1), QuocTich = "Việt Nam" };
            db.KhachHangs.Add(profile);
            db.GiayTos.Add(new GiayTo { MaGiayTo = Unique("PG"), MaKhachHang = profile.MaKhachHang,
                LoaiGiayTo = "CCCD", SoTrenGiayTo = Guid.NewGuid().ToString("N")[..12], NgayCap = new(2020, 1, 1),
                NgayHetHan = new(2030, 1, 1), NoiCap = "Dữ liệu kiểm thử" });
            var key = FixedLengthHelper.PadTo20(bookings[1]);
            (await db.DatDichVus.FirstAsync(b => b.MaBooking == key)).MaKhachHang = profile.MaKhachHang;
            if (paid) db.ThanhToans.Add(new ThanhToan { MaTt = Unique("PT"), MaBooking = FixedLengthHelper.PadTo20(bookings[0]),
                SoTien = 100000, TrangThai = FixedLengthHelper.PadTo20("DaXacNhan"), PhuongThuc = FixedLengthHelper.PadTo20("TienMat") });
            await db.SaveChangesAsync();
        }
        UseToken(users[0]);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client.GetAsync($"/api/LichKhoiHanh/{departure}/khach")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client.GetAsync($"/api/DatDichVu/{bookings[1]}/ho-so-khach")).StatusCode);
        var cancel = await Client.PutAsync($"/api/DatDichVu/{bookings[0]}/huy", null);
        cancel.EnsureSuccessStatusCode();
        Assert.Equal(paid ? "ChoHoanTien" : "DaHuy", (await cancel.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("trangThai").GetString());
        UseToken(sale);
        var after = await Client.GetFromJsonAsync<JsonElement>($"/api/LichKhoiHanh/{departure}/khach");
        Assert.Equal(6, after.GetProperty("daDat").GetInt64()); Assert.Equal(4, after.GetProperty("conTrong").GetInt64());
        Assert.Equal(2, after.GetProperty("soTaiKhoan").GetInt32()); Assert.Equal(2, after.GetProperty("bookings").GetArrayLength());
        Assert.DoesNotContain(after.GetProperty("bookings").EnumerateArray(), b => b.GetProperty("maBooking").GetString() == bookings[0]);
        var profileJson = await Client.GetFromJsonAsync<JsonElement>($"/api/DatDichVu/{bookings[1]}/ho-so-khach");
        Assert.Equal("Kiểm thử", profileJson.GetProperty("ten").GetString());
        Assert.Equal("CCCD", profileJson.GetProperty("giayTo")[0].GetProperty("loaiGiayTo").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync("/api/DatDichVu/NOT_FOUND/ho-so-khach")).StatusCode);
    }

    [Fact]
    public async Task ConcurrentLastSeat_OnlyOneSucceeds_AndAnotherDepartureIsIndependent()
    {
        var sale = await LoginSaleAsync(); var tour = await CreateTourAsync(sale, capacity: 40);
        var first = await Departure(sale, tour, 1); var second = await Departure(sale, tour, 1);
        var customers = new[] { await RegisterAsync(), await RegisterAsync() };
        async Task<HttpResponseMessage> Book(AuthResult customer, string departure)
        {
            using var client = Factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new("Bearer", customer.Token);
            return await client.PostAsJsonAsync("/api/DatDichVu", new { maTour = tour, maKhoiHanh = departure, slnguoiLon = 1, sltreEm = 0 });
        }
        var results = await Task.WhenAll(Book(customers[0], first), Book(customers[1], first));
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.BadRequest);
        Assert.Equal(HttpStatusCode.Created, (await Book(customers[0], second)).StatusCode);
    }

    private async Task<string> Departure(AuthResult sale, string tour, int capacity)
    {
        UseToken(sale);
        var id = Unique("IK"); var date = DateTime.UtcNow.AddDays(30);
        var result = await Client.PostAsJsonAsync("/api/LichKhoiHanh", new { maKhoiHanh = id, maTour = tour,
            ngayKhoiHanh = date, ngayKetThuc = date.AddDays(2), soCho = capacity });
        result.EnsureSuccessStatusCode();
        return id;
    }
}

