namespace TourDuLich.API.DTOs;

public class DanhGiaTourUpdateDto
{
    public int SaoDanhGia { get; set; }
    public string? NhanXet { get; set; }
    public List<MediaItemDto>? MediaUrls { get; set; }
}
