using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.Application.Services;

/// <summary>Công cụ CSDL cho LLM — model không được tự bịa ID/giá.</summary>
public sealed class PlannerCatalog
{
    private readonly AppDbContext _context;
    private readonly ITravelTimeService _travel;
    private readonly IDestinationResolver _destinations;

    public PlannerCatalog(AppDbContext context, ITravelTimeService travel, IDestinationResolver destinations)
    {
        _context = context;
        _travel = travel;
        _destinations = destinations;
    }

    public async Task<JsonElement> ExecuteAsync(string name, JsonElement args, CancellationToken cancellationToken)
    {
        return name switch
        {
            "search_destinations" or "search_sights" => await SearchSights(args, cancellationToken),
            "search_hotels" => await SearchHotels(args, cancellationToken),
            "search_meals" => await SearchMeals(args, cancellationToken),
            "search_tours" => await SearchTours(args, cancellationToken),
            "calculate_route" => await CalculateRoute(args, cancellationToken),
            "search_provinces" => await SearchProvinces(args, cancellationToken),
            "how_to" => HowTo(args),
            _ => JsonSerializer.SerializeToElement(new { error = $"Không có function {name}" })
        };
    }

    public static object[] PlannerDeclarations() =>
    [
        Fn("search_sights", "Điểm tham quan trong CSDL cùng tỉnh.", new
        {
            type = "object",
            properties = new
            {
                city = new { type = "string", description = "Tên tỉnh hoặc mã TN.." },
                keyword = new { type = "string" }
            },
            required = new[] { "city" }
        }),
        Fn("search_hotels", "Khách sạn/phòng đang hoạt động cùng tỉnh.", new
        {
            type = "object",
            properties = new { city = new { type = "string" }, maxPrice = new { type = "integer" } },
            required = new[] { "city" }
        }),
        Fn("search_meals", "Nhà hàng cùng tỉnh.", new
        {
            type = "object",
            properties = new { city = new { type = "string" } },
            required = new[] { "city" }
        }),
        Fn("calculate_route", "Thời gian/chi phí giữa hai tỉnh (ma trận hoặc Haversine, không Google).", new
        {
            type = "object",
            properties = new
            {
                origin = new { type = "string" },
                destination = new { type = "string" }
            },
            required = new[] { "origin", "destination" }
        })
    ];

    public static object[] SupportDeclarations() =>
    [
        Fn("search_tours", "Tour có sẵn đang mở bán.", new
        {
            type = "object",
            properties = new { keyword = new { type = "string" } }
        }),
        Fn("search_provinces", "Tìm tỉnh/thành.", new
        {
            type = "object",
            properties = new { query = new { type = "string" } }
        }),
        Fn("search_sights", "Điểm tham quan theo tỉnh.", new
        {
            type = "object",
            properties = new { city = new { type = "string" } },
            required = new[] { "city" }
        }),
        Fn("how_to", "Hướng dẫn thao tác trên website.", new
        {
            type = "object",
            properties = new
            {
                topic = new { type = "string", description = "dat_tour | tu_thiet_ke | thanh_toan | huy | dang_nhap | goi_y" }
            },
            required = new[] { "topic" }
        })
    ];

    private static object Fn(string name, string description, object parameters) =>
        new { name, description, parameters };

    private async Task<JsonElement> SearchSights(JsonElement args, CancellationToken cancellationToken)
    {
        var city = Str(args, "city", "keyword");
        var match = await _destinations.ResolveAsync(city, null, cancellationToken);
        if (match?.Province is null)
            return Json("error", "Không khớp tỉnh. Hãy dùng tên tỉnh Việt Nam.");
        var province = match.Province.MaTinh;
        var keyword = VietnameseText.Fold(Str(args, "keyword"));
        var rows = await _context.DiemThamQuans.AsNoTracking()
            .Where(item => item.MaTinh == province)
            .OrderBy(item => item.MaDthamQuan)
            .Take(40)
            .Select(item => new
            {
                id = item.MaDthamQuan.Trim(),
                name = item.TenDiaDanh,
                address = item.DiaChi,
                description = item.Mota
            })
            .ToListAsync(cancellationToken);
        if (!string.IsNullOrEmpty(keyword))
            rows = rows.Where(item => VietnameseText.ContainsFold($"{item.name} {item.address} {item.description}", keyword)).ToList();
        return JsonSerializer.SerializeToElement(new { province = match.Label, items = rows });
    }

    private async Task<JsonElement> SearchHotels(JsonElement args, CancellationToken cancellationToken)
    {
        var match = await _destinations.ResolveAsync(Str(args, "city"), null, cancellationToken);
        if (match?.Province is null)
            return Json("error", "Không khớp tỉnh.");
        var max = args.TryGetProperty("maxPrice", out var p) && p.TryGetInt32(out var n) ? n : int.MaxValue;
        var active = FixedLengthHelper.PadTo20("HoatDong");
        var rows = await _context.SanPhamDoiTacs.AsNoTracking()
            .Include(item => item.MaDoiTacNavigation)
            .Where(item => item.MaDoiTacNavigation.MaTinh == match.Province.MaTinh
                           && (item.TrangThai == null || item.TrangThai == active))
            .ToListAsync(cancellationToken);
        var hotels = rows.Where(HotelStayRules.IsHotelProduct)
            .Where(item => item.GiaNiemYet <= max)
            .OrderBy(item => item.GiaNiemYet)
            .Take(20)
            .Select(item => new
            {
                id = item.MaSanPham.Trim(),
                name = item.TenSanPham,
                partner = item.MaDoiTacNavigation.TenDoiTac,
                pricePerNight = item.GiaNiemYet,
                address = item.MaDoiTacNavigation.DiaChi
            });
        return JsonSerializer.SerializeToElement(new { province = match.Label, items = hotels });
    }

    private async Task<JsonElement> SearchMeals(JsonElement args, CancellationToken cancellationToken)
    {
        var match = await _destinations.ResolveAsync(Str(args, "city"), null, cancellationToken);
        if (match?.Province is null)
            return Json("error", "Không khớp tỉnh.");
        var active = FixedLengthHelper.PadTo20("HoatDong");
        var rows = await _context.SanPhamDoiTacs.AsNoTracking()
            .Include(item => item.MaDoiTacNavigation)
            .Where(item => item.MaDoiTacNavigation.MaTinh == match.Province.MaTinh
                           && (item.TrangThai == null || item.TrangThai == active))
            .ToListAsync(cancellationToken);
        var meals = rows.Where(item => HotelStayRules.IsDining(item.MaDoiTacNavigation?.LoaiDoiTac))
            .OrderBy(item => item.GiaNiemYet)
            .Take(20)
            .Select(item => new
            {
                id = item.MaSanPham.Trim(),
                name = item.TenSanPham,
                partner = item.MaDoiTacNavigation.TenDoiTac,
                price = item.GiaNiemYet
            });
        return JsonSerializer.SerializeToElement(new { province = match.Label, items = meals });
    }

    private async Task<JsonElement> SearchTours(JsonElement args, CancellationToken cancellationToken)
    {
        var keyword = VietnameseText.Fold(Str(args, "keyword"));
        var active = FixedLengthHelper.PadTo20("HoatDong");
        var query = _context.Tours.AsNoTracking().Where(item => item.TrangThai == active);
        var rows = await query.OrderBy(item => item.MaTour).Take(80).Select(item => new
        {
            id = item.MaTour.Trim(),
            name = item.TenTour,
            days = item.ThoiGian,
            price = item.GiaTour,
            description = item.Mota
        }).ToListAsync(cancellationToken);
        if (!string.IsNullOrEmpty(keyword))
            rows = rows.Where(item => VietnameseText.ContainsFold($"{item.name} {item.description}", keyword)).Take(15).ToList();
        else
            rows = rows.Take(12).ToList();
        return JsonSerializer.SerializeToElement(new { items = rows });
    }

    private async Task<JsonElement> SearchProvinces(JsonElement args, CancellationToken cancellationToken)
    {
        var rows = await _destinations.SearchAsync(Str(args, "query", "city"), cancellationToken);
        return JsonSerializer.SerializeToElement(rows.Take(20).Select(item => new
        {
            id = item.MaTinh.Trim(),
            name = item.TenTinh
        }));
    }

    private async Task<JsonElement> CalculateRoute(JsonElement args, CancellationToken cancellationToken)
    {
        var origin = await _destinations.ResolveAsync(Str(args, "origin"), null, cancellationToken);
        var dest = await _destinations.ResolveAsync(Str(args, "destination"), null, cancellationToken);
        if (origin?.Province is null || dest?.Province is null)
            return Json("error", "Không khớp tỉnh xuất phát hoặc tỉnh đến.");
        var legs = await _travel.EstimateAsync(origin.Province, dest.Province, cancellationToken);
        return JsonSerializer.SerializeToElement(new
        {
            origin = origin.Label,
            destination = dest.Label,
            options = legs.Select(item => new
            {
                mode = item.Mode,
                minutes = item.Minutes,
                cost = item.Cost,
                source = item.Source
            })
        });
    }

    private static JsonElement HowTo(JsonElement args)
    {
        var topic = VietnameseText.Fold(Str(args, "topic"));
        var text = topic switch
        {
            var t when t.Contains("dat") || t.Contains("booking") =>
                "Đặt tour: Khám phá → chọn tour → chọn lịch khởi hành → điền khách → thanh toán cọc hoặc toàn phần (VNPay/MoMo/chuyển khoản). Xem tiến độ tại Chuyến đi của tôi.",
            var t when t.Contains("thiet") || t.Contains("design") =>
                "Tự thiết kế: vào mục Tự thiết kế, chọn tỉnh xuất phát / tỉnh đến (không gõ tự do), ngày giờ đi, số ngày, số khách, ngân sách, mục đích rồi Gửi yêu cầu. Hệ thống ghép 3 lịch từ CSDL. Bạn chọn 1 phương án → Sale chỉnh → bạn đồng ý → Admin duyệt → đặt tour.",
            var t when t.Contains("thanh") || t.Contains("pay") =>
                "Thanh toán: mở Chuyến đi của tôi → thanh toán. Có thể cọc hoặc trả hết. Giữ đúng số tiền và nội dung chuyển khoản.",
            var t when t.Contains("huy") || t.Contains("cancel") =>
                "Hủy: Chuyến đi của tôi → hủy đơn (khi trạng thái còn cho phép). Hoàn tiền theo chính sách trên đơn.",
            var t when t.Contains("dang") || t.Contains("login") =>
                "Đăng nhập bằng số điện thoại 10 số (0…) và mật khẩu. Quên mật khẩu: liên hệ Sale/Admin.",
            var t when t.Contains("goi") || t.Contains("ai") =>
                "Gợi ý AI: đăng nhập → Gợi ý AI. Hệ thống gợi tour có sẵn theo hành vi, không phải tự thiết kế.",
            _ => "Bạn có thể: (1) Khám phá tour có sẵn, (2) Tự thiết kế form tỉnh/ngày/ngân sách, (3) Gợi ý AI, (4) Ưu đãi, (5) Chuyến đi của tôi để thanh toán/hủy."
        };
        return Json("guide", text);
    }

    private static string Str(JsonElement args, params string[] names)
    {
        foreach (var name in names)
        {
            if (args.ValueKind == JsonValueKind.Object && args.TryGetProperty(name, out var value))
                return value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : value.ToString();
        }
        return "";
    }

    private static JsonElement Json(string key, string value) =>
        JsonSerializer.SerializeToElement(new Dictionary<string, string> { [key] = value });
}
