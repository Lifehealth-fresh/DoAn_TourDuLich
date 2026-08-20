namespace TourDuLich.API.DTOs;

public class DoiTacUpdateDto
{
    public string TenDoiTac { get; set; } = null!;
    public string LoaiDoiTac { get; set; } = null!;
    public string? NguoiLienHe { get; set; }
    public string? SoDienThoai { get; set; }
    public string? Email { get; set; }
    public string? MaKhuVuc { get; set; }
    public decimal? PhanTramHoaHong { get; set; }
    public string? TrangThai { get; set; }
}