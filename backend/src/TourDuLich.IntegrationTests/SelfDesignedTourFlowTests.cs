using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;
using TourDuLich.Application.Helpers;
using Xunit;

namespace TourDuLich.IntegrationTests;

public sealed class SelfDesignedTourFlowTests : IsolatedApiTestBase
{
    [Fact]
    public async Task Customer_CannotGenerateRejectOrEditSchedule()
    {
        SkipIfNoConnection();
        var customer = await RegisterAsync();
        UseToken(customer);
        var generate = await Client.PostAsJsonAsync("/api/YeuCauThietKe/UNKNOWN/sinh-de-xuat", new { });
        var reject = await Client.PutAsJsonAsync("/api/YeuCauThietKe/UNKNOWN/tu-choi-boi-sale", new { lyDoTuChoi = "test" });
        var edit = await Client.PutAsJsonAsync("/api/YeuCauThietKe/UNKNOWN/sua-lich-trinh", new { chiTiets = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Forbidden, generate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, reject.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, edit.StatusCode);
    }

    [Fact]
    public async Task SelfDesignedTour_FollowsProposalReviewAndApprovalFlow()
    {
        SkipIfNoConnection();
        var customer = await RegisterAsync();
        UseToken(customer);
        var createRequest = await Client.PostAsJsonAsync("/api/YeuCauThietKe", new
        {
            diemDenMongMuon = "Đà Nẵng",
            ngayDuKienDi = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            soNgay = 2,
            soNguoiLon = 2,
            soTreEm = 0,
            nganSachDuKien = 100000000,
            soThichGhiChu = "Biển và ẩm thực"
        });
        createRequest.EnsureSuccessStatusCode();
        var requestJson = await createRequest.Content.ReadFromJsonAsync<JsonElement>();
        var maYeuCau = requestJson.GetProperty("maYeuCau").GetString()!;

        // Khách có đề xuất ngay sau POST, không cần Sale sinh lần đầu.
        var automaticProposals = await Client.GetFromJsonAsync<JsonElement>($"/api/YeuCauThietKe/{maYeuCau}/de-xuat");
        Assert.True(automaticProposals.GetArrayLength() >= 2);

        var sale = await LoginSaleAsync();
        UseToken(sale);
        (await Client.GetAsync($"/api/YeuCauThietKe/{maYeuCau}/de-xuat")).EnsureSuccessStatusCode();
        var generate = await Client.PostAsJsonAsync($"/api/YeuCauThietKe/{maYeuCau}/sinh-de-xuat", new { });
        generate.EnsureSuccessStatusCode();
        var proposals = await generate.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(proposals.GetArrayLength() >= 2);
        foreach (var proposal in proposals.EnumerateArray())
            Assert.Contains("hạn chế", proposal.GetProperty("ghiChu").GetString());
        var maDeXuat = proposals[0].GetProperty("maDeXuat").GetString()!;

        UseToken(customer);
        var choose = await Client.PutAsync($"/api/YeuCauThietKe/{maYeuCau}/chon-de-xuat/{maDeXuat}", null);
        choose.EnsureSuccessStatusCode();
        var chooseJson = await choose.Content.ReadFromJsonAsync<JsonElement>();
        var maTour = chooseJson.GetProperty("maTour").GetString()!;
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PutAsync($"/api/YeuCauThietKe/{maYeuCau}/chon-de-xuat/{maDeXuat}", null)).StatusCode);
        var firstDetail = proposals[0].GetProperty("chiTiets")[0];
        var maDthamQuan = firstDetail.GetProperty("maDthamQuan").GetString();
        var maSanPham = firstDetail.GetProperty("maSanPham").GetString();

        UseToken(sale);
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PutAsync($"/api/YeuCauThietKe/{maYeuCau}/duyet", null)).StatusCode);
        (await Client.PutAsync($"/api/YeuCauThietKe/{maYeuCau}/gui-duyet", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PutAsync($"/api/YeuCauThietKe/{maYeuCau}/duyet", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PutAsJsonAsync($"/api/YeuCauThietKe/{maYeuCau}/tu-choi-boi-sale", new { lyDoTuChoi = "Chưa đồng ý" })).StatusCode);
        UseToken(customer);
        var awaiting = await Client.GetFromJsonAsync<JsonElement>($"/api/YeuCauThietKe/{maYeuCau}/lich-hien-tai");
        Assert.Equal("ChoKhachXacNhan", awaiting.GetProperty("trangThai").GetString());
        Assert.True(awaiting.GetProperty("lichTrinh").GetArrayLength() > 0);
        (await Client.PutAsync($"/api/YeuCauThietKe/{maYeuCau}/dong-y-lich", null)).EnsureSuccessStatusCode();
        UseToken(sale);
        var rejected = await Client.PutAsJsonAsync($"/api/YeuCauThietKe/{maYeuCau}/tu-choi-boi-sale", new { lyDoTuChoi = "Cần bổ sung dịch vụ phù hợp hơn." });
        rejected.EnsureSuccessStatusCode();

        var edit = await Client.PutAsJsonAsync($"/api/YeuCauThietKe/{maYeuCau}/sua-lich-trinh", new
        {
            chiTiets = new[]
            {
                new { ngayThu = 1, thuTuTrongNgay = 1, maDthamQuan, maSanPham, soLuong = 1, mota = "Lịch trình đã chỉnh sửa" }
            }
        });
        edit.EnsureSuccessStatusCode();
        var keptReason = await edit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Admin", keptReason.GetProperty("nguonLyDo").GetString());
        (await Client.PutAsync($"/api/YeuCauThietKe/{maYeuCau}/gui-duyet", null)).EnsureSuccessStatusCode();
        UseToken(customer);
        var current = await Client.GetFromJsonAsync<JsonElement>($"/api/YeuCauThietKe/{maYeuCau}/lich-hien-tai");
        Assert.Equal("Lịch trình đã chỉnh sửa", current.GetProperty("lichTrinh")[0].GetProperty("mota").GetString());
        Assert.Equal(JsonValueKind.Null, current.GetProperty("lyDo").ValueKind);
        var revision = await Client.PutAsJsonAsync($"/api/YeuCauThietKe/{maYeuCau}/yeu-cau-chinh-sua", new { lyDo = "  Thêm thời gian nghỉ  " });
        revision.EnsureSuccessStatusCode();
        var revised = await revision.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("CanChinhSua", revised.GetProperty("trangThai").GetString());
        Assert.Equal("KhachHang", revised.GetProperty("nguonLyDo").GetString());
        Assert.Equal("Thêm thời gian nghỉ", revised.GetProperty("lyDo").GetString());
        UseToken(sale);
        (await Client.PutAsync($"/api/YeuCauThietKe/{maYeuCau}/gui-duyet", null)).EnsureSuccessStatusCode();
        UseToken(customer);
        (await Client.PutAsync($"/api/YeuCauThietKe/{maYeuCau}/dong-y-lich", null)).EnsureSuccessStatusCode();
        UseToken(sale);
        (await Client.PutAsync($"/api/YeuCauThietKe/{maYeuCau}/duyet", null)).EnsureSuccessStatusCode();

        var tour = await Client.GetFromJsonAsync<JsonElement>($"/api/Tour/{maTour}");
        Assert.Equal("HoatDong", tour.GetProperty("trangThai").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await Client.PutAsync($"/api/YeuCauThietKe/{maYeuCau}/duyet", null)).StatusCode);
        UseToken(customer);
        var departures = await Client.GetFromJsonAsync<JsonElement>($"/api/Tour/{maTour}/lich-khoi-hanh");
        Assert.Equal(1, departures.GetArrayLength());
        Assert.True(departures[0].GetProperty("ngayKhoiHanh").GetDateTime() > DateTime.UtcNow);
        var booking = await Client.PostAsJsonAsync("/api/DatDichVu", new
        {
            maTour, maKhoiHanh = departures[0].GetProperty("maKhoiHanh").GetString(), slnguoiLon = 2, sltreEm = 0
        });
        Assert.Equal(HttpStatusCode.Created, booking.StatusCode);
    }

    [Fact]
    public async Task GenerateProposals_WithEnoughPoints_UsesDifferentPrimaryPoints()
    {
        SkipIfNoConnection();
        var customer = await RegisterAsync();
        var regionId = Unique("QR");
        var pointIds = Enumerable.Range(1, 6).Select(_ => Unique("QP")).ToList();
        using (var scope = ((IServiceScopeFactory)Factory.Services
            .GetService(typeof(IServiceScopeFactory))!).CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.KhuVucs.Add(new KhuVuc
            {
                MaKhuVuc = regionId,
                TenKhuVuc = "Khu kiểm thử đề xuất",
                QuocGia = "Việt Nam",
                TrangThai = "HoatDong"
            });
            context.DiemThamQuans.AddRange(pointIds.Select((id, index) => new DiemThamQuan
            {
                MaDthamQuan = id,
                TenDiaDanh = $"Điểm kiểm thử {index + 1}",
                DiaChi = "Khu kiểm thử đề xuất",
                MaKhuVuc = regionId
            }));
            await context.SaveChangesAsync();
        }

        string? createdRequestId = null;
        try
        {
            UseToken(customer);
            var create = await Client.PostAsJsonAsync("/api/YeuCauThietKe", new
            {
                diemDenMongMuon = "Khu kiểm thử đề xuất",
                soNgay = 2,
                soNguoiLon = 1,
                soTreEm = 0,
                nganSachDuKien = 100000000
            });
            create.EnsureSuccessStatusCode();
            var request = await create.Content.ReadFromJsonAsync<JsonElement>();
            var maYeuCau = request.GetProperty("maYeuCau").GetString()!;
            createdRequestId = maYeuCau;

            UseToken(await LoginSaleAsync());
            var generate = await Client.PostAsJsonAsync($"/api/YeuCauThietKe/{maYeuCau}/sinh-de-xuat", new { });
            generate.EnsureSuccessStatusCode();
            var proposals = await generate.Content.ReadFromJsonAsync<JsonElement>();
            var first = proposals[0].GetProperty("chiTiets").EnumerateArray()
                .Select(item => item.GetProperty("maDthamQuan").GetString())
                .Where(value => value is not null).ToHashSet();
            var third = proposals[2].GetProperty("chiTiets").EnumerateArray()
                .Where((_, index) => index % 2 == 0)
                .Select(item => item.GetProperty("maDthamQuan").GetString())
                .Where(value => value is not null).ToHashSet();

            Assert.NotEqual(first, third);
            Assert.False(first.IsSubsetOf(third));
            Assert.False(third.IsSubsetOf(first));
        }
        finally
        {
            using var scope = ((IServiceScopeFactory)Factory.Services
                .GetService(typeof(IServiceScopeFactory))!).CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var details = await context.LichTrinhDeXuatChiTiets
                .Where(item => pointIds.Contains(item.MaDthamQuan!)).ToListAsync();
            context.LichTrinhDeXuatChiTiets.RemoveRange(details);
            if (createdRequestId is not null)
            {
                var proposals = await context.LichTrinhDeXuats
                    .Where(item => item.MaYeuCau == FixedLengthHelper.PadTo20(createdRequestId)).ToListAsync();
                context.LichTrinhDeXuats.RemoveRange(proposals);
                var createdRequest = await context.YeuCauThietKes
                    .FirstOrDefaultAsync(item => item.MaYeuCau == FixedLengthHelper.PadTo20(createdRequestId));
                if (createdRequest is not null)
                    context.YeuCauThietKes.Remove(createdRequest);
            }
            var points = await context.DiemThamQuans.Where(item => pointIds.Contains(item.MaDthamQuan)).ToListAsync();
            context.DiemThamQuans.RemoveRange(points);
            var region = await context.KhuVucs.FirstOrDefaultAsync(item => item.MaKhuVuc == regionId);
            if (region is not null)
                context.KhuVucs.Remove(region);
            await context.SaveChangesAsync();
        }
    }
}
