using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TourDuLich.Application.Services;
using TourDuLich.Infrastructure;
using Xunit.Sdk;

namespace TourDuLich.IntegrationTests;

public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly Action<IServiceCollection>? _configureServices;

    public TestApiFactory(Action<IServiceCollection>? configureServices = null)
    {
        _configureServices = configureServices;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        // Tests must not inherit the Windows Event Log provider: CI and local
        // non-admin sessions cannot write there, which masks the real result.
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var connection = TestSettings.ConnectionString;
            if (!string.IsNullOrWhiteSpace(connection))
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connection
                });
            }
        });
        builder.ConfigureServices(services => _configureServices?.Invoke(services));
    }
}

public static class TestSettings
{
    private static readonly Lazy<string?> Connection = new(LoadConnectionString);

    public static string? ConnectionString => Connection.Value;
    public static string AdminPhone => Environment.GetEnvironmentVariable("TOURDULICH_TEST_ADMIN_PHONE") ?? "0900000001";
    public static string AdminPassword => Environment.GetEnvironmentVariable("TOURDULICH_TEST_ADMIN_PASSWORD") ?? "Test@123456";
    public static string SalePhone => Environment.GetEnvironmentVariable("TOURDULICH_TEST_SALE_PHONE") ?? "0900000002";
    public static string SalePassword => Environment.GetEnvironmentVariable("TOURDULICH_TEST_SALE_PASSWORD") ?? "Test@123456";

    private static string? LoadConnectionString()
    {
        var configured = Environment.GetEnvironmentVariable("TOURDULICH_TEST_CONNECTION");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../TourDuLich.API/appsettings.Development.json"));
        if (!File.Exists(path))
            return null;

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("ConnectionStrings")
            .GetProperty("DefaultConnection").GetString();
    }
}

public abstract class ApiTestBase : IAsyncLifetime
{
    protected TestApiFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;

    public virtual Task InitializeAsync()
    {
        Factory = CreateFactory();
        Client = Factory.CreateClient();
        return Task.CompletedTask;
    }

    protected virtual TestApiFactory CreateFactory() => new();

    public virtual async Task DisposeAsync()
    {
        Client.Dispose();
        await CleanupAsync();
        Factory.Dispose();
    }

    protected async Task<AuthResult> RegisterAsync(string? phone = null)
    {
        phone ??= "0777" + Random.Shared.Next(10000000, 99999999);
        var response = await Client.PostAsJsonAsync("/api/Auth/register", new
        {
            soDienThoai = phone,
            matKhau = "Test@123456",
            maVaiTro = 1
        });
        response.EnsureSuccessStatusCode();
        var register = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new AuthResult(
            register.GetProperty("maUser").GetString()!,
            phone,
            register.GetProperty("token").GetString()!);
    }

    protected async Task<AuthResult> LoginAsync(string phone, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/Auth/login", new
        {
            soDienThoai = phone,
            matKhau = password
        });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new AuthResult(login.GetProperty("maUser").GetString()!, phone, login.GetProperty("token").GetString()!);
    }

    protected async Task<AuthResult> LoginAdminAsync() => await LoginAsync(TestSettings.AdminPhone, TestSettings.AdminPassword);
    protected async Task<AuthResult> LoginSaleAsync() => await LoginAsync(TestSettings.SalePhone, TestSettings.SalePassword);

    protected void UseToken(AuthResult auth)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
    }

    protected static string Unique(string prefix) => (prefix + Guid.NewGuid().ToString("N"))[..20].ToUpperInvariant();

    protected async Task<string> CreateTourAsync(AuthResult sale, string status = "HoatDong", int capacity = 20)
    {
        UseToken(sale);
        var id = Unique("IT");
        var response = await Client.PostAsJsonAsync("/api/Tour", new
        {
            maTour = id, tenTour = "Integration test tour", giaTour = 100000,
            slkhach = capacity, loaiTour = "Chuan", trangThai = status
        });
        response.EnsureSuccessStatusCode();
        return id;
    }

    protected async Task<string> CreateDepartureAsync(AuthResult sale, string tour, DateTime start)
    {
        UseToken(sale);
        var id = Unique("IK");
        var response = await Client.PostAsJsonAsync("/api/LichKhoiHanh", new
        {
            maKhoiHanh = id, maTour = tour, ngayKhoiHanh = start,
            ngayKetThuc = start.AddDays(2), diaDiem = "Integration test"
        });
        response.EnsureSuccessStatusCode();
        return id;
    }

    protected async Task CleanupAsync()
    {
        if (string.IsNullOrWhiteSpace(TestSettings.ConnectionString))
            return;

        using var scope = ((IServiceScopeFactory)Factory.Services
            .GetService(typeof(IServiceScopeFactory))!).CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.ExecuteSqlRawAsync("""
            DELETE FROM dbo.ThanhToan WHERE MaBooking IN (SELECT MaBooking FROM dbo.DatDichVu WHERE MaUser IN (SELECT MaUser FROM dbo.NguoiSuDung WHERE SoDienThoai LIKE N'0777%'));
            DELETE FROM dbo.HopDong WHERE MaBooking IN (SELECT MaBooking FROM dbo.DatDichVu WHERE MaUser IN (SELECT MaUser FROM dbo.NguoiSuDung WHERE SoDienThoai LIKE N'0777%'));
            DELETE FROM dbo.MediaDanhGiaTour WHERE MaDanhGiaTour IN (SELECT MaDanhGiaTour FROM dbo.DanhGiaTour WHERE MaUser IN (SELECT MaUser FROM dbo.NguoiSuDung WHERE SoDienThoai LIKE N'0777%'));
            DELETE FROM dbo.MediaDanhGiaHdv WHERE MaDanhGiaHdv IN (SELECT MaDanhGiaHdv FROM dbo.DanhGiaHDV WHERE MaUser IN (SELECT MaUser FROM dbo.NguoiSuDung WHERE SoDienThoai LIKE N'0777%'));
            DELETE FROM dbo.MediaDanhGiaSanPham WHERE MaDanhGia IN (SELECT MaDanhGia FROM dbo.DanhGiaSanPhamDoiTac WHERE MaUser IN (SELECT MaUser FROM dbo.NguoiSuDung WHERE SoDienThoai LIKE N'0777%'));
            DELETE FROM dbo.DatDichVu_KhuyenMai WHERE MaBooking IN (SELECT MaBooking FROM dbo.DatDichVu WHERE MaUser IN (SELECT MaUser FROM dbo.NguoiSuDung WHERE SoDienThoai LIKE N'0777%'));
            DELETE FROM dbo.DanhGiaTour WHERE MaUser IN (SELECT MaUser FROM dbo.NguoiSuDung WHERE SoDienThoai LIKE N'0777%');
            DELETE FROM dbo.DanhGiaHDV WHERE MaUser IN (SELECT MaUser FROM dbo.NguoiSuDung WHERE SoDienThoai LIKE N'0777%');
            DELETE FROM dbo.DanhGiaSanPhamDoiTac WHERE MaUser IN (SELECT MaUser FROM dbo.NguoiSuDung WHERE SoDienThoai LIKE N'0777%');
            DELETE FROM dbo.HanhViKhachHang WHERE MaUser IN (SELECT MaUser FROM dbo.NguoiSuDung WHERE SoDienThoai LIKE N'0777%');
            DELETE FROM dbo.DatDichVu WHERE MaUser IN (SELECT MaUser FROM dbo.NguoiSuDung WHERE SoDienThoai LIKE N'0777%');
            DELETE FROM dbo.GiayTo WHERE MaKhachHang IN (SELECT MaKhachHang FROM dbo.KhachHang WHERE MaUser IN (SELECT MaUser FROM dbo.NguoiSuDung WHERE SoDienThoai LIKE N'0777%'));
            DELETE FROM dbo.KhachHang WHERE MaUser IN (SELECT MaUser FROM dbo.NguoiSuDung WHERE SoDienThoai LIKE N'0777%');
            DELETE FROM dbo.YeuCauThietKe WHERE MaUser IN (SELECT MaUser FROM dbo.NguoiSuDung WHERE SoDienThoai LIKE N'0777%');
            DELETE FROM dbo.AIGoiY WHERE MaUser IN (SELECT MaUser FROM dbo.NguoiSuDung WHERE SoDienThoai LIKE N'0777%');
            DELETE FROM dbo.LichTrinh WHERE MaTour LIKE N'IT%';
            DELETE FROM dbo.LichKhoiHanh WHERE MaTour LIKE N'IT%';
            DELETE FROM dbo.Tour WHERE MaTour LIKE N'IT%';
            DELETE FROM dbo.NguoiSuDung WHERE SoDienThoai LIKE N'0777%';
            """);
    }

    protected static void SkipIfNoConnection()
    {
        if (string.IsNullOrWhiteSpace(TestSettings.ConnectionString))
            throw SkipException.ForSkip("Chưa cấu hình TOURDULICH_TEST_CONNECTION và không tìm thấy appsettings.Development.json.");
    }
}

public sealed record AuthResult(string MaUser, string Phone, string Token);
