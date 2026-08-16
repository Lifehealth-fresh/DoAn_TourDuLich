using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class LichDanTour
{
    public string MaLichDanTour { get; set; } = null!;

    public string MaHdv { get; set; } = null!;

    public string MaTour { get; set; } = null!;

    public string MaKhoiHanh { get; set; } = null!;

    public virtual HuongDanVien MaHdvNavigation { get; set; } = null!;

    public virtual LichKhoiHanh MaKhoiHanhNavigation { get; set; } = null!;

    public virtual Tour MaTourNavigation { get; set; } = null!;
}
