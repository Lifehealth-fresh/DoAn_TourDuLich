namespace TourDuLich.API.Authorization;

public static class PermissionCatalog
{
    public const string Them = "Them";
    public const string Sua = "Sua";
    public const string Xoa = "Xoa";
    public const string Xem = "Xem";

    public const string TongQuan = "TongQuan";
    public const string Tour = "Tour";
    public const string Booking = "Booking";
    public const string UuDai = "UuDai";
    public const string DiemThamQuan = "DiemThamQuan";
    public const string DoiTac = "DoiTac";
    public const string ThietKe = "ThietKe";
    public const string DanhGia = "DanhGia";
    public const string TaiKhoan = "TaiKhoan";

    public static readonly IReadOnlyList<PermissionModule> Modules =
    [
        new(TongQuan, "Tổng quan"),
        new(Tour, "Quản lý tour"),
        new(Booking, "Quản lý booking"),
        new(UuDai, "Quản lý ưu đãi"),
        new(DiemThamQuan, "Điểm tham quan"),
        new(DoiTac, "Đối tác"),
        new(ThietKe, "Thiết kế"),
        new(DanhGia, "Đánh giá nhận xét"),
        new(TaiKhoan, "Tài khoản")
    ];

    public static bool IsKnown(string? chucNang) =>
        Modules.Any(module => string.Equals(module.Ma, chucNang, StringComparison.OrdinalIgnoreCase));

    public static string DeniedMessage(string chucNang, string hanhDong)
    {
        var noun = chucNang switch
        {
            Tour => "tour",
            Booking => "booking",
            UuDai => "ưu đãi",
            DiemThamQuan => "điểm tham quan",
            DoiTac => "đối tác",
            ThietKe => "yêu cầu thiết kế",
            DanhGia => "đánh giá nhận xét",
            TaiKhoan => "tài khoản",
            TongQuan => "trang tổng quan",
            _ => chucNang.ToLowerInvariant()
        };
        var verb = hanhDong switch
        {
            Them => "thêm",
            Sua => "sửa",
            Xoa => "xóa",
            _ => "sử dụng"
        };
        return $"Bạn bị hạn chế quyền: không được {verb} {noun}.";
    }
}

public sealed record PermissionModule(string Ma, string Ten);
