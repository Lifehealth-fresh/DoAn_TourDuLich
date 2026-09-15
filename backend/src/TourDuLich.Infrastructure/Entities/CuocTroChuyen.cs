namespace TourDuLich.Infrastructure.Entities;

public partial class CuocTroChuyen
{
    public string MaCuoc { get; set; } = null!;
    public string MaUserKhach { get; set; } = null!;
    public string? MaUserNhanVien { get; set; }
    public string TieuDe { get; set; } = null!;
    public string TrangThai { get; set; } = "Mo";
    public DateTime ThoiGianTao { get; set; }
    public DateTime ThoiGianCapNhat { get; set; }
    public virtual NguoiSuDung MaUserKhachNavigation { get; set; } = null!;
    public virtual NguoiSuDung? MaUserNhanVienNavigation { get; set; }
    public virtual ICollection<TinNhanHoTro> TinNhanHoTros { get; set; } = new List<TinNhanHoTro>();
}
