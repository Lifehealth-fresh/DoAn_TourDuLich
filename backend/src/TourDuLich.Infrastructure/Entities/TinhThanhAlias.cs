namespace TourDuLich.Infrastructure.Entities;

public class TinhThanhAlias
{
    public int MaAlias { get; set; }
    public string MaTinh { get; set; } = null!;
    public string TenAlias { get; set; } = null!;
    public virtual TinhThanh MaTinhNavigation { get; set; } = null!;
}
