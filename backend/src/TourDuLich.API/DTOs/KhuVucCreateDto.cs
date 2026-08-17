namespace TourDuLich.API.DTOs;

public class KhuVucCreateDto
{
    public string MaKhuVuc { get; set; } = null!;
    public string? TenKhuVuc { get; set; }
    public string? QuocGia { get; set; }
    public decimal? ViDo { get; set; }
    public decimal? KinhDo { get; set; }
    public string? MuiGio { get; set; }
    public string? TrangThai { get; set; } = "HoatDong";
}
