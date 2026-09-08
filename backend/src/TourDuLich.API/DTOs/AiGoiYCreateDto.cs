namespace TourDuLich.API.DTOs;

public class AiGoiYCreateDto
{
    public string? MaUser { get; set; }
    public int SoLuong { get; set; } = 5;
    public double Alpha { get; set; } = 0.5;
}
