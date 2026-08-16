using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class DiemThamQuan
{
    public string MaDthamQuan { get; set; } = null!;

    public string? TenDiaDanh { get; set; }

    public string? DiaChi { get; set; }

    public string? MaKhuVuc { get; set; }

    public decimal? KinhDo { get; set; }

    public decimal? ViDo { get; set; }

    public string? Mota { get; set; }

    public virtual ICollection<LichTrinh> LichTrinhs { get; set; } = new List<LichTrinh>();

    public virtual KhuVuc? MaKhuVucNavigation { get; set; }

    public virtual ICollection<SanPhamDoiTac> SanPhamDoiTacs { get; set; } = new List<SanPhamDoiTac>();
}
