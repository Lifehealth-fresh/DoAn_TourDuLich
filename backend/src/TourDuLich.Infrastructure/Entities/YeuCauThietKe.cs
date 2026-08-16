using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class YeuCauThietKe
{
    public string MaYeuCau { get; set; } = null!;

    public string MaUser { get; set; } = null!;

    public string? DiemDenMongMuon { get; set; }

    public DateOnly? NgayDuKienDi { get; set; }

    public int? SoNgay { get; set; }

    public int? SoNguoiLon { get; set; }

    public int? SoTreEm { get; set; }

    public int? NganSachDuKien { get; set; }

    public string? SoThichGhiChu { get; set; }

    public string? MaGoiYthamKhao { get; set; }

    public string? LyDoTuChoiGoiY { get; set; }

    public string? TrangThai { get; set; }

    public DateTime? NgayGui { get; set; }

    public string? MaTourTao { get; set; }

    public virtual AigoiY? MaGoiYthamKhaoNavigation { get; set; }

    public virtual Tour? MaTourTaoNavigation { get; set; }

    public virtual NguoiSuDung MaUserNavigation { get; set; } = null!;
}
