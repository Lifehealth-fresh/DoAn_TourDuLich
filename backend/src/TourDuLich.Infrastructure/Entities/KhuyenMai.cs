using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class KhuyenMai
{
    public string MaKm { get; set; } = null!;

    public string? MaNhomKm { get; set; }

    public string? TenKm { get; set; }

    public string? MaCode { get; set; }

    public DateTime? NgayBd { get; set; }

    public DateTime? NgayKt { get; set; }

    public string? DonVi { get; set; }

    public int? GiamGia { get; set; }

    public bool? CoCongDon { get; set; }

    public string? TrangThai { get; set; }

    public virtual ICollection<DatDichVuKhuyenMai> DatDichVuKhuyenMais { get; set; } = new List<DatDichVuKhuyenMai>();

    public virtual ICollection<DieuKienKm> DieuKienKms { get; set; } = new List<DieuKienKm>();

    public virtual ICollection<KmTour> KmTours { get; set; } = new List<KmTour>();

    public virtual NhomKhuyenMai? MaNhomKmNavigation { get; set; }
}
