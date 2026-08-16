using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class LichKhoiHanh
{
    public string MaKhoiHanh { get; set; } = null!;

    public string MaTour { get; set; } = null!;

    public DateTime? NgayKhoiHanh { get; set; }

    public DateTime? NgayKetThuc { get; set; }

    public string? DiaDiem { get; set; }

    public virtual ICollection<DatDichVu> DatDichVus { get; set; } = new List<DatDichVu>();

    public virtual ICollection<LichDanTour> LichDanTours { get; set; } = new List<LichDanTour>();

    public virtual Tour MaTourNavigation { get; set; } = null!;
}
