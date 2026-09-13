namespace TourDuLich.Infrastructure.Entities;

public class QuyenNhanVien
{
    public string MaQuyen { get; set; } = null!;
    public string MaUser { get; set; } = null!;
    public string ChucNang { get; set; } = null!;
    public bool Them { get; set; }
    public bool Sua { get; set; }
    public bool Xoa { get; set; }
    public bool ToanQuyen { get; set; }

    public virtual NguoiSuDung MaUserNavigation { get; set; } = null!;
}
