using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourDuLich.API.DTOs;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KhuVucController : ControllerBase
{
    private readonly AppDbContext _context;

    public KhuVucController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/KhuVuc?trangThai={trangThai}
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult> GetKhuVucs([FromQuery] string? trangThai = null)
    {
        var query = _context.KhuVucs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(trangThai))
            query = query.Where(x => x.TrangThai == FixedLengthHelper.PadTo20(trangThai));

        var result = await query
            .Select(x => new
            {
                maKhuVuc = FixedLengthHelper.TrimSafe(x.MaKhuVuc),
                tenKhuVuc = x.TenKhuVuc,
                quocGia = FixedLengthHelper.TrimSafe(x.QuocGia),
                viDo = x.ViDo,
                kinhDo = x.KinhDo,
                muiGio = x.MuiGio,
                trangThai = FixedLengthHelper.TrimSafe(x.TrangThai)
            })
            .ToListAsync();

        return Ok(result);
    }

    // GET /api/KhuVuc/{maKhuVuc}
    [HttpGet("{maKhuVuc}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetKhuVuc(string maKhuVuc)
    {
        var key = FixedLengthHelper.PadTo20(maKhuVuc);

        var item = await _context.KhuVucs
            .AsNoTracking()
            .Where(x => x.MaKhuVuc == key)
            .Select(x => new
            {
                maKhuVuc = FixedLengthHelper.TrimSafe(x.MaKhuVuc),
                tenKhuVuc = x.TenKhuVuc,
                quocGia = FixedLengthHelper.TrimSafe(x.QuocGia),
                viDo = x.ViDo,
                kinhDo = x.KinhDo,
                muiGio = x.MuiGio,
                trangThai = FixedLengthHelper.TrimSafe(x.TrangThai)
            })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound(new { message = $"Không tìm thấy khu vực '{maKhuVuc}'." });

        return Ok(item);
    }

    // POST /api/KhuVuc
    [HttpPost]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> CreateKhuVuc([FromBody] KhuVucCreateDto dto)
    {
        var maKhuVuc = FixedLengthHelper.PadTo20(dto.MaKhuVuc);

        if (await _context.KhuVucs.AnyAsync(x => x.MaKhuVuc == maKhuVuc))
            return Conflict(new { message = "MaKhuVuc đã tồn tại." });

        var entity = new KhuVuc
        {
            MaKhuVuc = maKhuVuc,
            TenKhuVuc = dto.TenKhuVuc,
            QuocGia = string.IsNullOrWhiteSpace(dto.QuocGia) ? null : FixedLengthHelper.PadTo20(dto.QuocGia),
            ViDo = dto.ViDo,
            KinhDo = dto.KinhDo,
            MuiGio = dto.MuiGio,
            TrangThai = FixedLengthHelper.PadTo20(string.IsNullOrWhiteSpace(dto.TrangThai) ? "HoatDong" : dto.TrangThai)
        };

        _context.KhuVucs.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetKhuVuc), new { maKhuVuc = FixedLengthHelper.TrimSafe(entity.MaKhuVuc) }, new
        {
            maKhuVuc = FixedLengthHelper.TrimSafe(entity.MaKhuVuc),
            tenKhuVuc = entity.TenKhuVuc,
            quocGia = FixedLengthHelper.TrimSafe(entity.QuocGia),
            viDo = entity.ViDo,
            kinhDo = entity.KinhDo,
            muiGio = entity.MuiGio,
            trangThai = FixedLengthHelper.TrimSafe(entity.TrangThai)
        });
    }

    // PUT /api/KhuVuc/{maKhuVuc}
    [HttpPut("{maKhuVuc}")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<IActionResult> UpdateKhuVuc(string maKhuVuc, [FromBody] KhuVucUpdateDto dto)
    {
        var existing = await _context.KhuVucs.FindAsync(FixedLengthHelper.PadTo20(maKhuVuc));
        if (existing == null)
            return NotFound();

        existing.TenKhuVuc = dto.TenKhuVuc;
        existing.QuocGia = dto.QuocGia is null ? existing.QuocGia : FixedLengthHelper.PadTo20(dto.QuocGia);
        existing.ViDo = dto.ViDo;
        existing.KinhDo = dto.KinhDo;
        existing.MuiGio = dto.MuiGio;
        existing.TrangThai = dto.TrangThai is null ? existing.TrangThai : FixedLengthHelper.PadTo20(dto.TrangThai);

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/KhuVuc/{maKhuVuc}
    [HttpDelete("{maKhuVuc}")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<IActionResult> DeleteKhuVuc(string maKhuVuc)
    {
        var key = FixedLengthHelper.PadTo20(maKhuVuc);
        var existing = await _context.KhuVucs.FindAsync(key);
        if (existing == null)
            return NotFound();

        var hasDiemThamQuan = await _context.DiemThamQuans.AnyAsync(d => d.MaKhuVuc == key);
        var hasDoiTac = await _context.DoiTacs.AnyAsync(d => d.MaKhuVuc == key);

        if (hasDiemThamQuan || hasDoiTac)
            return BadRequest(new { message = "Không thể xóa khu vực vì đã có điểm tham quan hoặc đối tác tham chiếu." });

        _context.KhuVucs.Remove(existing);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
