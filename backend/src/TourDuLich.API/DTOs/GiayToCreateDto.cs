namespace TourDuLich.API.DTOs;

public class GiayToCreateDto
{
    public string LoaiGiayTo { get; set; } = null!;
    public string SoTrenGiayTo { get; set; } = null!;
    public DateOnly NgayCap { get; set; }
    public DateOnly NgayHetHan { get; set; }
    public string NoiCap { get; set; } = null!;
}