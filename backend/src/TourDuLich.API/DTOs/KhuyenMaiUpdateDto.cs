namespace TourDuLich.API.DTOs;

public class KhuyenMaiUpdateDto
{
    public string? TenKm { get; set; }
    public string? MaCode { get; set; }
    public DateTime? NgayBd { get; set; }
    public DateTime? NgayKt { get; set; }
    public string? DonVi { get; set; }
    public int? GiamGia { get; set; }
    public bool? CoCongDon { get; set; }
    public string? MaNhomKm { get; set; }
    public string? TrangThai { get; set; }
}
