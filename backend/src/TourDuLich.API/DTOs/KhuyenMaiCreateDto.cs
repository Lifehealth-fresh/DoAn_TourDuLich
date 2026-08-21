namespace TourDuLich.API.DTOs;

public class KhuyenMaiCreateDto
{
    public string TenKm { get; set; } = null!;
    public string MaCode { get; set; } = null!;
    public DateTime NgayBd { get; set; }
    public DateTime NgayKt { get; set; }
    public string DonVi { get; set; } = null!;
    public int GiamGia { get; set; }
    public bool CoCongDon { get; set; }
    public string? MaNhomKm { get; set; }
}
