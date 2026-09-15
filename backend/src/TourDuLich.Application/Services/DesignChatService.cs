using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.Application.Services;

public sealed class DesignChatTurn
{
    public string MaHoiThoai { get; set; } = "";
    public string TrangThai { get; set; } = "DangHoi";
    public string? MaYeuCau { get; set; }
    public object Slots { get; set; } = new { };
    public IReadOnlyList<object> Messages { get; set; } = [];
    public object? Widget { get; set; }
}

public sealed class DesignChatRequest
{
    public string? MaHoiThoai { get; set; }
    public string? Message { get; set; }
    public string? MaTinh { get; set; }
    public string? Date { get; set; }
    public string? Time { get; set; }
    public int? Number { get; set; }
    public string? MucDich { get; set; }
    public bool Confirm { get; set; }
}

internal sealed class ChatSlots
{
    public string Step { get; set; } = "origin";
    public string? TenChuyenDi { get; set; }
    public string? MucDich { get; set; }
    public string? MaTinhXuatPhat { get; set; }
    public string? TenTinhXuatPhat { get; set; }
    public string? MaTinhDen { get; set; }
    public string? TenTinhDen { get; set; }
    public DateOnly? NgayKhoiHanh { get; set; }
    public TimeSpan? GioKhoiHanh { get; set; }
    public DateOnly? NgayKetThuc { get; set; }
    public TimeSpan? GioKetThuc { get; set; }
    public int? SoNguoiLon { get; set; }
    public int? SoTreEm { get; set; }
    public int? NganSach { get; set; }
    public int SoSuKienMoiNgay { get; set; } = 3;
    public string? GhiChu { get; set; }
}

public interface IDesignChatService
{
    Task<DesignChatTurn> StartOrContinueAsync(string maUser, DesignChatRequest request, CancellationToken cancellationToken = default);
    Task<DesignChatTurn> GetAsync(string maUser, string maHoiThoai, CancellationToken cancellationToken = default);
}

public sealed class DesignChatService : IDesignChatService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly AppDbContext _context;
    private readonly IDestinationResolver _destinations;
    private readonly IDeXuatLichTrinhService _planner;

    public DesignChatService(AppDbContext context, IDestinationResolver destinations, IDeXuatLichTrinhService planner)
    {
        _context = context;
        _destinations = destinations;
        _planner = planner;
    }

    public async Task<DesignChatTurn> GetAsync(string maUser, string maHoiThoai, CancellationToken cancellationToken = default)
    {
        var chat = await LoadAsync(maUser, maHoiThoai, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy hội thoại.");
        return ToTurn(chat, JsonSerializer.Deserialize<ChatSlots>(chat.DuLieuJson ?? "{}", JsonOpts) ?? new ChatSlots());
    }

    public async Task<DesignChatTurn> StartOrContinueAsync(string maUser, DesignChatRequest request, CancellationToken cancellationToken = default)
    {
        var userId = FixedLengthHelper.PadTo20(maUser);
        HoiThoaiThietKe chat;
        ChatSlots slots;
        if (string.IsNullOrWhiteSpace(request.MaHoiThoai))
        {
            chat = new HoiThoaiThietKe
            {
                MaHoiThoai = await NewIdAsync("HT", id => _context.HoiThoaiThietKes.AnyAsync(item => item.MaHoiThoai == id, cancellationToken)),
                MaUser = userId,
                TrangThai = FixedLengthHelper.PadTo20("DangHoi"),
                DuLieuJson = JsonSerializer.Serialize(new ChatSlots(), JsonOpts),
                NgayTao = DateTime.UtcNow
            };
            _context.HoiThoaiThietKes.Add(chat);
            await AddBot(chat, "Xin chào, tôi là trợ lý thiết kế tour ANAM. Tôi chỉ ghép lịch từ điểm tham quan, khách sạn và nhà hàng đang có trong hệ thống — không bịa địa điểm.\n\nBạn muốn xuất phát từ tỉnh/thành nào?", ProvinceWidget(), cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return ToTurn(chat, new ChatSlots());
        }

        chat = await LoadAsync(maUser, request.MaHoiThoai, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy hội thoại.");
        slots = JsonSerializer.Deserialize<ChatSlots>(chat.DuLieuJson ?? "{}", JsonOpts) ?? new ChatSlots();
        if (FixedLengthHelper.TrimSafe(chat.TrangThai) != "DangHoi")
            return ToTurn(chat, slots);

        var spoken = (request.Message ?? string.Empty).Trim();
        if (spoken.Length > 0)
            await AddGuest(chat, spoken, cancellationToken);

        if (spoken.Equals("làm lại", StringComparison.OrdinalIgnoreCase) || spoken.Equals("reset", StringComparison.OrdinalIgnoreCase))
        {
            slots = new ChatSlots();
            await AddBot(chat, "Đã xóa lựa chọn. Bạn xuất phát từ đâu?", ProvinceWidget(), cancellationToken);
            return await Persist(chat, slots, cancellationToken);
        }

        switch (slots.Step)
        {
            case "origin":
                if (await TryProvince(request, spoken, true, slots, cancellationToken) is { } originErr)
                    await AddBot(chat, originErr, ProvinceWidget(), cancellationToken);
                else
                {
                    slots.Step = "dest";
                    await AddBot(chat, $"Xuất phát: **{slots.TenTinhXuatPhat}**. Bạn muốn đến tỉnh/thành nào? Gõ Nha Trang, Hội An, Đà Lạt… hoặc chọn trong danh sách.", ProvinceWidget(), cancellationToken);
                }
                break;
            case "dest":
                if (await TryProvince(request, spoken, false, slots, cancellationToken) is { } destErr)
                    await AddBot(chat, destErr, ProvinceWidget(), cancellationToken);
                else
                {
                    slots.Step = "depart";
                    await AddBot(chat, $"Điểm đến: **{slots.TenTinhDen}**. Ngày và giờ khởi hành? (ví dụ 15/11/2026 15:00)", DateTimeWidget("depart"), cancellationToken);
                }
                break;
            case "depart":
                if (!TryDateTime(request, spoken, out var departDate, out var departTime))
                    await AddBot(chat, "Nhập ngày giờ đi theo dạng 15/11/2026 15:00 hoặc dùng ô chọn.", DateTimeWidget("depart"), cancellationToken);
                else if (departDate < DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7).AddDays(1)))
                    await AddBot(chat, "Ngày đi phải từ ngày mai trở đi.", DateTimeWidget("depart"), cancellationToken);
                else
                {
                    slots.NgayKhoiHanh = departDate;
                    slots.GioKhoiHanh = departTime;
                    slots.Step = "return";
                    await AddBot(chat, "Ngày và giờ bạn cần có mặt lại tại điểm xuất phát?", DateTimeWidget("return"), cancellationToken);
                }
                break;
            case "return":
                if (!TryDateTime(request, spoken, out var backDate, out var backTime))
                    await AddBot(chat, "Nhập ngày giờ về, ví dụ 18/11/2026 20:00.", DateTimeWidget("return"), cancellationToken);
                else if (slots.NgayKhoiHanh is { } start && backDate < start)
                    await AddBot(chat, "Ngày về không được trước ngày đi.", DateTimeWidget("return"), cancellationToken);
                else
                {
                    slots.NgayKetThuc = backDate;
                    slots.GioKetThuc = backTime;
                    slots.Step = "people";
                    await AddBot(chat, "Bao nhiêu người lớn và trẻ em? Ví dụ: 2 người lớn, 1 trẻ em.", PeopleWidget(), cancellationToken);
                }
                break;
            case "people":
                if (!TryPeople(request, spoken, slots))
                    await AddBot(chat, "Cho tôi số người lớn (và trẻ em nếu có).", PeopleWidget(), cancellationToken);
                else
                {
                    slots.Step = "budget";
                    await AddBot(chat, "Ngân sách cả đoàn khoảng bao nhiêu (VNĐ)? Tôi sẽ ghép 3 phương án quanh mức này (±12%).", NumberWidget("budget"), cancellationToken);
                }
                break;
            case "budget":
                if (!TryBudget(request, spoken, slots))
                    await AddBot(chat, "Nhập số tiền, ví dụ 15000000 hoặc 15 triệu.", NumberWidget("budget"), cancellationToken);
                else
                {
                    slots.Step = "purpose";
                    await AddBot(chat, "Mục đích chuyến đi?", PurposeWidget(), cancellationToken);
                }
                break;
            case "purpose":
                slots.MucDich = NormalizePurpose(request.MucDich ?? spoken);
                slots.Step = "events";
                await AddBot(chat, "Mỗi ngày bạn muốn khoảng bao nhiêu hoạt động (1–4)? Tôi có thể xếp ít hơn nếu hết giờ trước 20:00 hoặc vượt ngân sách.", NumberWidget("events"), cancellationToken);
                break;
            case "events":
                slots.SoSuKienMoiNgay = Math.Clamp(request.Number ?? ParseInt(spoken) ?? 3, 1, 4);
                slots.Step = "notes";
                await AddBot(chat, "Ghi chú / sở thích (ẩm thực, biển, nghỉ dưỡng…)? Gõ bỏ qua nếu không có.", null, cancellationToken);
                break;
            case "notes":
                if (!string.Equals(spoken, "bỏ qua", StringComparison.OrdinalIgnoreCase))
                    slots.GhiChu = spoken.Length == 0 ? slots.GhiChu : spoken;
                slots.TenChuyenDi ??= $"{slots.TenTinhXuatPhat} → {slots.TenTinhDen}";
                slots.Step = "confirm";
                await AddBot(chat, Summary(slots), ConfirmWidget(), cancellationToken);
                break;
            case "confirm":
                if (!request.Confirm && !spoken.Contains("đồng ý", StringComparison.OrdinalIgnoreCase) &&
                    !spoken.Contains("ok", StringComparison.OrdinalIgnoreCase) &&
                    !spoken.Contains("sinh", StringComparison.OrdinalIgnoreCase))
                {
                    await AddBot(chat, "Bấm xác nhận để tôi ghép 3 lịch từ CSDL, hoặc gõ làm lại.", ConfirmWidget(), cancellationToken);
                    break;
                }
                var yeuCau = await CreateRequestAsync(userId, slots, cancellationToken);
                chat.MaYeuCau = yeuCau.MaYeuCau;
                chat.TrangThai = FixedLengthHelper.PadTo20("DaSinhDeXuat");
                var extras = new DesignPlannerContext
                {
                    MaTinhXuatPhat = slots.MaTinhXuatPhat,
                    MaTinhDen = slots.MaTinhDen,
                    GioKhoiHanh = slots.GioKhoiHanh,
                    NgayKetThuc = slots.NgayKetThuc,
                    GioKetThuc = slots.GioKetThuc,
                    SoSuKienMoiNgay = slots.SoSuKienMoiNgay
                };
                var plans = await _planner.GenerateAsync(yeuCau, extras, cancellationToken);
                slots.Step = "done";
                var note = plans.Count == 0
                    ? "Chưa ghép được lịch — tỉnh đến chưa có khách sạn LuuTru hoặc điểm tham quan trong catalog. Sale sẽ xử lý trên trang yêu cầu."
                    : $"Đã ghép {plans.Count} phương án (tiết kiệm / cân bằng / cao cấp) từ khách sạn và điểm tại {slots.TenTinhDen}. Xem chi tiết bên dưới hoặc mở yêu cầu {FixedLengthHelper.TrimSafe(yeuCau.MaYeuCau)}.";
                await AddBot(chat, note, new { type = "proposals", maYeuCau = FixedLengthHelper.TrimSafe(yeuCau.MaYeuCau), soPhuongAn = plans.Count }, cancellationToken);
                break;
        }

        return await Persist(chat, slots, cancellationToken);
    }

    private async Task<string?> TryProvince(DesignChatRequest request, string spoken, bool origin, ChatSlots slots, CancellationToken cancellationToken)
    {
        var match = await _destinations.ResolveAsync(spoken, request.MaTinh, cancellationToken);
        if (match?.Province is null)
            return "Tôi chưa khớp được tỉnh/thành. Hãy chọn trong danh sách (gõ Nha Trang, Hà Nội, Đà Lạt…) để tránh nhập sai.";
        if (origin)
        {
            slots.MaTinhXuatPhat = match.Province.MaTinh;
            slots.TenTinhXuatPhat = match.Province.TenTinh;
        }
        else
        {
            slots.MaTinhDen = match.Province.MaTinh;
            slots.TenTinhDen = match.Province.TenTinh;
        }
        return null;
    }

    private async Task<YeuCauThietKe> CreateRequestAsync(string userId, ChatSlots slots, CancellationToken cancellationToken)
    {
        string maYeuCau;
        do
        {
            maYeuCau = FixedLengthHelper.PadTo20($"YC{Guid.NewGuid():N}"[..20].ToUpperInvariant());
        }
        while (await _context.YeuCauThietKes.AnyAsync(item => item.MaYeuCau == maYeuCau, cancellationToken));

        var days = 1;
        if (slots.NgayKhoiHanh is { } a && slots.NgayKetThuc is { } b && b >= a)
            days = Math.Clamp(b.DayNumber - a.DayNumber + 1, 1, 30);

        var yeuCau = new YeuCauThietKe
        {
            MaYeuCau = maYeuCau,
            MaUser = userId,
            DiemDenMongMuon = slots.TenTinhDen,
            NgayDuKienDi = slots.NgayKhoiHanh,
            SoNgay = days,
            SoNguoiLon = slots.SoNguoiLon ?? 1,
            SoTreEm = slots.SoTreEm ?? 0,
            NganSachDuKien = slots.NganSach,
            SoThichGhiChu = $"[{slots.TenChuyenDi}] {slots.MucDich}. Xuất phát {slots.TenTinhXuatPhat} {slots.GioKhoiHanh:hh\\:mm}. {slots.GhiChu}".Trim(),
            TrangThai = FixedLengthHelper.PadTo20("Moi"),
            NgayGui = DateTime.UtcNow
        };
        _context.YeuCauThietKes.Add(yeuCau);
        await _context.SaveChangesAsync(cancellationToken);
        return yeuCau;
    }

    private static bool TryDateTime(DesignChatRequest request, string spoken, out DateOnly date, out TimeSpan time)
    {
        date = default;
        time = new TimeSpan(8, 0, 0);
        if (DateOnly.TryParse(request.Date, out date))
        {
            if (TimeSpan.TryParse(request.Time, out var parsed))
                time = parsed;
            return true;
        }
        var match = Regex.Match(spoken, @"(\d{1,2})[/\-](\d{1,2})[/\-](\d{4})(?:\s+(\d{1,2}):(\d{2}))?");
        if (!match.Success)
            return false;
        date = new DateOnly(int.Parse(match.Groups[3].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[1].Value));
        if (match.Groups[4].Success)
            time = new TimeSpan(int.Parse(match.Groups[4].Value), int.Parse(match.Groups[5].Value), 0);
        return true;
    }

    private static bool TryPeople(DesignChatRequest request, string spoken, ChatSlots slots)
    {
        if (request.Number is > 0)
        {
            slots.SoNguoiLon = request.Number;
            slots.SoTreEm ??= 0;
            return true;
        }
        var adults = Regex.Match(spoken, @"(\d+)\s*(người lớn|nl|nguoi lon)", RegexOptions.IgnoreCase);
        var kids = Regex.Match(spoken, @"(\d+)\s*(trẻ em|te|tre em)", RegexOptions.IgnoreCase);
        var just = Regex.Match(spoken, @"(\d+)");
        if (adults.Success)
            slots.SoNguoiLon = int.Parse(adults.Groups[1].Value);
        if (kids.Success)
            slots.SoTreEm = int.Parse(kids.Groups[1].Value);
        if (slots.SoNguoiLon is null && just.Success)
            slots.SoNguoiLon = int.Parse(just.Groups[1].Value);
        slots.SoTreEm ??= 0;
        return (slots.SoNguoiLon ?? 0) + slots.SoTreEm > 0;
    }

    private static bool TryBudget(DesignChatRequest request, string spoken, ChatSlots slots)
    {
        if (request.Number is > 0)
        {
            slots.NganSach = request.Number;
            return true;
        }
        var million = Regex.Match(spoken.Replace(".", "").Replace(",", ""), @"(\d+)\s*(triệu|trieu|tr)", RegexOptions.IgnoreCase);
        if (million.Success)
        {
            slots.NganSach = int.Parse(million.Groups[1].Value) * 1_000_000;
            return slots.NganSach > 0;
        }
        var digits = Regex.Match(spoken.Replace(".", "").Replace(",", ""), @"(\d{5,})");
        if (digits.Success)
        {
            slots.NganSach = int.Parse(digits.Groups[1].Value);
            return true;
        }
        return false;
    }

    private static int? ParseInt(string spoken)
        => Regex.Match(spoken, @"(\d+)").Success ? int.Parse(Regex.Match(spoken, @"(\d+)").Groups[1].Value) : null;

    private static string NormalizePurpose(string? raw)
    {
        var text = (raw ?? "").Trim().ToLowerInvariant();
        if (text.Contains("công tác") || text.Contains("cong tac")) return "CongTac";
        if (text.Contains("nghỉ") || text.Contains("nghi duong")) return "NghiDuong";
        if (text.Contains("khám") || text.Contains("kham pha")) return "KhamPha";
        return "DuLich";
    }

    private static string Summary(ChatSlots slots)
    {
        var days = slots.NgayKhoiHanh is { } a && slots.NgayKetThuc is { } b ? b.DayNumber - a.DayNumber + 1 : 1;
        return $"Tóm tắt: {slots.TenTinhXuatPhat} → {slots.TenTinhDen}, {slots.NgayKhoiHanh:dd/MM/yyyy} {slots.GioKhoiHanh:hh\\:mm} đến {slots.NgayKetThuc:dd/MM/yyyy} {slots.GioKetThuc:hh\\:mm} ({days} ngày), {slots.SoNguoiLon} NL + {slots.SoTreEm} TE, ngân sách {slots.NganSach:N0} đ, ~{slots.SoSuKienMoiNgay} hoạt động/ngày.\n\nTôi sẽ tính thời gian di chuyển và ghép 3 lịch từ CSDL (không vượt 20:00, ngày về về đúng điểm xuất phát). Xác nhận nhé?";
    }

    private static object ProvinceWidget() => new { type = "province" };
    private static object DateTimeWidget(string role) => new { type = "datetime", role };
    private static object PeopleWidget() => new { type = "people" };
    private static object NumberWidget(string role) => new { type = "number", role };
    private static object PurposeWidget() => new { type = "purpose", options = new[] { "Du lịch", "Công tác", "Nghỉ dưỡng", "Khám phá" } };
    private static object ConfirmWidget() => new { type = "confirm" };

    private async Task<HoiThoaiThietKe?> LoadAsync(string maUser, string maHoiThoai, CancellationToken cancellationToken)
    {
        var id = FixedLengthHelper.PadTo20(maHoiThoai);
        var user = FixedLengthHelper.PadTo20(maUser);
        return await _context.HoiThoaiThietKes.Include(item => item.TinNhans)
            .FirstOrDefaultAsync(item => item.MaHoiThoai == id && item.MaUser == user, cancellationToken);
    }

    private async Task AddBot(HoiThoaiThietKe chat, string text, object? widget, CancellationToken cancellationToken)
        => await Add(chat, "Bot", text, widget, cancellationToken);

    private async Task AddGuest(HoiThoaiThietKe chat, string text, CancellationToken cancellationToken)
        => await Add(chat, "Khach", text, null, cancellationToken);

    private async Task Add(HoiThoaiThietKe chat, string role, string text, object? widget, CancellationToken cancellationToken)
    {
        chat.TinNhans.Add(new TinNhanThietKe
        {
            MaTinNhan = await NewIdAsync("TN", id => _context.TinNhanThietKes.AnyAsync(item => item.MaTinNhan == id, cancellationToken)),
            MaHoiThoai = chat.MaHoiThoai,
            VaiTro = FixedLengthHelper.PadTo20(role),
            NoiDung = text,
            PayloadJson = widget is null ? null : JsonSerializer.Serialize(widget, JsonOpts),
            NgayTao = DateTime.UtcNow
        });
    }

    private async Task<DesignChatTurn> Persist(HoiThoaiThietKe chat, ChatSlots slots, CancellationToken cancellationToken)
    {
        chat.DuLieuJson = JsonSerializer.Serialize(slots, JsonOpts);
        await _context.SaveChangesAsync(cancellationToken);
        await _context.Entry(chat).Collection(item => item.TinNhans).LoadAsync(cancellationToken);
        return ToTurn(chat, slots);
    }

    private static DesignChatTurn ToTurn(HoiThoaiThietKe chat, ChatSlots slots)
    {
        var lastWidget = chat.TinNhans.OrderBy(item => item.NgayTao).LastOrDefault(item => item.PayloadJson != null);
        return new DesignChatTurn
        {
            MaHoiThoai = FixedLengthHelper.TrimSafe(chat.MaHoiThoai) ?? "",
            TrangThai = FixedLengthHelper.TrimSafe(chat.TrangThai) ?? "DangHoi",
            MaYeuCau = FixedLengthHelper.TrimSafe(chat.MaYeuCau),
            Slots = slots,
            Widget = lastWidget?.PayloadJson is null ? null : JsonSerializer.Deserialize<JsonElement>(lastWidget.PayloadJson),
            Messages = chat.TinNhans.OrderBy(item => item.NgayTao).Select(item => (object)new
            {
                maTinNhan = FixedLengthHelper.TrimSafe(item.MaTinNhan),
                vaiTro = FixedLengthHelper.TrimSafe(item.VaiTro),
                noiDung = item.NoiDung,
                payload = item.PayloadJson is null ? (JsonElement?)null : JsonSerializer.Deserialize<JsonElement>(item.PayloadJson),
                ngayTao = item.NgayTao
            }).ToList()
        };
    }

    private static async Task<string> NewIdAsync(string prefix, Func<string, Task<bool>> exists)
    {
        string id;
        do { id = FixedLengthHelper.PadTo20($"{prefix}{Guid.NewGuid():N}"[..20].ToUpperInvariant()); }
        while (await exists(id));
        return id;
    }
}
