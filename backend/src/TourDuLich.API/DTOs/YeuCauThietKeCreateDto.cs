namespace TourDuLich.API.DTOs;

public class YeuCauThietKeCreateDto
{
    public string? DiemDenMongMuon { get; set; }
    public string? MaTinhDen { get; set; }
    public string? MaTinhXuatPhat { get; set; }
    public DateOnly? NgayDuKienDi { get; set; }
    public TimeSpan? GioKhoiHanh { get; set; }
    public DateOnly? NgayKetThuc { get; set; }
    public TimeSpan? GioKetThuc { get; set; }
    public int? SoNgay { get; set; }
    public int SoNguoiLon { get; set; }
    public int SoTreEm { get; set; }
    public int? NganSachDuKien { get; set; }
    public string? MucDich { get; set; }
    public string? TenChuyenDi { get; set; }
    public string? SoThichGhiChu { get; set; }
    public string? MaGoiYThamKhao { get; set; }
    public string? LyDoTuChoiGoiY { get; set; }
}
