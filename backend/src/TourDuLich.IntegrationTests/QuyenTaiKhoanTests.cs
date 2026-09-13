using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace TourDuLich.IntegrationTests;

public sealed class QuyenTaiKhoanTests : ApiTestBase
{
    [Fact]
    public async Task Login_Staff_ReturnsPermissionMatrix()
    {
        SkipIfNoConnection();
        var login = await Client.PostAsJsonAsync("/api/Auth/login", new
        {
            soDienThoai = TestSettings.AdminPhone,
            matKhau = TestSettings.AdminPassword
        });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("tenVaiTro").GetString().Should().Be("Admin");
        var quyen = body.GetProperty("quyen");
        quyen.GetArrayLength().Should().BeGreaterThanOrEqualTo(8);
        quyen.EnumerateArray().Should().Contain(item =>
            item.GetProperty("chucNang").GetString() == "Tour"
            && item.GetProperty("toanQuyen").GetBoolean());
    }

    [Fact]
    public async Task StaffWithoutTourThem_IsForbiddenToCreateTour()
    {
        SkipIfNoConnection();
        var staff = await CreateStaffAsync(TourGrant(them: false));
        UseToken(staff);

        var response = await Client.PostAsJsonAsync("/api/Tour", new
        {
            maTour = Unique("IT"),
            tenTour = "Bị hạn chế thêm",
            giaTour = 100000,
            slkhach = 10,
            loaiTour = "Chuan",
            trangThai = "HoatDong"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should()
            .Be("Bạn bị hạn chế quyền: không được thêm tour.");
    }

    [Fact]
    public async Task StaffWithTourThem_CanCreateTour()
    {
        SkipIfNoConnection();
        var staff = await CreateStaffAsync(TourGrant(them: true));
        UseToken(staff);

        var response = await Client.PostAsJsonAsync("/api/Tour", new
        {
            maTour = Unique("IT"),
            tenTour = "Được thêm tour",
            giaTour = 100000,
            slkhach = 10,
            loaiTour = "Chuan",
            trangThai = "HoatDong"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task StaffWithoutTourSuaOrXoa_CannotEditOrDelete()
    {
        SkipIfNoConnection();
        var admin = await LoginAdminAsync();
        var tour = await CreateTourAsync(admin);
        var staff = await CreateStaffAsync(TourGrant(them: true));
        UseToken(staff);

        var update = await Client.PutAsJsonAsync($"/api/Tour/{tour}", new
        {
            tenTour = "Không được sửa",
            giaTour = 1,
            slkhach = 10,
            loaiTour = "Chuan",
            trangThai = "HoatDong"
        });
        update.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await update.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("message").GetString().Should()
            .Be("Bạn bị hạn chế quyền: không được sửa tour.");

        var delete = await Client.DeleteAsync($"/api/Tour/{tour}");
        delete.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await delete.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("message").GetString().Should()
            .Be("Bạn bị hạn chế quyền: không được xóa tour.");
    }

    [Fact]
    public async Task EmptyCheckboxes_BlockEveryAdminModule()
    {
        SkipIfNoConnection();
        var staff = await CreateStaffAsync();
        UseToken(staff);

        (await Client.GetAsync("/api/BaoCao/tong-quan")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Client.GetAsync("/api/DatDichVu/danh-sach")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Client.GetAsync("/api/Admin/tai-khoan")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Client.PostAsJsonAsync("/api/KhuyenMai", new
        {
            tenKm = "BLOCK", maCode = "BLK1", ngayBd = DateTime.UtcNow, ngayKt = DateTime.UtcNow.AddDays(1),
            donVi = "%", giamGia = 10, trangThai = "HoatDong"
        })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_CannotStripOwnAccountPermission()
    {
        SkipIfNoConnection();
        var admin = await LoginAdminAsync();
        UseToken(admin);

        var response = await Client.PutAsJsonAsync($"/api/Admin/tai-khoan/{admin.MaUser}/quyen", new
        {
            quyen = new[]
            {
                new { chucNang = "Tour", them = true, sua = true, xoa = true, toanQuyen = true },
                new { chucNang = "TaiKhoan", them = false, sua = false, xoa = false, toanQuyen = false }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("message").GetString().Should()
            .Be("Không được gỡ quyền quản lý tài khoản của chính mình.");
    }

    [Fact]
    public async Task Admin_CreatesAccountWithSelectedGrants()
    {
        SkipIfNoConnection();
        UseToken(await LoginAdminAsync());
        var phone = "0777" + Random.Shared.Next(10000000, 99999999);
        var created = await Client.PostAsJsonAsync("/api/Admin/tai-khoan", new
        {
            soDienThoai = phone,
            matKhau = "Test@123456",
            tenVaiTro = "Sale",
            quyen = new[]
            {
                new { chucNang = "Tour", them = true, sua = false, xoa = false, toanQuyen = false },
                new { chucNang = "UuDai", them = false, sua = true, xoa = true, toanQuyen = false }
            }
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("tenVaiTro").GetString().Should().Be("Sale");
        Grant(body, "Tour").GetProperty("them").GetBoolean().Should().BeTrue();
        Grant(body, "Tour").GetProperty("sua").GetBoolean().Should().BeFalse();
        Grant(body, "UuDai").GetProperty("sua").GetBoolean().Should().BeTrue();
        Grant(body, "TaiKhoan").GetProperty("toanQuyen").GetBoolean().Should().BeFalse();
    }

    private async Task<AuthResult> CreateStaffAsync(params object[] grants)
    {
        UseToken(await LoginAdminAsync());
        var phone = "0777" + Random.Shared.Next(10000000, 99999999);
        var response = await Client.PostAsJsonAsync("/api/Admin/tai-khoan", new
        {
            soDienThoai = phone,
            matKhau = "Test@123456",
            tenVaiTro = "Sale",
            quyen = grants
        });
        response.EnsureSuccessStatusCode();
        return await LoginAsync(phone, "Test@123456");
    }

    private static object TourGrant(bool them = false, bool sua = false, bool xoa = false, bool toanQuyen = false) =>
        new { chucNang = "Tour", them, sua, xoa, toanQuyen };

    private static JsonElement Grant(JsonElement body, string chucNang) =>
        body.GetProperty("quyen").EnumerateArray()
            .Single(item => item.GetProperty("chucNang").GetString() == chucNang);
}
