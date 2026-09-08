namespace TourDuLich.Infrastructure.Entities;

public partial class MediaDanhGiaSanPham
{
    public string MaMedia { get; set; } = null!;
    public string MaDanhGia { get; set; } = null!;
    public string LoaiMedia { get; set; } = "Anh";
    public string Url { get; set; } = null!;
    public int? ThuTu { get; set; }
    public virtual DanhGiaSanPhamDoiTac MaDanhGiaNavigation { get; set; } = null!;
}
