using System.Globalization;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.Application.Services;

public sealed class PlannedStop
{
    public int Day { get; init; }
    public TimeSpan Start { get; init; }
    public TimeSpan End { get; init; }
    public string Caption { get; init; } = "";
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

    public static string AddressOf(DiemThamQuan? point, string place)
    {
        var raw = point?.DiaChi?.Trim();
        if (!string.IsNullOrWhiteSpace(raw))
            return raw;
        var name = point?.TenDiaDanh?.Trim();
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
        var hotelAddress = $"{hotelName}, {placeName}";
        var result = new List<PlannedStop>();
        var visitIndex = 0;
        var points = visits.Count > 0 ? visits : plays;

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

        void FillVisits(Timeline timeline, TimeSpan from, TimeSpan until)
        {
            if (points.Count == 0)
                return;
            foreach (var (gapStart, gapEnd) in timeline.Gaps(from, until))
            {
                var start = gapStart;
                while (gapEnd - start >= VisitMin)
                {
                    var take = VisitDuration <= gapEnd - start ? VisitDuration : gapEnd - start;
                    if (take < VisitMin)
                        break;
                    var point = NextPoint();
                    var ticket = TicketOf(point);
                    timeline.Add(start, take, false,
                        $"Tham quan {point.TenDiaDanh} — {AddressOf(point, placeName)} ({DurationText(take)}).",
                        point, ticket, ticket?.GiaNiemYet ?? 0, guests);
                    start += take + Gap;
                }
            }
        }

        string LunchCaption(Timeline timeline, SanPhamDoiTac? lunchMeal)
        {
            var lastVisit = timeline.Slots.LastOrDefault(item => item.Point is not null);
            if (lastVisit is not null && IsOnSiteDining(lastVisit.Point?.TenDiaDanh))
                return $"Ăn trưa buffet tại {lastVisit.Point!.TenDiaDanh} — {AddressOf(lastVisit.Point, placeName)}";
            if (lunchMeal is null)
                return $"Ăn trưa tại {hotelName} — {hotelAddress}";
            var near = lastVisit?.Point is null ? "" : $" (gần {lastVisit.Point.TenDiaDanh})";
            return $"Ăn trưa tại {lunchMeal.MaDoiTacNavigation?.TenDoiTac?.Trim() ?? lunchMeal.TenSanPham}{near} — {placeName}";
        }

        var dayCount = spill ? days + 1 : days;
        for (var day = 1; day <= dayCount; day++)
        {
            var timeline = new Timeline(day);
            var isFirst = day == 1;
            var isLast = !spill && day == days;
            var isSpillMorning = spill && day == days + 1;
            var isFullStay = !isLast && !isSpillMorning;

            if (isSpillMorning)
            {
                var back = backMinutes > 0
                    ? $" Sau đó về {originName} bằng {backLabel} (~{backMinutes} phút), có mặt trước {returnBy:hh\\:mm}."
                    : "";
                timeline.Add(CheckoutMorning, CheckOutDuration, true,
                    $"Check-out tại {hotelName} — {hotelAddress} ({DurationText(CheckOutDuration)}).{back}",
                    null, hotel, 0, 1);
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
                    null, hotel, hotel.GiaNiemYet, nights);

                var freeStart = arrive + CheckInDuration + Gap;
                var freeEnd = freeStart + FreeTimeDuration;
                if (freeEnd > BedTime)
                    freeEnd = BedTime;
                if (freeStart < LunchStart && freeEnd > LunchStart - Gap && LunchStart - Gap - freeStart >= FreeTimeMin)
                    freeEnd = LunchStart - Gap;
                if (freeStart < DinnerStart && freeEnd > DinnerStart - Gap && DinnerStart - Gap - freeStart >= FreeTimeMin)
                    freeEnd = DinnerStart - Gap;
                if (freeEnd - freeStart >= FreeTimeMin)
                    timeline.Add(freeStart, freeEnd - freeStart, true,
                        $"Tự túc / nghỉ ngơi tại {hotelName} — {hotelAddress} ({DurationText(freeEnd - freeStart)}).",
                        null, hotel, 0, 1);
            }

            if (isLast)
            {
                var leave = returnBy - TimeSpan.FromMinutes(Math.Max(0, backMinutes)) - CheckOutDuration;
                if (leave < new TimeSpan(8, 0, 0))
                    leave = new TimeSpan(8, 0, 0);
                if (leave > BedTime - CheckOutDuration)
                    leave = BedTime - CheckOutDuration;
                var back = backMinutes > 0
                    ? $" Sau đó về {originName} bằng {backLabel} (~{backMinutes} phút), có mặt trước {returnBy:hh\\:mm}."
                    : "";
                timeline.Add(leave, CheckOutDuration, false,
                    $"Check-out tại {hotelName} — {hotelAddress} ({DurationText(CheckOutDuration)}).{back}",
                    null, hotel, 0, 1);
            }

            var earliest = isFirst
                ? timeline.Slots.Select(item => item.End).DefaultIfEmpty(TimeSpan.Zero).Max()
                : TimeSpan.Zero;
            if (BreakfastEnd > earliest)
                TryMeal(timeline, BreakfastStart, BreakfastEnd, MealAt(day * 3) ?? hotel,
                    $"Ăn sáng tại {hotelName} — {hotelAddress}", guests);
            var visitFrom = isFirst
                ? timeline.Slots.Select(item => item.End).DefaultIfEmpty(BreakfastStart).Max()
                : BreakfastStart;
            FillVisits(timeline, visitFrom, LunchStart);
            var lunchMeal = MealAt(day * 3 + 1);
            if (LunchEnd > earliest)
                TryMeal(timeline, LunchStart, LunchEnd, lunchMeal ?? hotel, LunchCaption(timeline, lunchMeal), guests);
            FillVisits(timeline, LunchEnd, DinnerStart);
            var dinnerMeal = MealAt(day * 3 + 2);
            var dinnerCaption = dinnerMeal is null
                ? $"Ăn tối tại {hotelName} — {hotelAddress}"
                : $"Ăn tối tại {dinnerMeal.MaDoiTacNavigation?.TenDoiTac?.Trim() ?? dinnerMeal.TenSanPham} — {placeName}";
            if (DinnerEnd > earliest)
                TryMeal(timeline, DinnerStart, DinnerEnd, dinnerMeal ?? hotel, dinnerCaption, guests);

            if (isFullStay && timeline.Fits(BedTime, BedTime, true))
                timeline.Add(BedTime, TimeSpan.Zero, true,
                    $"Tự túc và nghỉ đêm tại {hotelName} — {hotelAddress}.",
                    null, hotel, 0, 1);

            result.AddRange(timeline.Slots);
        }

        return result;
    }

    private static void TryMeal(Timeline timeline, TimeSpan start, TimeSpan end,
        SanPhamDoiTac product, string caption, int guests)
    {
        var duration = end - start;
        if (!timeline.Fits(start, end))
        {
            var late = timeline.Cursor > start ? timeline.Cursor : start;
            if (end - late < TimeSpan.FromMinutes(45) || !timeline.Fits(late, end))
                return;
            start = late;
            duration = end - start;
        }
        var dining = HotelStayRules.IsDining(product.MaDoiTacNavigation?.LoaiDoiTac);
        timeline.Add(start, duration, false,
            $"{caption} ({DurationText(duration)}).",
            null, product, dining ? product.GiaNiemYet : 0, dining ? guests : 1);
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
            DiemThamQuan? point, SanPhamDoiTac? product, int donGia, int soLuong)
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
