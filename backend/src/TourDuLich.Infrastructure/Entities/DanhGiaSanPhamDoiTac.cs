using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class DanhGiaSanPhamDoiTac
{
    public string MaDanhGia { get; set; } = null!;

    public string MaSanPham { get; set; } = null!;

    public string MaUser { get; set; } = null!;

    public DateTime ThoiGian { get; set; }

    public int SaoDanhGia { get; set; }

    public string? NhanXet { get; set; }

    public virtual SanPhamDoiTac MaSanPhamNavigation { get; set; } = null!;

    public virtual NguoiSuDung MaUserNavigation { get; set; } = null!;
}
