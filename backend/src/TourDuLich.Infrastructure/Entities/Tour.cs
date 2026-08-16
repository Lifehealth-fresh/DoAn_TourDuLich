using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class Tour
{
    public string MaTour { get; set; } = null!;

    public string TenTour { get; set; } = null!;

    public string? Mota { get; set; }

    public int? ThoiGian { get; set; }

    public string? DieuKhoan { get; set; }

    public int GiaTour { get; set; }

    public int Slkhach { get; set; }

    public int? SlhuongDanVien { get; set; }

    public string LoaiTour { get; set; } = null!;

    public string? TrangThai { get; set; }

    public virtual ICollection<AigoiY> AigoiYs { get; set; } = new List<AigoiY>();

    public virtual ICollection<AnhTour> AnhTours { get; set; } = new List<AnhTour>();

    public virtual ICollection<DanhGiaTour> DanhGiaTours { get; set; } = new List<DanhGiaTour>();

    public virtual ICollection<DanhSachYeuThich> DanhSachYeuThiches { get; set; } = new List<DanhSachYeuThich>();

    public virtual ICollection<DatDichVu> DatDichVus { get; set; } = new List<DatDichVu>();

    public virtual ICollection<HanhViKhachHang> HanhViKhachHangs { get; set; } = new List<HanhViKhachHang>();

    public virtual ICollection<KmTour> KmTours { get; set; } = new List<KmTour>();

    public virtual ICollection<LichDanTour> LichDanTours { get; set; } = new List<LichDanTour>();

    public virtual ICollection<LichKhoiHanh> LichKhoiHanhs { get; set; } = new List<LichKhoiHanh>();

    public virtual ICollection<LichTrinh> LichTrinhs { get; set; } = new List<LichTrinh>();

    public virtual ICollection<YeuCauThietKe> YeuCauThietKes { get; set; } = new List<YeuCauThietKe>();
}
