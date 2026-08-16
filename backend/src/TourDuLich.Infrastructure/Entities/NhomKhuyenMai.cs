using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class NhomKhuyenMai
{
    public string MaNhomKm { get; set; } = null!;

    public string? TenNhomKm { get; set; }

    public virtual ICollection<KhuyenMai> KhuyenMais { get; set; } = new List<KhuyenMai>();
}
