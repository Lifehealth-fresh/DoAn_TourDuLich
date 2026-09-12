namespace TourDuLich.API.DTOs;

public class LichKhoiHanhUpdateDto
{
    public DateTime? NgayKhoiHanh { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public string? DiaDiem { get; set; }
    [System.ComponentModel.DataAnnotations.Range(0, int.MaxValue)]
    public int? SoCho { get; set; }
}
