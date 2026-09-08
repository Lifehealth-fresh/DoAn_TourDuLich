namespace TourDuLich.API.DTOs;

public class DanhGiaSanPhamDoiTacCreateDto
{
    public string MaSanPham { get; set; } = null!;
    public int SaoDanhGia { get; set; }
    public string? NhanXet { get; set; }
    public List<MediaItemDto>? MediaUrls { get; set; }
}
