using System.Globalization;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.Application.Services;

public sealed class PlannedStop
{
    public int Day { get; init; }
    public TimeSpan Start { get; init; }
    public TimeSpan End { get; init; }
    public string Caption { get; init; } = "";
    public string Kind { get; init; } = ItineraryKinds.ThamQuan;
    public DiemThamQuan? Point { get; init; }
    public SanPhamDoiTac? Product { get; init; }
    public int DonGia { get; init; }
    public int SoLuong { get; init; }
}

public static class ItineraryDayFrame
{
    public static readonly TimeSpan BreakfastStart = new(7, 30, 0);
    public static readonly TimeSpan BreakfastEnd = new(8, 30, 0);
    public static readonly TimeSpan LunchStart = new(11, 0, 0);
    public static readonly TimeSpan LunchEnd = new(13, 0, 0);
    public static readonly TimeSpan DinnerStart = new(17, 0, 0);
    public static readonly TimeSpan DinnerEnd = new(19, 0, 0);
    public static readonly TimeSpan BedTime = new(20, 30, 0);
    public static readonly TimeSpan Gap = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan CheckInDuration = TimeSpan.FromHours(1);
    public static readonly TimeSpan CheckOutDuration = TimeSpan.FromHours(1);
    public static readonly TimeSpan FreeTimeDuration = TimeSpan.FromMinutes(150);
    public static readonly TimeSpan FreeTimeMin = TimeSpan.FromHours(2);
    public static readonly TimeSpan VisitDuration = TimeSpan.FromMinutes(90);
    public static readonly TimeSpan VisitMin = TimeSpan.FromHours(1);
    public static readonly TimeSpan CheckoutMorning = new(8, 30, 0);

    public static string FormatDate(DateOnly? start, int dayNumber)
    {
        if (start is null || dayNumber < 1)
            return $"Ngày {dayNumber}";
        return start.Value.AddDays(dayNumber - 1).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    }

    public static bool SpillCheckout(TimeSpan returnBy) => returnBy > BedTime;

    public static int HotelNights(int days, bool spill)
    {
        days = Math.Max(1, days);
        return days == 1 || spill ? days : days - 1;
    }

    public static int CalendarDays(int days, bool spill) => Math.Max(1, days) + (spill ? 1 : 0);

    public static DateOnly? CheckoutDate(DateOnly? start, int days, TimeSpan returnBy)
    {
        if (start is null) return null;
        var spill = SpillCheckout(returnBy);
        return start.Value.AddDays(CalendarDays(days, spill) - 1);
    }

    public static string DurationText(TimeSpan span)
    {
        if (span <= TimeSpan.Zero)
            return "0 phút";
        var hours = (int)span.TotalHours;
        var minutes = span.Minutes;
        if (hours > 0 && minutes > 0)
            return $"{hours} giờ {minutes} phút";
        if (hours > 0)
            return $"{hours} giờ";
        return $"{minutes} phút";
    }

    public static string AddressOf(DiemThamQuan? point, SanPhamDoiTac? product, string place)
    {
        var partner = product?.MaDoiTacNavigation?.DiaChi?.Trim();
        if (!string.IsNullOrWhiteSpace(partner))
            return partner;
        var raw = point?.DiaChi?.Trim();
        if (!string.IsNullOrWhiteSpace(raw))
            return raw;
        var name = point?.TenDiaDanh?.Trim() ?? product?.MaDoiTacNavigation?.TenDoiTac?.Trim();
        return string.IsNullOrWhiteSpace(name) ? place : $"{name}, {place}";
    }

    public static bool IsOnSiteDining(string? name)
    {
        var text = (name ?? "").ToLowerInvariant();
        return text.Contains("bà nà") || text.Contains("ba na") || text.Contains("hills")
            || text.Contains("sun world") || text.Contains("vinwonders") || text.Contains("buffet")
            || text.Contains("cáp treo") || text.Contains("cap treo") || text.Contains("khu du lịch")
            || text.Contains("công viên");
    }

    public static (TimeSpan Start, TimeSpan End) ShiftedDinner(TimeSpan checkoutStart)
    {
        var end = checkoutStart - Gap;
        var start = end - TimeSpan.FromHours(2);
        if (start < LunchEnd + Gap)
            start = LunchEnd + Gap;
        if (end - start < TimeSpan.FromMinutes(45))
            end = start + TimeSpan.FromMinutes(45);
        return (start, end);
    }

    public static IReadOnlyList<PlannedStop> Compose(
        int days,
        TimeSpan departTime,
        TimeSpan returnBy,
        int goMinutes,
        int backMinutes,
        string goLabel,
        string backLabel,
        string originName,
        string placeName,
        SanPhamDoiTac hotel,
        IReadOnlyList<DiemThamQuan> visits,
        IReadOnlyList<DiemThamQuan> plays,
        IReadOnlyList<SanPhamDoiTac> meals,
        IReadOnlyList<SanPhamDoiTac> tickets,
        int guests,
        int nights,
        int seed)
    {
        days = Math.Clamp(days, 1, 30);
        var spill = SpillCheckout(returnBy);
        var hotelName = hotel.MaDoiTacNavigation?.TenDoiTac?.Trim() ?? hotel.TenSanPham.Trim();
        var hotelAddress = AddressOf(null, hotel, placeName);
        var result = new List<PlannedStop>();
        var visitIndex = 0;
        var points = visits.Count > 0 ? visits : plays;
        var checkoutStart = CheckoutStart(returnBy, backMinutes);

        TimeSpan Arrive()
        {
            var arrive = departTime + TimeSpan.FromMinutes(Math.Max(0, goMinutes));
            if (arrive >= TimeSpan.FromDays(1))
                return new TimeSpan(23, 0, 0);
            if (arrive < TimeSpan.Zero)
                return TimeSpan.Zero;
            return new TimeSpan(arrive.Hours, arrive.Minutes, 0);
        }

        DiemThamQuan NextPoint()
        {
            var pool = plays.Count > 0 && visitIndex % 3 == 1 ? plays : points;
            var point = pool[(seed + visitIndex) % pool.Count];
            visitIndex++;
            return point;
        }

        SanPhamDoiTac? TicketOf(DiemThamQuan point) =>
            tickets.FirstOrDefault(item => item.MaDthamQuan == point.MaDthamQuan);

        SanPhamDoiTac? MealAt(int index) =>
            meals.Count == 0 ? null : meals[Math.Abs(seed + index) % meals.Count];

        bool FillOneVisit(Timeline timeline, TimeSpan from, TimeSpan until)
        {
            if (points.Count == 0)
                return false;
            foreach (var (gapStart, gapEnd) in timeline.Gaps(from, until))
            {
                var take = VisitDuration <= gapEnd - gapStart ? VisitDuration : gapEnd - gapStart;
                if (take < VisitMin)
                    continue;
                var point = NextPoint();
                var ticket = TicketOf(point);
                timeline.Add(gapStart, take, false,
                    $"Tham quan {point.TenDiaDanh} — {AddressOf(point, ticket, placeName)} ({DurationText(take)}).",
                    ItineraryKinds.ThamQuan, point, ticket, ticket?.GiaNiemYet ?? 0, guests);
                return true;
            }
            return false;
        }

        string MealCaption(string verb, SanPhamDoiTac? meal, Timeline timeline, bool lunch)
        {
            if (lunch)
            {
                var lastVisit = timeline.Slots.LastOrDefault(item => item.Point is not null);
                if (lastVisit is not null && IsOnSiteDining(lastVisit.Point?.TenDiaDanh))
                    return $"{verb} buffet tại {lastVisit.Point!.TenDiaDanh} — {AddressOf(lastVisit.Point, null, placeName)}";
            }
            if (meal is null)
                return $"{verb} tại {hotelName} — {hotelAddress}";
            return $"{verb} tại {meal.MaDoiTacNavigation?.TenDoiTac?.Trim() ?? meal.TenSanPham} — {AddressOf(null, meal, placeName)}";
        }

        void PlaceMeal(Timeline timeline, string kind, TimeSpan start, TimeSpan end, SanPhamDoiTac? meal, string caption)
        {
            var product = meal ?? hotel;
            var duration = end - start;
            if (!timeline.Fits(start, end))
            {
                var late = timeline.Cursor > start ? timeline.Cursor : start;
                if (end - late < TimeSpan.FromMinutes(45) || !timeline.Fits(late, end))
                    return;
                start = late;
                duration = end - start;
            }
            var dining = meal is not null && HotelStayRules.IsDining(meal.MaDoiTacNavigation?.LoaiDoiTac);
            timeline.Add(start, duration, false,
                $"{caption} ({DurationText(duration)}).",
                kind, null, product, dining ? product.GiaNiemYet : 0, dining ? guests : 1);
        }

        void StandardDayBody(Timeline timeline, int day, TimeSpan visitUntil, bool dinner, TimeSpan? dinnerUntil)
        {
            PlaceMeal(timeline, ItineraryKinds.AnSang, BreakfastStart, BreakfastEnd, MealAt(day * 3),
                MealCaption("Ăn sáng", MealAt(day * 3), timeline, false));
            FillOneVisit(timeline, BreakfastEnd, LunchStart);
            PlaceMeal(timeline, ItineraryKinds.AnTrua, LunchStart, LunchEnd, MealAt(day * 3 + 1),
                MealCaption("Ăn trưa", MealAt(day * 3 + 1), timeline, true));
            FillOneVisit(timeline, LunchEnd, dinnerUntil ?? visitUntil);
            if (dinner)
            {
                var window = dinnerUntil is { } cut ? ShiftedDinner(cut) : (DinnerStart, DinnerEnd);
                PlaceMeal(timeline, ItineraryKinds.AnToi, window.Item1, window.Item2, MealAt(day * 3 + 2),
                    MealCaption("Ăn tối", MealAt(day * 3 + 2), timeline, false));
            }
        }

        var dayCount = CalendarDays(days, spill);
        for (var day = 1; day <= dayCount; day++)
        {
            var timeline = new Timeline(day);
            var isFirst = day == 1;
            var isSpillMorning = spill && day == days + 1;
            var isCheckoutDay = (!spill && day == days) || isSpillMorning;
            var isMiddle = !isFirst && !isCheckoutDay;

            if (isSpillMorning)
            {
                var breakfastEnd = CheckoutMorning - Gap;
                var breakfastStart = breakfastEnd - TimeSpan.FromHours(1);
                PlaceMeal(timeline, ItineraryKinds.AnSang, breakfastStart, breakfastEnd, MealAt(day * 3),
                    MealCaption("Ăn sáng", MealAt(day * 3), timeline, false));
                var back = backMinutes > 0
                    ? $" Sau đó về {originName} bằng {backLabel} (~{backMinutes} phút), có mặt trước {returnBy:hh\\:mm}."
                    : "";
                timeline.Add(CheckoutMorning, CheckOutDuration, true,
                    $"Check-out tại {hotelName} — {hotelAddress} ({DurationText(CheckOutDuration)}).{back}",
                    ItineraryKinds.CheckOut, null, hotel, 0, 1);
                result.AddRange(timeline.Slots);
                continue;
            }

            if (isFirst)
            {
                var arrive = Arrive();
                var ride = goMinutes > 0
                    ? $"Sau hành trình {originName} → {placeName} bằng {goLabel} (~{goMinutes} phút). "
                    : "";
                timeline.Add(arrive, CheckInDuration, true,
                    $"{ride}Check-in tại {hotelName} — {hotelAddress} ({DurationText(CheckInDuration)}). Phòng {hotel.TenSanPham} · {nights} đêm.",
                    ItineraryKinds.CheckIn, null, hotel, hotel.GiaNiemYet, nights);

                var freeStart = arrive + CheckInDuration + Gap;
                var freeEnd = freeStart + FreeTimeDuration;
                if (freeEnd > BedTime)
                    freeEnd = BedTime;
                if (freeEnd - freeStart >= FreeTimeMin)
                    timeline.Add(freeStart, freeEnd - freeStart, true,
                        $"Tự túc / nghỉ ngơi tại {hotelName} — {hotelAddress} ({DurationText(freeEnd - freeStart)}).",
                        ItineraryKinds.TuTuc, null, hotel, 0, 1);
            }

            if (isMiddle || (isFirst && !isCheckoutDay))
            {
                if (isFirst)
                {
                    var earliest = timeline.Slots.Select(item => item.End).DefaultIfEmpty(TimeSpan.Zero).Max();
                    if (BreakfastEnd > earliest)
                        PlaceMeal(timeline, ItineraryKinds.AnSang, BreakfastStart, BreakfastEnd, MealAt(day * 3),
                            MealCaption("Ăn sáng", MealAt(day * 3), timeline, false));
                    FillOneVisit(timeline, timeline.Slots.Select(item => item.End).DefaultIfEmpty(BreakfastEnd).Max(), LunchStart);
                    PlaceMeal(timeline, ItineraryKinds.AnTrua, LunchStart, LunchEnd, MealAt(day * 3 + 1),
                        MealCaption("Ăn trưa", MealAt(day * 3 + 1), timeline, true));
                    FillOneVisit(timeline, LunchEnd, DinnerStart);
                    PlaceMeal(timeline, ItineraryKinds.AnToi, DinnerStart, DinnerEnd, MealAt(day * 3 + 2),
                        MealCaption("Ăn tối", MealAt(day * 3 + 2), timeline, false));
                }
                else
                    StandardDayBody(timeline, day, DinnerStart, true, null);

                if (timeline.Fits(BedTime, BedTime, true))
                    timeline.Add(BedTime, TimeSpan.Zero, true,
                        $"Tự túc và nghỉ đêm tại {hotelName} — {hotelAddress}.",
                        ItineraryKinds.NghiDem, null, hotel, 0, 1);
            }

            if (isCheckoutDay && !isSpillMorning)
            {
                var back = backMinutes > 0
                    ? $" Sau đó về {originName} bằng {backLabel} (~{backMinutes} phút), có mặt trước {returnBy:hh\\:mm}."
                    : "";
                if (returnBy <= new TimeSpan(11, 0, 0))
                {
                    var breakfastEnd = checkoutStart - Gap;
                    var breakfastStart = breakfastEnd - TimeSpan.FromHours(1);
                    if (breakfastStart < TimeSpan.FromHours(6))
                        breakfastStart = TimeSpan.FromHours(6);
                    if (breakfastEnd - breakfastStart >= TimeSpan.FromMinutes(45))
                        PlaceMeal(timeline, ItineraryKinds.AnSang, breakfastStart, breakfastEnd, MealAt(day * 3),
                            MealCaption("Ăn sáng", MealAt(day * 3), timeline, false));
                }
                else if (returnBy <= new TimeSpan(17, 0, 0))
                {
                    PlaceMeal(timeline, ItineraryKinds.AnSang, BreakfastStart, BreakfastEnd, MealAt(day * 3),
                        MealCaption("Ăn sáng", MealAt(day * 3), timeline, false));
                    FillOneVisit(timeline, BreakfastEnd, LunchStart);
                    if (checkoutStart >= LunchEnd + Gap)
                    {
                        PlaceMeal(timeline, ItineraryKinds.AnTrua, LunchStart, LunchEnd, MealAt(day * 3 + 1),
                            MealCaption("Ăn trưa", MealAt(day * 3 + 1), timeline, true));
                        FillOneVisit(timeline, LunchEnd, checkoutStart);
                    }
                }
                else
                {
                    StandardDayBody(timeline, day, checkoutStart, true, checkoutStart);
                }

                timeline.Add(checkoutStart, CheckOutDuration, true,
                    $"Check-out tại {hotelName} — {hotelAddress} ({DurationText(CheckOutDuration)}).{back}",
                    ItineraryKinds.CheckOut, null, hotel, 0, 1);
            }

            result.AddRange(timeline.Slots);
        }

        return result;
    }

    private static TimeSpan CheckoutStart(TimeSpan returnBy, int backMinutes)
    {
        var leave = returnBy - TimeSpan.FromMinutes(Math.Max(0, backMinutes)) - CheckOutDuration;
        if (leave < new TimeSpan(6, 0, 0))
            leave = new TimeSpan(6, 0, 0);
        if (leave > BedTime - CheckOutDuration)
            leave = BedTime - CheckOutDuration;
        return leave;
    }

    private sealed class Timeline
    {
        public int Day { get; }
        public List<PlannedStop> Slots { get; } = [];
        public TimeSpan Cursor => Slots.Count == 0 ? TimeSpan.Zero : Slots.Max(item => item.End) + Gap;

        public Timeline(int day) => Day = day;

        public bool Fits(TimeSpan start, TimeSpan end, bool allowLate = false)
        {
            if (end < start)
                return false;
            if (!allowLate && end > BedTime)
                return false;
            foreach (var slot in Slots)
            {
                var paddedEnd = slot.End == slot.Start ? slot.Start : slot.End + Gap;
                if (start < paddedEnd && slot.Start < end + Gap)
                    return false;
            }
            return true;
        }

        public PlannedStop? Add(TimeSpan start, TimeSpan duration, bool allowLate, string caption,
            string kind, DiemThamQuan? point, SanPhamDoiTac? product, int donGia, int soLuong)
        {
            var end = start + duration;
            if (!Fits(start, end, allowLate))
                return null;
            var stop = new PlannedStop
            {
                Day = Day,
                Start = start,
                End = end,
                Caption = caption,
                Kind = kind,
                Point = point,
                Product = product,
                DonGia = donGia,
                SoLuong = Math.Max(1, soLuong)
            };
            Slots.Add(stop);
            Slots.Sort((a, b) => a.Start.CompareTo(b.Start));
            return stop;
        }

        public IEnumerable<(TimeSpan Start, TimeSpan End)> Gaps(TimeSpan from, TimeSpan until)
        {
            var cursor = from;
            foreach (var slot in Slots.Where(item => item.End > from && item.End > item.Start).OrderBy(item => item.Start))
            {
                var occupiedStart = slot.Start < from ? from : slot.Start;
                if (occupiedStart - Gap - cursor >= VisitMin)
                    yield return (cursor, occupiedStart - Gap);
                var next = slot.End + Gap;
                cursor = next < from ? from : next;
            }
            var cap = until < BedTime ? until : BedTime;
            if (cap - cursor >= VisitMin)
                yield return (cursor, cap);
        }
    }
}
