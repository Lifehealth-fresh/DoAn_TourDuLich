using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class NguoiSuDung
{
    public string MaUser { get; set; } = null!;

    public string SoDienThoai { get; set; } = null!;

    public string MatKhau { get; set; } = null!;

    public int MaVaiTro { get; set; }

    public virtual ICollection<AigoiY> AigoiYs { get; set; } = new List<AigoiY>();

    public virtual ICollection<DanhGiaHdv> DanhGiaHdvs { get; set; } = new List<DanhGiaHdv>();

    public virtual ICollection<DanhGiaSanPhamDoiTac> DanhGiaSanPhamDoiTacs { get; set; } = new List<DanhGiaSanPhamDoiTac>();

    public virtual ICollection<DanhGiaTour> DanhGiaTours { get; set; } = new List<DanhGiaTour>();

    public virtual ICollection<DanhSachYeuThich> DanhSachYeuThiches { get; set; } = new List<DanhSachYeuThich>();

    public virtual ICollection<DatDichVu> DatDichVus { get; set; } = new List<DatDichVu>();

    public virtual ICollection<HanhViKhachHang> HanhViKhachHangs { get; set; } = new List<HanhViKhachHang>();

    public virtual ICollection<HopDong> HopDongs { get; set; } = new List<HopDong>();

    public virtual KhachHang? KhachHang { get; set; }

    public virtual VaiTro MaVaiTroNavigation { get; set; } = null!;

    public virtual ICollection<ThongBao> ThongBaos { get; set; } = new List<ThongBao>();

    public virtual ICollection<YeuCauThietKe> YeuCauThietKes { get; set; } = new List<YeuCauThietKe>();
}
