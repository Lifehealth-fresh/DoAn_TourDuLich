using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class LichTrinh
{
    public string MaLichTrinh { get; set; } = null!;

    public string MaTour { get; set; } = null!;

    public int? NgayThu { get; set; }

    public int? ThuTuTrongNgay { get; set; }

    public string? MaDthamQuan { get; set; }

    public string? MaSanPham { get; set; }

    public int? SoLuong { get; set; }

    public int? DonGia { get; set; }

    public int? ThanhTien { get; set; }

    public DateTime? ThoiGianDuKien { get; set; }

    public string? Mota { get; set; }

    public virtual DiemThamQuan? MaDthamQuanNavigation { get; set; }

    public virtual SanPhamDoiTac? MaSanPhamNavigation { get; set; }

    public virtual Tour MaTourNavigation { get; set; } = null!;
}
