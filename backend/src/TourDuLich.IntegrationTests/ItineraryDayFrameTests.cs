using TourDuLich.Application.Helpers;
using TourDuLich.Application.Services;
using TourDuLich.Infrastructure.Entities;
using Xunit;

namespace TourDuLich.IntegrationTests;

public sealed class ItineraryDayFrameTests
{
    [Fact]
    public void FormatDate_UsesCalendarDaysFromDeparture()
    {
        var start = new DateOnly(2026, 10, 20);
        Assert.Equal("20/10/2026", ItineraryDayFrame.FormatDate(start, 1));
        Assert.Equal("21/10/2026", ItineraryDayFrame.FormatDate(start, 2));
        Assert.Equal("22/10/2026", ItineraryDayFrame.FormatDate(start, 3));
        Assert.Equal("23/10/2026", ItineraryDayFrame.FormatDate(start, 4));
    }

    [Fact]
    public void DayOne_StartsWithCheckInThenFreeTime()
    {
        var stops = Compose(days: 4, depart: new TimeSpan(8, 0, 0), returnBy: new TimeSpan(20, 0, 0));
        var day1 = stops.Where(item => item.Day == 1).OrderBy(item => item.Start).ToList();
        Assert.Contains("Check-in", day1[0].Caption, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(TimeSpan.FromHours(1), day1[0].End - day1[0].Start);
        Assert.Contains("Tự túc", day1[1].Caption, StringComparison.OrdinalIgnoreCase);
        Assert.InRange((day1[1].End - day1[1].Start).TotalMinutes, 120, 180);
        AssertNoOverlap(day1);
    }

    [Fact]
    public void MiddleDay_HasThreeMealsAndStopsByBedtime()
    {
        var stops = Compose(days: 4, depart: new TimeSpan(8, 0, 0), returnBy: new TimeSpan(20, 0, 0));
        var day2 = stops.Where(item => item.Day == 2).OrderBy(item => item.Start).ToList();
        Assert.Contains(day2, item => item.Caption.Contains("Ăn sáng", StringComparison.Ordinal));
        Assert.Contains(day2, item => item.Caption.Contains("Ăn trưa", StringComparison.Ordinal));
        Assert.Contains(day2, item => item.Caption.Contains("Ăn tối", StringComparison.Ordinal));
        Assert.Contains(day2, item => item.Caption.Contains("Tham quan", StringComparison.Ordinal));
        Assert.All(day2.Where(item => item.End > item.Start), item => Assert.True(item.End <= ItineraryDayFrame.BedTime));
        AssertNoOverlap(day2);
    }

    [Fact]
    public void ReturnAfterBedtime_MovesCheckoutToNextMorning()
    {
        var stops = Compose(days: 4, depart: new TimeSpan(8, 0, 0), returnBy: new TimeSpan(21, 0, 0));
        Assert.Contains(stops, item => item.Day == 5 && item.Caption.Contains("Check-out", StringComparison.OrdinalIgnoreCase)
            && item.Start == ItineraryDayFrame.CheckoutMorning);
        Assert.DoesNotContain(stops.Where(item => item.Day == 4), item => item.Caption.Contains("Check-out", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HotelStayRules_AllowsFirstAndLastDayWithoutVisit()
    {
        var hotel = Hotel();
        var error = HotelStayRules.ItineraryStructureMessage(new (int, int, string?, SanPhamDoiTac?)[]
        {
            (1, 1, null, hotel),
            (2, 1, "DT1", hotel),
            (2, 2, null, Meal()),
            (3, 1, null, hotel)
        });
        Assert.Null(error);
    }

    private static IReadOnlyList<PlannedStop> Compose(int days, TimeSpan depart, TimeSpan returnBy)
    {
        var hotel = Hotel();
        var visits = Enumerable.Range(1, 8).Select(i => new DiemThamQuan
        {
            MaDthamQuan = $"DT{i:00}",
            TenDiaDanh = i == 1 ? "Bà Nà Hills" : $"Điểm {i}",
            DiaChi = $"Số {i} đường Trần Phú, phường Hải Châu, Đà Nẵng"
        }).ToList();
        var meals = new[]
        {
            Meal("NH1", "Nhà hàng A"),
            Meal("NH2", "Nhà hàng B")
        };
        return ItineraryDayFrame.Compose(days, depart, returnBy, 0, 0, "xe", "xe",
            "Hà Nội", "Đà Nẵng", hotel, visits, [], meals, [], 2, ItineraryDayFrame.HotelNights(days, ItineraryDayFrame.SpillCheckout(returnBy)), 1);
    }

    private static void AssertNoOverlap(IReadOnlyList<PlannedStop> day)
    {
        for (var i = 1; i < day.Count; i++)
        {
            if (day[i - 1].End == day[i - 1].Start || day[i].End == day[i].Start)
                continue;
            Assert.True(day[i].Start >= day[i - 1].End + ItineraryDayFrame.Gap,
                $"{day[i - 1].Caption} ends {day[i - 1].End} but next starts {day[i].Start}");
        }
    }

    private static SanPhamDoiTac Hotel() => new()
    {
        MaSanPham = "KS1",
        MaDoiTac = "DTKS",
        TenSanPham = "Deluxe",
        GiaNiemYet = 1200000,
        MaDoiTacNavigation = new DoiTac
        {
            MaDoiTac = "DTKS",
            TenDoiTac = "Khách sạn Sông Hàn",
            LoaiDoiTac = FixedLengthHelper.PadTo20("LuuTru")
        }
    };

    private static SanPhamDoiTac Meal(string id = "NH1", string name = "Nhà hàng") => new()
    {
        MaSanPham = id,
        MaDoiTac = id,
        TenSanPham = "Set menu",
        GiaNiemYet = 250000,
        MaDoiTacNavigation = new DoiTac
        {
            MaDoiTac = id,
            TenDoiTac = name,
            LoaiDoiTac = FixedLengthHelper.PadTo20("AnUong")
        }
    };
}
