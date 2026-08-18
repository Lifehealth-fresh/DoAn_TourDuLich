namespace TourDuLich.API.DTOs;

public class KhachHangUpdateDto
{
    public string Ho { get; set; } = null!;
    public string Ten { get; set; } = null!;
    public string? HoGiayTo { get; set; }
    public string? TenGiayTo { get; set; }
    public string? QuocTich { get; set; }
    public string? DanhXung { get; set; }
    public string? GioiTinh { get; set; }
    public DateOnly? NgaySinh { get; set; }
    public string? Email { get; set; }
}