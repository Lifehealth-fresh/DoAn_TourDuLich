namespace TourDuLich.API.DTOs;

public class LichKhoiHanhCreateDto
{
    public string MaKhoiHanh { get; set; } = null!;
    public string MaTour { get; set; } = null!;
    public DateTime? NgayKhoiHanh { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public string? DiaDiem { get; set; }
}
