namespace TourDuLich.API.DTOs;

public class RegisterDto
{
    public string SoDienThoai { get; set; } = null!;
    public string MatKhau { get; set; } = null!;
    public int MaVaiTro { get; set; } = 1;
}