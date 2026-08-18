namespace TourDuLich.API.DTOs;

public class ThanhToanCreateDto
{
    public string MaBooking { get; set; } = null!;
    public int SoTien { get; set; }
    public string PhuongThuc { get; set; } = null!;
    public string LoaiThanhToan { get; set; } = null!;
}