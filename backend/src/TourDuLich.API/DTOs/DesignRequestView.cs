using System.Linq.Expressions;
using System.Text.Json.Serialization;
using TourDuLich.Application.Services;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.API.DTOs;

public sealed class DesignRequestView
{
    public string MaYeuCau { get; set; } = "";
    public string MaUser { get; set; } = "";
    public string? DiemDenMongMuon { get; set; }
    public DateOnly? NgayDuKienDi { get; set; }
    public int? SoNgay { get; set; }
    public int? SoNguoiLon { get; set; }
    public int? SoTreEm { get; set; }
    public int? NganSachDuKien { get; set; }
    public string? SoThichGhiChu { get; set; }
    public string? MaGoiYThamKhao { get; set; }
    public string? LyDoTuChoiGoiY { get; set; }
    public string? TrangThai { get; set; }
    public DateTime? NgayGui { get; set; }
    public string? MaTourTao { get; set; }
    [JsonIgnore] public string? StoredReason { get; set; }
    public string? LyDo => DesignRevisionReason.Read(StoredReason).LyDo;
    public string? NguonLyDo => DesignRevisionReason.Read(StoredReason).NguonLyDo;

    // Only SQL-translatable expressions here. Prefix parsing happens during JSON serialization.
    public static Expression<Func<YeuCauThietKe, DesignRequestView>> Projection => r => new DesignRequestView
    {
        MaYeuCau = r.MaYeuCau.Trim(), MaUser = r.MaUser.Trim(),
        DiemDenMongMuon = r.DiemDenMongMuon, NgayDuKienDi = r.NgayDuKienDi,
        SoNgay = r.SoNgay, SoNguoiLon = r.SoNguoiLon, SoTreEm = r.SoTreEm, NganSachDuKien = r.NganSachDuKien,
        SoThichGhiChu = r.SoThichGhiChu, MaGoiYThamKhao = r.MaGoiYthamKhao == null ? null : r.MaGoiYthamKhao.Trim(),
        LyDoTuChoiGoiY = r.LyDoTuChoiGoiY, StoredReason = r.LyDoTuChoiBoiSale,
        TrangThai = r.TrangThai == null ? null : r.TrangThai.Trim(), NgayGui = r.NgayGui,
        MaTourTao = r.MaTourTao == null ? null : r.MaTourTao.Trim()
    };
}
public sealed class CustomerRevisionDto
{
    public string LyDo { get; set; } = "";
}
