namespace TourDuLich.Infrastructure.Entities;

public class MatranDiChuyen
{
    public string MaTinhDi { get; set; } = null!;
    public string MaTinhDen { get; set; } = null!;
    public string PhuongTien { get; set; } = null!;
    public int SoPhut { get; set; }
    public int? ChiPhiUocTinh { get; set; }
}
