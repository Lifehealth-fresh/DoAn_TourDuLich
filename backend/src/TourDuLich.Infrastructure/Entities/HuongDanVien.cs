using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class HuongDanVien
{
    public string MaHuongDanVien { get; set; } = null!;

    public string? HoTen { get; set; }

    public DateOnly? NgaySinh { get; set; }

    public string? QueQuan { get; set; }

    public string? Email { get; set; }

    public string? Cccd { get; set; }

    public string? SoDienThoai { get; set; }

    public virtual ICollection<DanhGiaHdv> DanhGiaHdvs { get; set; } = new List<DanhGiaHdv>();

    public virtual ICollection<LichDanTour> LichDanTours { get; set; } = new List<LichDanTour>();
}
