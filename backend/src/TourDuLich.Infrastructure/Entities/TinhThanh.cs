namespace TourDuLich.Infrastructure.Entities;

public partial class TinhThanh
{
    public string MaTinh { get; set; } = null!;
    public string TenTinh { get; set; } = null!;
    public string MaKhuVuc { get; set; } = null!;
    public virtual KhuVuc MaKhuVucNavigation { get; set; } = null!;
    public virtual ICollection<DiemThamQuan> DiemThamQuans { get; set; } = new List<DiemThamQuan>();
    public virtual ICollection<DoiTac> DoiTacs { get; set; } = new List<DoiTac>();
    public virtual ICollection<TinhThanhAlias> Aliases { get; set; } = new List<TinhThanhAlias>();
}
