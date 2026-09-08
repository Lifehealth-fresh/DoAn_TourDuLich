namespace TourDuLich.Infrastructure.Entities;

public partial class MediaDanhGiaHdv
{
    public string MaMedia { get; set; } = null!;
    public string MaDanhGiaHdv { get; set; } = null!;
    public string LoaiMedia { get; set; } = "Anh";
    public string Url { get; set; } = null!;
    public int? ThuTu { get; set; }
    public virtual DanhGiaHdv MaDanhGiaHdvNavigation { get; set; } = null!;
}
