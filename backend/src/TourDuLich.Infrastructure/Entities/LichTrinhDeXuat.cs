namespace TourDuLich.Infrastructure.Entities;

public partial class LichTrinhDeXuat
{
    public string MaDeXuat { get; set; } = null!;
    public string MaYeuCau { get; set; } = null!;
    public int ThuTuPhuongAn { get; set; }
    public string TenPhuongAn { get; set; } = null!;
    public int TongTienDuKien { get; set; }
    public string? GhiChu { get; set; }
    public string TrangThai { get; set; } = "DeXuat";
    public DateTime NgayTao { get; set; }
    public virtual YeuCauThietKe MaYeuCauNavigation { get; set; } = null!;
    public virtual ICollection<LichTrinhDeXuatChiTiet> ChiTiets { get; set; } = new List<LichTrinhDeXuatChiTiet>();
}
