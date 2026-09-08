using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class HopDong
{
    public string MaHopDong { get; set; } = null!;

    public string MaBooking { get; set; } = null!;

    public string? SoHopDong { get; set; }

    public DateOnly? NgayKy { get; set; }

    public string? DieuKhoanCamKet { get; set; }

    public string? FileHopDongUrl { get; set; }

    public string? NguoiDaiDien { get; set; }

    public string? TrangThai { get; set; }

    public string? HoTenKhach { get; set; }

    public string? LoaiGiayTo { get; set; }

    public string? SoGiayTo { get; set; }

    public virtual DatDichVu MaBookingNavigation { get; set; } = null!;

    public virtual NguoiSuDung? NguoiDaiDienNavigation { get; set; }
}
