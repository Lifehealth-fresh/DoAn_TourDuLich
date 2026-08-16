using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class SanPhamDoiTac
{
    public string MaSanPham { get; set; } = null!;

    public string MaDoiTac { get; set; } = null!;

    public string TenSanPham { get; set; } = null!;

    public string? DonViTinh { get; set; }

    public int GiaNiemYet { get; set; }

    public string? MaDthamQuan { get; set; }

    public string? Mota { get; set; }

    public string? TrangThai { get; set; }

    public virtual ICollection<DanhGiaSanPhamDoiTac> DanhGiaSanPhamDoiTacs { get; set; } = new List<DanhGiaSanPhamDoiTac>();

    public virtual ICollection<LichTrinh> LichTrinhs { get; set; } = new List<LichTrinh>();

    public virtual DoiTac MaDoiTacNavigation { get; set; } = null!;

    public virtual DiemThamQuan? MaDthamQuanNavigation { get; set; }
}
