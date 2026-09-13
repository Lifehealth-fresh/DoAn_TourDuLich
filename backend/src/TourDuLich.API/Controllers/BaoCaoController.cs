using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourDuLich.API.Authorization;
using TourDuLich.API.Services;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Sale")]
public class BaoCaoController : ControllerBase
{
    private readonly AppDbContext _context;

    public BaoCaoController(AppDbContext context) => _context = context;

    [HttpGet("tong-quan")]
    [RequirePermission(PermissionCatalog.TongQuan, PermissionCatalog.Xem)]
    public async Task<ActionResult> TongQuan(CancellationToken cancellationToken)
    {
        var daHuy = FixedLengthHelper.PadTo20("DaHuy");
        var choHoan = FixedLengthHelper.PadTo20("ChoHoanTien");
        var payOk = new[] { FixedLengthHelper.PadTo20("DaXacNhan"), FixedLengthHelper.PadTo20("ThanhCong") };
        var chuan = FixedLengthHelper.PadTo20("Chuan");
        var hoatDong = FixedLengthHelper.PadTo20("HoatDong");
        var now = DateTime.UtcNow;
        var fromMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);

        var toursDangBan = await _context.Tours.AsNoTracking()
            .CountAsync(t => t.LoaiTour == chuan && (t.TrangThai == null || t.TrangThai == hoatDong), cancellationToken);

        var theoTrangThai = (await _context.DatDichVus.AsNoTracking()
            .GroupBy(b => b.TrangThai)
            .Select(g => new
            {
                trangThai = g.Key,
                soLuong = g.Count(),
                thanhTien = g.Sum(x => (long?)(x.ThanhTien ?? 0)) ?? 0
            })
            .ToListAsync(cancellationToken))
            .Select(row => new
            {
                trangThai = FixedLengthHelper.TrimSafe(row.trangThai) ?? "KhongRo",
                row.soLuong,
                row.thanhTien
            })
            .OrderByDescending(row => row.soLuong)
            .ToList();

        var choXacNhanCount = theoTrangThai
            .Where(row => row.trangThai == "ChoXacNhan")
            .Select(row => row.soLuong)
            .FirstOrDefault();
        var tongBooking = theoTrangThai.Sum(row => row.soLuong);
        var soHuy = theoTrangThai.Where(row => row.trangThai is "DaHuy" or "ChoHoanTien").Sum(row => row.soLuong);

        var payments = await _context.ThanhToans.AsNoTracking()
            .Where(p => payOk.Contains(p.TrangThai!))
            .Select(p => new { p.SoTien, Ngay = p.PaidAt ?? p.NgayTt, p.MaBooking })
            .ToListAsync(cancellationToken);
        var daThu = payments.Sum(p => (long)(p.SoTien ?? 0));

        var outstanding = await _context.DatDichVus.AsNoTracking()
            .Where(b => b.TrangThai != daHuy && b.TrangThai != choHoan)
            .Select(b => new
            {
                thanhTien = b.ThanhTien ?? 0,
                daTra = b.ThanhToans
                    .Where(p => payOk.Contains(p.TrangThai!))
                    .Sum(p => (int?)p.SoTien) ?? 0
            })
            .ToListAsync(cancellationToken);
        var conPhaiThu = outstanding.Sum(row => Math.Max(0L, row.thanhTien - row.daTra));

        var monthlyMap = payments
            .Where(p => p.Ngay != null && p.Ngay >= fromMonth)
            .GroupBy(p => (p.Ngay!.Value.Year, p.Ngay.Value.Month))
            .ToDictionary(g => g.Key, g => new { daThu = g.Sum(x => (long)(x.SoTien ?? 0)), soGiaoDich = g.Count() });
        var doanhThuTheoThang = Enumerable.Range(0, 6)
            .Select(offset => fromMonth.AddMonths(offset))
            .Select(month =>
            {
                monthlyMap.TryGetValue((month.Year, month.Month), out var row);
                return new
                {
                    nam = month.Year,
                    thang = month.Month,
                    nhan = $"T{month.Month}/{month.Year}",
                    daThu = row?.daThu ?? 0,
                    soGiaoDich = row?.soGiaoDich ?? 0
                };
            })
            .ToList();

        var paidByBooking = payments
            .GroupBy(p => p.MaBooking)
            .ToDictionary(g => g.Key, g => g.Sum(x => (long)(x.SoTien ?? 0)));
        var bookingRows = await _context.DatDichVus.AsNoTracking()
            .Select(b => new { b.MaBooking, b.MaTour, b.TrangThai, b.ThanhTien })
            .ToListAsync(cancellationToken);
        var tourNames = await _context.Tours.AsNoTracking()
            .Select(t => new { t.MaTour, t.TenTour })
            .ToListAsync(cancellationToken);
        var ratings = await _context.DanhGiaTours.AsNoTracking()
            .Where(r => r.MaTour != null && r.SaoDanhGia != null)
            .GroupBy(r => r.MaTour!)
            .Select(g => new { MaTour = g.Key, Diem = g.Average(x => (double)x.SaoDanhGia!) })
            .ToListAsync(cancellationToken);
        var ratingMap = ratings.ToDictionary(r => r.MaTour, r => r.Diem, StringComparer.OrdinalIgnoreCase);

        var topTour = tourNames.Select(tour =>
        {
            var rows = bookingRows.Where(b => b.MaTour == tour.MaTour).ToList();
            var huy = rows.Count(b =>
            {
                var state = FixedLengthHelper.TrimSafe(b.TrangThai);
                return state is "DaHuy" or "ChoHoanTien";
            });
            var thu = rows.Sum(b => paidByBooking.GetValueOrDefault(b.MaBooking));
            ratingMap.TryGetValue(tour.MaTour, out var diem);
            return new
            {
                maTour = FixedLengthHelper.TrimSafe(tour.MaTour),
                tenTour = tour.TenTour,
                soBooking = rows.Count,
                daThu = thu,
                soHuy = huy,
                tyLeHuy = rows.Count == 0 ? 0 : Math.Round(huy / (double)rows.Count, 4),
                diemTrungBinh = Math.Round(diem, 2)
            };
        })
            .Where(row => row.soBooking > 0 || row.daThu > 0)
            .OrderByDescending(row => row.daThu)
            .ThenByDescending(row => row.soBooking)
            .Take(8)
            .ToList();

        var upcomingQuery = _context.LichKhoiHanhs.AsNoTracking()
            .Where(d => d.NgayKhoiHanh != null && d.NgayKhoiHanh >= now)
            .OrderBy(d => d.NgayKhoiHanh)
            .Take(12);
        var lichSapKhoiHanh = (await DepartureAvailability.Select(upcomingQuery).ToListAsync(cancellationToken))
            .Select(d =>
            {
                var tour = tourNames.FirstOrDefault(t => t.MaTour.Trim() == d.MaTour || t.MaTour == d.MaTour);
                return new
                {
                    maKhoiHanh = d.MaKhoiHanh,
                    maTour = d.MaTour,
                    tenTour = tour?.TenTour ?? d.MaTour,
                    ngayKhoiHanh = d.NgayKhoiHanh,
                    sucChua = d.SucChua,
                    daDat = d.DaDat,
                    conTrong = d.ConTrong,
                    tyLeLapDay = d.SucChua <= 0 ? 0 : Math.Round(d.DaDat / (double)d.SucChua, 4)
                };
            })
            .ToList();

        return Ok(new
        {
            toursDangBan,
            tongBooking,
            choXacNhan = choXacNhanCount,
            daThu,
            conPhaiThu,
            soHuy,
            tyLeHuy = tongBooking == 0 ? 0 : Math.Round(soHuy / (double)tongBooking, 4),
            theoTrangThai,
            doanhThuTheoThang,
            topTour,
            lichSapKhoiHanh
        });
    }
}
