namespace TourDuLich.Infrastructure.Entities;

public partial class TinNhanHoTro
{
    public string MaTinNhan { get; set; } = null!;
    public string MaCuoc { get; set; } = null!;
    public string MaUserGui { get; set; } = null!;
    public string VaiTroGui { get; set; } = null!;
    public string NoiDung { get; set; } = null!;
    public DateTime ThoiGian { get; set; }
    public bool DaDoc { get; set; }
    public virtual CuocTroChuyen MaCuocNavigation { get; set; } = null!;
    public virtual NguoiSuDung MaUserGuiNavigation { get; set; } = null!;
}
