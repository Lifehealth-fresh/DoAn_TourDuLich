namespace TourDuLich.API.DTOs;

public class AdminCreateAccountDto
{
    public string SoDienThoai { get; set; } = null!;
    public string MatKhau { get; set; } = null!;
    public string? TenVaiTro { get; set; }
    public List<QuyenChucNangDto>? Quyen { get; set; }
}

public class AdminUpdateQuyenDto
{
    public List<QuyenChucNangDto> Quyen { get; set; } = [];
}

public class QuyenChucNangDto
{
    public string ChucNang { get; set; } = null!;
    public bool Them { get; set; }
    public bool Sua { get; set; }
    public bool Xoa { get; set; }
    public bool ToanQuyen { get; set; }
}
