using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class KmTour
{
    public int Stt { get; set; }

    public string MaKhuyenMai { get; set; } = null!;

    public string MaTour { get; set; } = null!;

    public virtual KhuyenMai MaKhuyenMaiNavigation { get; set; } = null!;

    public virtual Tour MaTourNavigation { get; set; } = null!;
}
