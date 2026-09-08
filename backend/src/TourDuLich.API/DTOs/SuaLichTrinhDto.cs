namespace TourDuLich.API.DTOs;

public class SuaLichTrinhDto
{
    public List<SuaLichTrinhChiTietDto> ChiTiets { get; set; } = [];
}

public class SuaLichTrinhChiTietDto
{
    public int NgayThu { get; set; }
    public int ThuTuTrongNgay { get; set; }
    public string? MaDthamQuan { get; set; }
    public string? MaSanPham { get; set; }
    public int SoLuong { get; set; } = 1;
    public string? Mota { get; set; }
}
