namespace TourDuLich.Infrastructure.Entities;

public class JobRunLog
{
    public long MaJobRun { get; set; }
    public string TenJob { get; set; } = null!;
    public DateTime ThoiDiemBatDau { get; set; }
    public DateTime? ThoiDiemKetThuc { get; set; }
    public int SoBanGhi { get; set; }
    public string TrangThai { get; set; } = null!;
    public string? Loi { get; set; }
}
