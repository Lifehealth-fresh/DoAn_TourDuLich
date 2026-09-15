namespace TourDuLich.Infrastructure.Entities;

public partial class MediaDanhGiaTour
{
    public string MaMedia { get; set; } = null!;
    public string MaDanhGiaTour { get; set; } = null!;
    public string LoaiMedia { get; set; } = "Anh";
    public string Url { get; set; } = null!;
    public string? CloudPublicId { get; set; }
    public string? CloudResourceType { get; set; }
    public int? ThuTu { get; set; }
    public virtual DanhGiaTour MaDanhGiaTourNavigation { get; set; } = null!;
}
