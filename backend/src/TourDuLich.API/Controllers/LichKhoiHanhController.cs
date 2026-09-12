using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourDuLich.API.Services;
using TourDuLich.API.DTOs;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

using Microsoft.AspNetCore.Authorization;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LichKhoiHanhController : ControllerBase
{
    private readonly AppDbContext _context;

    public LichKhoiHanhController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/LichKhoiHanh?maTour={maTour}
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult> GetLichKhoiHanhs([FromQuery] string? maTour = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = VisibleDepartures();

        if (!string.IsNullOrWhiteSpace(maTour))
            query = query.Where(x => x.MaTour == FixedLengthHelper.PadTo20(maTour));

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var totalCount = await query.CountAsync();
        var result = await DepartureAvailability.Select(query.OrderBy(x => x.NgayKhoiHanh).ThenBy(x => x.MaKhoiHanh)
            .Skip((page - 1) * pageSize).Take(pageSize)).ToListAsync();

        return Ok(new { items = result, page, pageSize, totalCount });
    }

    // GET /api/LichKhoiHanh/{maKhoiHanh}
    [HttpGet("{maKhoiHanh}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetLichKhoiHanh(string maKhoiHanh)
    {
        var key = FixedLengthHelper.PadTo20(maKhoiHanh);

        var item = await DepartureAvailability.Select(VisibleDepartures().Where(x => x.MaKhoiHanh == key)).FirstOrDefaultAsync();

        if (item == null)
            return NotFound(new { message = $"Không tìm thấy lịch khởi hành '{maKhoiHanh}'." });

        return Ok(item);
    }

    // POST /api/LichKhoiHanh
    [HttpPost]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> CreateLichKhoiHanh([FromBody] LichKhoiHanhCreateDto dto)
    {
        var maKhoiHanh = FixedLengthHelper.PadTo20(dto.MaKhoiHanh);
        var maTour = FixedLengthHelper.PadTo20(dto.MaTour);

        if (await _context.LichKhoiHanhs.AnyAsync(x => x.MaKhoiHanh == maKhoiHanh))
            return Conflict(new { message = "MaKhoiHanh đã tồn tại." });

        if (!await _context.Tours.AnyAsync(t => t.MaTour == maTour))
            return BadRequest(new { message = $"MaTour '{FixedLengthHelper.TrimSafe(maTour)}' không tồn tại." });

        var entity = new LichKhoiHanh
        {
            MaKhoiHanh = maKhoiHanh,
            MaTour = maTour,
            NgayKhoiHanh = dto.NgayKhoiHanh,
            NgayKetThuc = dto.NgayKetThuc,
            DiaDiem = dto.DiaDiem,
            SoCho = dto.SoCho
        };

        _context.LichKhoiHanhs.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLichKhoiHanh), new { maKhoiHanh = FixedLengthHelper.TrimSafe(entity.MaKhoiHanh) }, new
        {
            maKhoiHanh = FixedLengthHelper.TrimSafe(entity.MaKhoiHanh),
            maTour = FixedLengthHelper.TrimSafe(entity.MaTour),
            ngayKhoiHanh = entity.NgayKhoiHanh,
            ngayKetThuc = entity.NgayKetThuc,
            diaDiem = entity.DiaDiem,
            soCho = entity.SoCho
        });
    }

    // PUT /api/LichKhoiHanh/{maKhoiHanh}
    [HttpPut("{maKhoiHanh}")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<IActionResult> UpdateLichKhoiHanh(string maKhoiHanh, [FromBody] LichKhoiHanhUpdateDto dto)
    {
        var key = FixedLengthHelper.PadTo20(maKhoiHanh);
        var tourId = await _context.LichKhoiHanhs.AsNoTracking()
            .Where(d => d.MaKhoiHanh == key).Select(d => d.MaTour).FirstOrDefaultAsync();
        if (tourId is null) return NotFound();
        await using var transaction = await _context.Database.BeginTransactionAsync();
        // All capacity writers lock Tour then departure, as CreateBooking does.
        var tour = await _context.Tours
            .FromSqlRaw("SELECT * FROM dbo.Tour WITH (UPDLOCK, HOLDLOCK) WHERE MaTour = {0}", tourId).FirstOrDefaultAsync();
        var existing = await _context.LichKhoiHanhs
            .FromSqlRaw("SELECT * FROM dbo.LichKhoiHanh WITH (UPDLOCK, HOLDLOCK) WHERE MaKhoiHanh = {0}", key).FirstOrDefaultAsync();
        if (existing is null || tour is null) return NotFound();
        var held = await DepartureAvailability.HeldBookings(_context.DatDichVus).Where(b => b.MaKhoiHanh == key)
            .SumAsync(b => (long?)(b.SlnguoiLon ?? 0) + (b.SltreEm ?? 0)) ?? 0L;
        if ((dto.SoCho ?? tour.Slkhach) < held)
            return Conflict(new { message = "Số chỗ không được nhỏ hơn số chỗ đang giữ." });
        existing.NgayKhoiHanh = dto.NgayKhoiHanh;
        existing.NgayKetThuc = dto.NgayKetThuc;
        existing.DiaDiem = dto.DiaDiem;
        existing.SoCho = dto.SoCho;
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return NoContent();
    }

    private IQueryable<LichKhoiHanh> VisibleDepartures()
    {
        var query = _context.LichKhoiHanhs.AsNoTracking();
        if (User.IsInRole("Sale") || User.IsInRole("Admin")) return query;
        var now = DateTime.UtcNow;
        var privateType = FixedLengthHelper.PadTo20("TuThietKe");
        var userId = FixedLengthHelper.PadTo20(User.FindFirst("MaUser")?.Value ?? "");
        return query.Where(d => d.NgayKhoiHanh > now &&
            (d.MaTourNavigation.LoaiTour != privateType ||
             _context.YeuCauThietKes.Any(r => r.MaTourTao == d.MaTour && r.MaUser == userId)));
    }

    [HttpGet("{maKhoiHanh}/khach")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> GetGuests(string maKhoiHanh)
    {
        var key = FixedLengthHelper.PadTo20(maKhoiHanh);
        // Totals and individual rows must be read consistently if a cancellation arrives.
        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var departure = await DepartureAvailability.Select(_context.LichKhoiHanhs.AsNoTracking()
            .Where(d => d.MaKhoiHanh == key)).FirstOrDefaultAsync();
        if (departure is null) return NotFound(new { message = "Không tìm thấy lịch khởi hành." });
        var bookings = await DepartureAvailability.Guests(_context.DatDichVus.AsNoTracking()
            .Where(b => b.MaKhoiHanh == key).OrderBy(b => b.NgayDat).ThenBy(b => b.MaBooking),
            _context.KhachHangs.AsNoTracking()).ToListAsync();
        await transaction.CommitAsync();
        return Ok(new
        {
            departure.MaKhoiHanh, departure.MaTour, departure.NgayKhoiHanh,
            departure.SucChua, departure.DaDat, departure.ConTrong, departure.SoTaiKhoan, bookings
        });
    }

    // DELETE /api/LichKhoiHanh/{maKhoiHanh}
    [HttpDelete("{maKhoiHanh}")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<IActionResult> DeleteLichKhoiHanh(string maKhoiHanh)
    {
        var key = FixedLengthHelper.PadTo20(maKhoiHanh);
        var existing = await _context.LichKhoiHanhs.FindAsync(key);
        if (existing == null)
            return NotFound();

        var hasDatDichVu = await _context.DatDichVus.AnyAsync(d => d.MaKhoiHanh == key);
        var hasLichDanTour = await _context.LichDanTours.AnyAsync(l => l.MaKhoiHanh == key);

        if (hasDatDichVu || hasLichDanTour)
            return BadRequest(new { message = "Không thể xóa lịch khởi hành vì đã có booking hoặc lịch dẫn tour tham chiếu." });

        _context.LichKhoiHanhs.Remove(existing);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
