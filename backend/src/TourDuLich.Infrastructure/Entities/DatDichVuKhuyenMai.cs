using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class DatDichVuKhuyenMai
{
    public int Stt { get; set; }

    public string MaBooking { get; set; } = null!;

    public string MaKhuyenMai { get; set; } = null!;

    public int? SoTienGiam { get; set; }

    public virtual DatDichVu MaBookingNavigation { get; set; } = null!;

    public virtual KhuyenMai MaKhuyenMaiNavigation { get; set; } = null!;
}
