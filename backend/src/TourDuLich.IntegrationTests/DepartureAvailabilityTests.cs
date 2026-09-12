using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourDuLich.API.Controllers;
using TourDuLich.API.DTOs;
using TourDuLich.API.Services;
using TourDuLich.Application.Helpers;
using TourDuLich.Application.Services;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.IntegrationTests;

public sealed class DepartureAvailabilityTests
{
    private static string Key(string v) => FixedLengthHelper.PadTo20(v);
    private static AppDbContext SqlContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlServer("Server=unused;Database=unused;Integrated Security=true;Connect Timeout=1").Options);

    [Fact]
    public void Capacity_IsPerDeparture_SumsPeople_NotAccounts_AndFallbackIsNullable()
    {
        var tour = new Tour { MaTour = Key("T1"), Slkhach = 20 };
        var departure = new LichKhoiHanh { MaKhoiHanh = Key("D1"), MaTour = tour.MaTour, MaTourNavigation = tour, SoCho = 10 };
        var bookings = new List<DatDichVu>
        {
            Booking("B1", "U1", "D1", 3, 1), Booking("B2", "U2", "D1", 4, 0), Booking("B3", "U3", "D1", 1, 1),
            Booking("B4", "U4", "D1", 10, 0, "DaHuy"), Booking("B5", "U5", "D1", 10, 0, "ChoHoanTien"),
            Booking("OTHER", "U6", "D2", 20, 0)
        };
        departure.DatDichVus = bookings.Where(b => b.MaKhoiHanh == departure.MaKhoiHanh).ToList();
        DepartureView Read() => DepartureAvailability.Select(new[] { departure }.AsQueryable()).Single();
        var full = Read();
        Assert.Equal((10, 10L, 0L, 3), (full.SucChua, full.DaDat, full.ConTrong, full.SoTaiKhoan));
        bookings[0].TrangThai = Key("ChoHoanTien");
        var cancelled = Read();
        Assert.Equal((6L, 4L, 2), (cancelled.DaDat, cancelled.ConTrong, cancelled.SoTaiKhoan));
        var roster = DepartureAvailability.Guests(bookings.AsQueryable().Where(b => b.MaKhoiHanh == departure.MaKhoiHanh), new List<KhachHang>().AsQueryable()).ToArray();
        Assert.Equal(new[] { "B2", "B3" }, roster.Select(b => b.MaBooking));
        Assert.Equal(6, roster.Sum(b => b.SoCho));
        bookings[2].MaUser = Key("U2");
        Assert.Equal(1, Read().SoTaiKhoan);
        Assert.Equal(6, Read().DaDat);
        departure.SoCho = null; Assert.Equal(20, Read().SucChua);
        departure.SoCho = 0; Assert.Equal(0, Read().SucChua);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("HoanThanh")]
    [InlineData("ChoXacNhan")]
    [InlineData("DaThanhToan")]
    public void AllStatusesExceptCancelledAndRefundPending_HoldSeats(string? state)
    {
        var booking = Booking("B1", "U1", "D1", int.MaxValue, 1, state);
        var departure = new LichKhoiHanh { MaKhoiHanh = Key("D1"), MaTour = Key("T1"),
            MaTourNavigation = new Tour { Slkhach = int.MaxValue }, DatDichVus = [booking] };
        Assert.Single(DepartureAvailability.HeldBookings(new[] { booking }.AsQueryable()));
        Assert.Equal((long)int.MaxValue + 1, DepartureAvailability.Select(new[] { departure }.AsQueryable()).Single().DaDat);
    }

    [Fact]
    public void SqlQueries_TranslateWithoutTrimSafe_AndDoNotOpenConnection()
    {
        using var context = SqlContext();
        var sql = DepartureAvailability.Select(context.LichKhoiHanhs).ToQueryString();
        Assert.Contains("SoCho", sql); Assert.Contains("SUM", sql); Assert.Contains("bigint", sql); Assert.Contains("DISTINCT", sql);
        Assert.Contains("DaHuy", sql); Assert.Contains("ChoHoanTien", sql);
        var guests = DepartureAvailability.Guests(context.DatDichVus, context.KhachHangs).ToQueryString();
        Assert.Contains("MaKhachHang", guests); Assert.Contains("SoDienThoai", guests);
        Assert.DoesNotContain("TrimSafe", sql + guests);
        Assert.Contains("LyDoTuChoiBoiSale", context.YeuCauThietKes.Select(DesignRequestView.Projection).ToQueryString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Profile_PrefersLinkedProfile_OtherwiseUsesAccount_AndReturnsPapers(bool linked)
    {
        using var context = SqlContext();
        var accountProfile = new KhachHang { MaKhachHang = Key("K1"), MaUser = Key("U1"), Ho = "Nguyễn", Ten = "Tài khoản", SoDienThoai = "0900000001" };
        var selectedProfile = new KhachHang { MaKhachHang = Key("K2"), MaUser = Key("U1"), Ho = "Nguyễn", Ten = "Đã chọn", SoDienThoai = "0900000002", Email = "test@example.invalid", QuocTich = "Việt Nam" };
        var expected = linked ? selectedProfile : accountProfile;
        expected.GiayTos.Add(new GiayTo { MaGiayTo = Key("G1"), MaKhachHang = expected.MaKhachHang, LoaiGiayTo = "CCCD", SoTrenGiayTo = "000000000001", NgayCap = new(2020, 1, 1), NgayHetHan = new(2030, 1, 1), NoiCap = "Nơi cấp kiểm thử" });
        var booking = Booking("B1", "U1", "D1", 1, 0); booking.MaKhachHang = linked ? selectedProfile.MaKhachHang : null;
        context.DatDichVus = new KeyQuerySet<DatDichVu>(context, new List<DatDichVu> { booking });
        context.KhachHangs = new KeyQuerySet<KhachHang>(context, new List<KhachHang> { accountProfile, selectedProfile });
        var controller = new DatDichVuController(context, new NoLogger());
        var result = JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(await controller.GetGuestProfile("B1")).Value);
        Assert.Equal(expected.MaKhachHang.Trim(), result.GetProperty("maKhachHang").GetString());
        Assert.Equal(expected.Ten, result.GetProperty("ten").GetString());
        var paper = Assert.Single(result.GetProperty("giayTo").EnumerateArray());
        Assert.Equal("CCCD", paper.GetProperty("loaiGiayTo").GetString());
        Assert.Equal("000000000001", paper.GetProperty("soTrenGiayTo").GetString());
        Assert.Equal("2020-01-01", paper.GetProperty("ngayCap").GetString());
        Assert.Equal("2030-01-01", paper.GetProperty("ngayHetHan").GetString());
        Assert.Equal("Nơi cấp kiểm thử", paper.GetProperty("noiCap").GetString());
        Assert.IsType<NotFoundObjectResult>(await controller.GetGuestProfile("MISSING"));
    }

    [Fact]
    public void StaffRosterAndProfile_AreNotCustomerEndpoints()
    {
        foreach (var method in new[] {
            typeof(LichKhoiHanhController).GetMethod(nameof(LichKhoiHanhController.GetGuests))!,
            typeof(DatDichVuController).GetMethod(nameof(DatDichVuController.GetGuestProfile))! })
            Assert.Equal("Sale,Admin", method.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }

    private static DatDichVu Booking(string id, string user, string departure, int adults, int children, string? status = "ChoXacNhan")
        => new() { MaBooking = Key(id), MaUser = Key(user), MaTour = Key("T1"), MaKhoiHanh = Key(departure),
            SlnguoiLon = adults, SltreEm = children, TrangThai = status is null ? null : Key(status),
            MaUserNavigation = new NguoiSuDung { SoDienThoai = "0900000000" } };
    private sealed class NoLogger : IHanhViLogger { public Task LogAsync(string user, string? tour, string action) => Task.CompletedTask; }
}

