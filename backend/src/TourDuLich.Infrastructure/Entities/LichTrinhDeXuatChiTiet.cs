namespace TourDuLich.Infrastructure.Entities;

public partial class LichTrinhDeXuatChiTiet
{
    public string MaChiTiet { get; set; } = null!;
    public string MaDeXuat { get; set; } = null!;
    public int NgayThu { get; set; }
    public int ThuTuTrongNgay { get; set; }
    public string? MaDthamQuan { get; set; }
    public string? MaSanPham { get; set; }
    public int SoLuong { get; set; }
    public int DonGia { get; set; }
    public int ThanhTien { get; set; }
    public string? Mota { get; set; }
    public virtual LichTrinhDeXuat MaDeXuatNavigation { get; set; } = null!;
    public virtual DiemThamQuan? MaDthamQuanNavigation { get; set; }
    public virtual SanPhamDoiTac? MaSanPhamNavigation { get; set; }
}
