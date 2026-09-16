using System.Text.Json;
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

    public DesignChatService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DesignChatTurn> GetAsync(string maUser, string maHoiThoai, CancellationToken cancellationToken = default)
    {
        var chat = await LoadAsync(maUser, maHoiThoai, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy hội thoại.");
        return ToTurn(chat);
    }

    public async Task<DesignChatTurn> StartOrContinueAsync(string maUser, DesignChatRequest request, CancellationToken cancellationToken = default)
    {
        var userId = FixedLengthHelper.PadTo20(maUser);
        HoiThoaiThietKe chat;
        if (string.IsNullOrWhiteSpace(request.MaHoiThoai))
        {
            chat = new HoiThoaiThietKe
            {
                MaHoiThoai = await NewIdAsync("HT", id => _context.HoiThoaiThietKes.AnyAsync(item => item.MaHoiThoai == id, cancellationToken)),
                MaUser = userId,
                TrangThai = FixedLengthHelper.PadTo20("DangHoi"),
                DuLieuJson = "{}",
                NgayTao = DateTime.UtcNow
            };
            _context.HoiThoaiThietKes.Add(chat);
            await AddBot(chat,
                "Xin chào, tôi là trợ lý ANAM. Hỏi tôi cách đặt tour, thanh toán, hủy đơn, tự thiết kế, gợi ý AI…\nLịch trình tự thiết kế nằm ở trang Tự thiết kế (form chọn tỉnh), không nhập trong chat này.",
                cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return ToTurn(chat);
        }

        chat = await LoadAsync(maUser, request.MaHoiThoai, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy hội thoại.");
        var spoken = (request.Message ?? string.Empty).Trim();
        if (spoken.Length == 0)
            return ToTurn(chat);
        if (spoken.Equals("làm lại", StringComparison.OrdinalIgnoreCase))
        {
            chat.TinNhans.Clear();
            await AddBot(chat, "Đã xóa hội thoại. Bạn cần hướng dẫn thao tác nào?", cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return ToTurn(chat);
        }

        await AddGuest(chat, spoken, cancellationToken);
        var reply = Faq(spoken);
        await AddBot(chat, reply, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return ToTurn(chat);
    }

    private static string Faq(string spoken)
    {
        var t = VietnameseText.Fold(spoken);
        if (t.Contains("thiet ke") || t.Contains("lich trinh") && t.Contains("tu"))
            return "Tự thiết kế: vào menu Tự thiết kế → chọn tỉnh xuất phát và tỉnh đến → ngày/giờ đi, số ngày, số khách, ngân sách, mục đích → Gửi yêu cầu. Hệ thống ghép 3 lịch từ CSDL. Chat này chỉ hướng dẫn, không thay form.";
        if (t.Contains("dat tour") || t.Contains("dat cho") || t.Contains("booking"))
            return "Đặt tour: Khám phá → chọn tour → chọn ngày khởi hành còn chỗ → điền khách → thanh toán cọc hoặc toàn phần. Theo dõi ở Chuyến đi của tôi.";
        if (t.Contains("thanh toan") || t.Contains("vnpay") || t.Contains("momo"))
            return "Thanh toán trong Chuyến đi của tôi. Hỗ trợ VNPay, MoMo, chuyển khoản. Giữ đúng số tiền và nội dung.";
        if (t.Contains("huy") || t.Contains("hoan tien"))
            return "Hủy đơn khi trạng thái còn cho phép (Chuyến đi của tôi). Hoàn tiền theo chính sách ghi trên đơn.";
        if (t.Contains("goi y") || t.Contains("ai"))
            return "Gợi ý AI (menu Gợi ý AI) đề xuất tour có sẵn theo sở thích. Khác với Tự thiết kế (bạn tự khai báo rồi hệ thống ghép lịch).";
        if (t.Contains("dang nhap") || t.Contains("mat khau"))
            return "Đăng nhập bằng SĐT 10 số bắt đầu 0 và mật khẩu (chữ + số, ≥ 8 ký tự).";
        if (t.Contains("nha trang") || t.Contains("da lat") || t.Contains("da nang") || t.Contains("ha noi"))
            return "Muốn đi tỉnh đó: (1) Khám phá tour có sẵn, hoặc (2) Tự thiết kế → chọn đúng tỉnh trên danh sách (gõ không dấu vẫn ra). Chat không sinh lịch.";
        return "Bạn có thể hỏi: đặt tour, thanh toán, hủy, tự thiết kế, gợi ý AI, đăng nhập. Hoặc gõ tên tỉnh để biết nên dùng tour có sẵn hay form tự thiết kế.";
    }

    private async Task<HoiThoaiThietKe?> LoadAsync(string maUser, string maHoiThoai, CancellationToken cancellationToken)
    {
        var id = FixedLengthHelper.PadTo20(maHoiThoai);
        var user = FixedLengthHelper.PadTo20(maUser);
        return await _context.HoiThoaiThietKes.Include(item => item.TinNhans)
            .FirstOrDefaultAsync(item => item.MaHoiThoai == id && item.MaUser == user, cancellationToken);
    }

    private async Task AddBot(HoiThoaiThietKe chat, string text, CancellationToken cancellationToken)
        => await Add(chat, "Bot", text, cancellationToken);

    private async Task AddGuest(HoiThoaiThietKe chat, string text, CancellationToken cancellationToken)
        => await Add(chat, "Khach", text, cancellationToken);

    private async Task Add(HoiThoaiThietKe chat, string role, string text, CancellationToken cancellationToken)
    {
        chat.TinNhans.Add(new TinNhanThietKe
        {
            MaTinNhan = await NewIdAsync("TN", id => _context.TinNhanThietKes.AnyAsync(item => item.MaTinNhan == id, cancellationToken)),
            MaHoiThoai = chat.MaHoiThoai,
            VaiTro = FixedLengthHelper.PadTo20(role),
            NoiDung = text,
            NgayTao = DateTime.UtcNow
        });
    }

    private static DesignChatTurn ToTurn(HoiThoaiThietKe chat) => new()
    {
        MaHoiThoai = FixedLengthHelper.TrimSafe(chat.MaHoiThoai) ?? "",
        TrangThai = FixedLengthHelper.TrimSafe(chat.TrangThai) ?? "DangHoi",
        MaYeuCau = FixedLengthHelper.TrimSafe(chat.MaYeuCau),
        Messages = chat.TinNhans.OrderBy(item => item.NgayTao).Select(item => (object)new
        {
            maTinNhan = FixedLengthHelper.TrimSafe(item.MaTinNhan),
            vaiTro = FixedLengthHelper.TrimSafe(item.VaiTro),
            noiDung = item.NoiDung,
            ngayTao = item.NgayTao
        }).ToList()
    };

    private static async Task<string> NewIdAsync(string prefix, Func<string, Task<bool>> exists)
    {
        string id;
        do { id = FixedLengthHelper.PadTo20($"{prefix}{Guid.NewGuid():N}"[..20].ToUpperInvariant()); }
        while (await exists(id));
        return id;
    }
}
