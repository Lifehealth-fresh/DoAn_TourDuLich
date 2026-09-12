using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.API.Services;

public static class DepartureAvailability
{
    public static IQueryable<DatDichVu> HeldBookings(IQueryable<DatDichVu> query)
    {
        var cancelled = FixedLengthHelper.PadTo20("DaHuy");
        var refundPending = FixedLengthHelper.PadTo20("ChoHoanTien");
        return query.Where(b => b.TrangThai != cancelled && b.TrangThai != refundPending);
    }

    public static IQueryable<DepartureView> Select(IQueryable<LichKhoiHanh> query)
    {
        var cancelled = FixedLengthHelper.PadTo20("DaHuy");
        var refundPending = FixedLengthHelper.PadTo20("ChoHoanTien");
        return query.Select(d => new
        {
            departure = d,
            capacity = d.SoCho ?? d.MaTourNavigation.Slkhach,
            held = d.DatDichVus.Where(b => b.TrangThai != cancelled && b.TrangThai != refundPending)
                .Sum(b => (long?)(b.SlnguoiLon ?? 0) + (b.SltreEm ?? 0)) ?? 0L,
            users = d.DatDichVus.Where(b => b.TrangThai != cancelled && b.TrangThai != refundPending)
                .Select(b => b.MaUser).Distinct().Count()
        }).Select(x => new DepartureView
        {
            MaKhoiHanh = x.departure.MaKhoiHanh.Trim(), MaTour = x.departure.MaTour.Trim(),
            NgayKhoiHanh = x.departure.NgayKhoiHanh, NgayKetThuc = x.departure.NgayKetThuc,
            DiaDiem = x.departure.DiaDiem, SoCho = x.departure.SoCho,
            SucChua = x.capacity, DaDat = x.held, ConTrong = x.capacity - x.held, SoTaiKhoan = x.users
        });
    }

    public static IQueryable<DepartureGuest> Guests(IQueryable<DatDichVu> query, IQueryable<KhachHang> profiles)
        => HeldBookings(query).Select(b => new
        {
            booking = b,
            profile = profiles.Where(k => b.MaKhachHang != null
                ? k.MaKhachHang == b.MaKhachHang : k.MaUser == b.MaUser)
                .OrderBy(k => k.MaKhachHang).FirstOrDefault()
        }).Select(x => new DepartureGuest
        {
            MaBooking = x.booking.MaBooking.Trim(), MaUser = x.booking.MaUser.Trim(),
            SoDienThoai = x.profile != null ? x.profile.SoDienThoai.Trim() : x.booking.MaUserNavigation.SoDienThoai.Trim(),
            HoTen = x.profile == null ? null : (x.profile.Ho.Trim() + " " + x.profile.Ten.Trim()).Trim(),
            SlnguoiLon = x.booking.SlnguoiLon ?? 0, SltreEm = x.booking.SltreEm ?? 0,
            SoCho = (long)(x.booking.SlnguoiLon ?? 0) + (x.booking.SltreEm ?? 0),
            TrangThai = x.booking.TrangThai == null ? null : x.booking.TrangThai.Trim(),
            NgayDat = x.booking.NgayDat
        });
}
public sealed class DepartureView
{
    public string MaKhoiHanh { get; set; } = "";
    public string MaTour { get; set; } = "";
    public DateTime? NgayKhoiHanh { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public string? DiaDiem { get; set; }
    public int? SoCho { get; set; }
    public int SucChua { get; set; }
    public long DaDat { get; set; }
    public long ConTrong { get; set; }
    public int SoTaiKhoan { get; set; }
}
public sealed class DepartureGuest
{
    public string MaBooking { get; set; } = "";
    public string MaUser { get; set; } = "";
    public string SoDienThoai { get; set; } = "";
    public string? HoTen { get; set; }
    public int SlnguoiLon { get; set; }
    public int SltreEm { get; set; }
    public long SoCho { get; set; }
    public string? TrangThai { get; set; }
    public DateOnly? NgayDat { get; set; }
}
