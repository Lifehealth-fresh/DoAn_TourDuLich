using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class DatDichVu
{
    public string MaBooking { get; set; } = null!;

    public string MaUser { get; set; } = null!;

    public string MaTour { get; set; } = null!;

    public string? MaKhoiHanh { get; set; }

    public DateOnly? NgayDat { get; set; }

    public int? SlnguoiLon { get; set; }

    public int? SltreEm { get; set; }

    public int? TongTien { get; set; }

    public int? TongGiamGia { get; set; }

    public int? ThanhTien { get; set; }

    public string? TrangThai { get; set; }

    public virtual ICollection<DatDichVuKhuyenMai> DatDichVuKhuyenMais { get; set; } = new List<DatDichVuKhuyenMai>();

    public virtual ICollection<HopDong> HopDongs { get; set; } = new List<HopDong>();

    public virtual LichKhoiHanh? MaKhoiHanhNavigation { get; set; }

    public virtual Tour MaTourNavigation { get; set; } = null!;

    public virtual NguoiSuDung MaUserNavigation { get; set; } = null!;

    public virtual ICollection<ThanhToan> ThanhToans { get; set; } = new List<ThanhToan>();
}
