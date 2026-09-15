namespace TourDuLich.Infrastructure.Entities;

public partial class NhanVien
{
    public string MaNhanVien { get; set; } = null!;
    public string MaUser { get; set; } = null!;
    public string Ho { get; set; } = null!;
    public string Ten { get; set; } = null!;
    public string SoCccd { get; set; } = null!;
    public string ChucVu { get; set; } = null!;
    public virtual NguoiSuDung MaUserNavigation { get; set; } = null!;
}
