namespace TourDuLich.API.DTOs;

public class AnhTourCreateDto
{
    public string MaTour { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string LoaiMedia { get; set; } = "Anh";
    public int? ThuTu { get; set; }
    public bool? IsAvatar { get; set; }
}
