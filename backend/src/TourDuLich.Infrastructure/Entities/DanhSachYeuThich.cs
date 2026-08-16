using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class DanhSachYeuThich
{
    public string MaWish { get; set; } = null!;

    public string? MaUser { get; set; }

    public string? MaTour { get; set; }

    public DateOnly? NgayThem { get; set; }

    public virtual Tour? MaTourNavigation { get; set; }

    public virtual NguoiSuDung? MaUserNavigation { get; set; }
}
