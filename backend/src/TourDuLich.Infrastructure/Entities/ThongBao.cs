using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class ThongBao
{
    public string MaThongBao { get; set; } = null!;

    public string? MaUser { get; set; }

    public string? TieuDe { get; set; }

    public string? NoiDung { get; set; }

    public bool? DaDoc { get; set; }

    public DateOnly? NgayGui { get; set; }

    public virtual NguoiSuDung? MaUserNavigation { get; set; }
}
