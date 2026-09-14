namespace TourDuLich.Infrastructure.Entities;

public partial class RefreshToken
{
    public string MaRefresh { get; set; } = null!;

    public string MaUser { get; set; } = null!;

    public string TokenHash { get; set; } = null!;

    public DateTime HetHan { get; set; }

    public DateTime? ThuHoiLuc { get; set; }

    public DateTime TaoLuc { get; set; }

    public virtual NguoiSuDung MaUserNavigation { get; set; } = null!;
}
