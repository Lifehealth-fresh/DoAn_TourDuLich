namespace TourDuLich.API.DTOs;

public class DanhGiaTourCreateDto
{
    public string MaTour { get; set; } = null!;
    public int SaoDanhGia { get; set; }
    public string? NhanXet { get; set; }
}
