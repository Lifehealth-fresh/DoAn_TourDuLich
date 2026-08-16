using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class DanhGiaHdv
{
    public string MaDanhGiaHdv { get; set; } = null!;

    public string MaUser { get; set; } = null!;

    public string MaHdv { get; set; } = null!;

    public DateTime ThoiGian { get; set; }

    public int SaoDanhGia { get; set; }

    public string? NhanXet { get; set; }

    public virtual HuongDanVien MaHdvNavigation { get; set; } = null!;

    public virtual NguoiSuDung MaUserNavigation { get; set; } = null!;
}
