namespace TourDuLich.API.DTOs;

public class TourUpdateDto
{
    public string TenTour { get; set; } = null!;
    public string? Mota { get; set; }
    public int? ThoiGian { get; set; }
    public string? DieuKhoan { get; set; }
    public int GiaTour { get; set; }
    public int Slkhach { get; set; }
    public int? SlhuongDanVien { get; set; }
    public string LoaiTour { get; set; } = null!;
    public string? TrangThai { get; set; }
}