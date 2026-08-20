namespace TourDuLich.API.DTOs;

public class SanPhamDoiTacUpdateDto
{
    public string MaDoiTac { get; set; } = null!;
    public string TenSanPham { get; set; } = null!;
    public string? DonViTinh { get; set; }
    public int GiaNiemYet { get; set; }
    public string? MaDthamQuan { get; set; }
    public string? Mota { get; set; }
    public string? TrangThai { get; set; }
}