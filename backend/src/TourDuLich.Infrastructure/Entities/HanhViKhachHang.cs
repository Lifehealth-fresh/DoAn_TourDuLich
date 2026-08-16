using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class HanhViKhachHang
{
    public string MaHanhDong { get; set; } = null!;

    public string? MaUser { get; set; }

    public string? MaTour { get; set; }

    public string? HanhDong { get; set; }

    public DateTime? ThoiGian { get; set; }

    public virtual Tour? MaTourNavigation { get; set; }

    public virtual NguoiSuDung? MaUserNavigation { get; set; }
}
