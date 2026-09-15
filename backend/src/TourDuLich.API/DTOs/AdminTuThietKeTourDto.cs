namespace TourDuLich.API.DTOs;

public class AdminTuThietKeTourDto
{
    public string? MaTour { get; set; }
    public string TenTour { get; set; } = null!;
    public string? Mota { get; set; }
    public int? ThoiGian { get; set; }
    public string? DieuKhoan { get; set; }
    public int? SlhuongDanVien { get; set; }
    public string SoDienThoaiKhach { get; set; } = null!;
    public int SoNguoiLon { get; set; }
    public int SoTreEm { get; set; }
    public int? NganSachDuKien { get; set; }
    public string? MaTinhXuatPhat { get; set; }
    public string? MaTinhDen { get; set; }
    public string? DiemDenMongMuon { get; set; }
    public DateTime? NgayKhoiHanh { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public string? DiaDiem { get; set; }
    public string? MucDich { get; set; }
    public string? SoThichGhiChu { get; set; }
}
