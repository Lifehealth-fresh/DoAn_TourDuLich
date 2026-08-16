using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class DieuKienKm
{
    public string MaDk { get; set; } = null!;

    public string? MaKhuyenMai { get; set; }

    public int? DonToiThieu { get; set; }

    public bool? LanDatDau { get; set; }

    public int? SoLuong { get; set; }

    public virtual KhuyenMai? MaKhuyenMaiNavigation { get; set; }
}
