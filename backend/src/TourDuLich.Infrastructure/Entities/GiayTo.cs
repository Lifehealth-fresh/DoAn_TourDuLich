using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class GiayTo
{
    public string MaGiayTo { get; set; } = null!;

    public string LoaiGiayTo { get; set; } = null!;

    public string SoTrenGiayTo { get; set; } = null!;

    public DateOnly NgayCap { get; set; }

    public DateOnly NgayHetHan { get; set; }

    public string NoiCap { get; set; } = null!;

    public string? AnhMatTruoc { get; set; }

    public string? AnhMatSau { get; set; }

    public string? CloudPublicIdTruoc { get; set; }

    public string? CloudPublicIdSau { get; set; }

    public string MaKhachHang { get; set; } = null!;

    public virtual KhachHang MaKhachHangNavigation { get; set; } = null!;
}
