namespace TourDuLich.API.DTOs;

public class DatDichVuCreateDto
{
    public string MaTour { get; set; } = null!;
    public string MaKhoiHanh { get; set; } = null!;
    public string? MaKhachHang { get; set; }
    public int SlnguoiLon { get; set; }
    public int SltreEm { get; set; }
}
