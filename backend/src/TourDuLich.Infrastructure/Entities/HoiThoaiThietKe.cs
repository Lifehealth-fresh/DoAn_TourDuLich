namespace TourDuLich.Infrastructure.Entities;

public class HoiThoaiThietKe
{
    public string MaHoiThoai { get; set; } = null!;
    public string MaUser { get; set; } = null!;
    public string? MaYeuCau { get; set; }
    public string TrangThai { get; set; } = null!;
    public string? DuLieuJson { get; set; }
    public DateTime NgayTao { get; set; }
    public virtual NguoiSuDung MaUserNavigation { get; set; } = null!;
    public virtual YeuCauThietKe? MaYeuCauNavigation { get; set; }
    public virtual ICollection<TinNhanThietKe> TinNhans { get; set; } = new List<TinNhanThietKe>();
}
