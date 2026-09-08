using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        var query = _context.LichKhoiHanhs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(maTour))
            query = query.Where(x => x.MaTour == FixedLengthHelper.PadTo20(maTour));

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var totalCount = await query.CountAsync();
        var result = await query.OrderBy(x => x.NgayKhoiHanh).ThenBy(x => x.MaKhoiHanh)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                maKhoiHanh = FixedLengthHelper.TrimSafe(x.MaKhoiHanh),
                maTour = FixedLengthHelper.TrimSafe(x.MaTour),
                ngayKhoiHanh = x.NgayKhoiHanh,
                ngayKetThuc = x.NgayKetThuc,
                diaDiem = x.DiaDiem
            })
            .ToListAsync();

        return Ok(new { items = result, page, pageSize, totalCount });
    }

    // GET /api/LichKhoiHanh/{maKhoiHanh}
    [HttpGet("{maKhoiHanh}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetLichKhoiHanh(string maKhoiHanh)
    {
        var key = FixedLengthHelper.PadTo20(maKhoiHanh);

        var item = await _context.LichKhoiHanhs
            .AsNoTracking()
            .Where(x => x.MaKhoiHanh == key)
            .Select(x => new
            {
                maKhoiHanh = FixedLengthHelper.TrimSafe(x.MaKhoiHanh),
                maTour = FixedLengthHelper.TrimSafe(x.MaTour),
                ngayKhoiHanh = x.NgayKhoiHanh,
                ngayKetThuc = x.NgayKetThuc,
                diaDiem = x.DiaDiem
            })
            .FirstOrDefaultAsync();

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
            DiaDiem = dto.DiaDiem
        };

        _context.LichKhoiHanhs.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLichKhoiHanh), new { maKhoiHanh = FixedLengthHelper.TrimSafe(entity.MaKhoiHanh) }, new
        {
            maKhoiHanh = FixedLengthHelper.TrimSafe(entity.MaKhoiHanh),
            maTour = FixedLengthHelper.TrimSafe(entity.MaTour),
            ngayKhoiHanh = entity.NgayKhoiHanh,
            ngayKetThuc = entity.NgayKetThuc,
            diaDiem = entity.DiaDiem
        });
    }

    // PUT /api/LichKhoiHanh/{maKhoiHanh}
    [HttpPut("{maKhoiHanh}")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<IActionResult> UpdateLichKhoiHanh(string maKhoiHanh, [FromBody] LichKhoiHanhUpdateDto dto)
    {
        var existing = await _context.LichKhoiHanhs.FindAsync(FixedLengthHelper.PadTo20(maKhoiHanh));
        if (existing == null)
            return NotFound();

        existing.NgayKhoiHanh = dto.NgayKhoiHanh;
        existing.NgayKetThuc = dto.NgayKetThuc;
        existing.DiaDiem = dto.DiaDiem;

        await _context.SaveChangesAsync();
        return NoContent();
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
