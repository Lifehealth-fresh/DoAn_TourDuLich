namespace TourDuLich.API.DTOs;

public class DanhGiaHdvCreateDto
{
    public string MaHdv { get; set; } = null!;
    public int SaoDanhGia { get; set; }
    public string? NhanXet { get; set; }
    public List<MediaItemDto>? MediaUrls { get; set; }
}
