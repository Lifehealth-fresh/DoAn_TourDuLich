namespace TourDuLich.API.DTOs;

public class LichTrinhCreateDto
{
    public string MaTour { get; set; } = null!;
    public int NgayThu { get; set; }
    public int ThuTuTrongNgay { get; set; }
    public string? MaDthamQuan { get; set; }
    public string? MaSanPham { get; set; }
    public int SoLuong { get; set; }
    public string? Mota { get; set; }
}