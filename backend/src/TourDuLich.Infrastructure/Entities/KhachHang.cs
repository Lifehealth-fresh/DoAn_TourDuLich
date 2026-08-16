using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class KhachHang
{
    public string MaKhachHang { get; set; } = null!;

    public string Ho { get; set; } = null!;

    public string Ten { get; set; } = null!;

    public string? HoGiayTo { get; set; }

    public string? TenGiayTo { get; set; }

    public string? QuocTich { get; set; }

    public string? DanhXung { get; set; }

    public string? GioiTinh { get; set; }

    public DateOnly? NgaySinh { get; set; }

    public string? Email { get; set; }

    public string SoDienThoai { get; set; } = null!;

    public string MaUser { get; set; } = null!;

    public virtual ICollection<GiayTo> GiayTos { get; set; } = new List<GiayTo>();

    public virtual NguoiSuDung MaUserNavigation { get; set; } = null!;
}
