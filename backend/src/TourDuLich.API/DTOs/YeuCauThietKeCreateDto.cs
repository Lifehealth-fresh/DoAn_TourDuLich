namespace TourDuLich.API.DTOs;

public class YeuCauThietKeCreateDto
{
    public string? DiemDenMongMuon { get; set; }
    public DateOnly? NgayDuKienDi { get; set; }
    public int? SoNgay { get; set; }
    public int SoNguoiLon { get; set; }
    public int SoTreEm { get; set; }
    public int? NganSachDuKien { get; set; }
    public string? SoThichGhiChu { get; set; }
    public string? MaGoiYThamKhao { get; set; }
    public string? LyDoTuChoiGoiY { get; set; }
}