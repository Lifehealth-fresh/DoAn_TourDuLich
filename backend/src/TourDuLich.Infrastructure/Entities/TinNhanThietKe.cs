namespace TourDuLich.Infrastructure.Entities;

public class TinNhanThietKe
{
    public string MaTinNhan { get; set; } = null!;
    public string MaHoiThoai { get; set; } = null!;
    public string VaiTro { get; set; } = null!;
    public string NoiDung { get; set; } = null!;
    public string? PayloadJson { get; set; }
    public DateTime NgayTao { get; set; }
    public virtual HoiThoaiThietKe MaHoiThoaiNavigation { get; set; } = null!;
}
