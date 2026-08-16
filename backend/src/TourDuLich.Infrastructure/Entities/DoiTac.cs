using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class DoiTac
{
    public string MaDoiTac { get; set; } = null!;

    public string TenDoiTac { get; set; } = null!;

    public string LoaiDoiTac { get; set; } = null!;

    public string? NguoiLienHe { get; set; }

    public string? SoDienThoai { get; set; }

    public string? Email { get; set; }

    public string? MaKhuVuc { get; set; }

    public decimal? PhanTramHoaHong { get; set; }

    public string? TrangThai { get; set; }

    public virtual KhuVuc? MaKhuVucNavigation { get; set; }

    public virtual ICollection<SanPhamDoiTac> SanPhamDoiTacs { get; set; } = new List<SanPhamDoiTac>();
}
