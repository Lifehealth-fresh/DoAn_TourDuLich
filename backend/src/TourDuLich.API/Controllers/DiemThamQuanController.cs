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
public class DiemThamQuanController : ControllerBase
{
    private readonly AppDbContext _context;

    public DiemThamQuanController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/DiemThamQuan?maKhuVuc={maKhuVuc}
    [HttpGet]
    public async Task<ActionResult> GetDiemThamQuans([FromQuery] string? maKhuVuc = null)
    {
        var query = _context.DiemThamQuans.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(maKhuVuc))
            query = query.Where(x => x.MaKhuVuc == FixedLengthHelper.PadTo20(maKhuVuc));

        var result = await query
            .Select(x => new
            {
                maDthamQuan = FixedLengthHelper.TrimSafe(x.MaDthamQuan),
                tenDiaDanh = x.TenDiaDanh,
                diaChi = x.DiaChi,
                maKhuVuc = FixedLengthHelper.TrimSafe(x.MaKhuVuc),
                kinhDo = x.KinhDo,
                viDo = x.ViDo,
                mota = x.Mota
            })
            .ToListAsync();

        return Ok(result);
    }

    // GET /api/DiemThamQuan/{maDthamQuan}
    [HttpGet("{maDthamQuan}")]
    public async Task<ActionResult> GetDiemThamQuan(string maDthamQuan)
    {
        var key = FixedLengthHelper.PadTo20(maDthamQuan);

        var item = await _context.DiemThamQuans
            .AsNoTracking()
            .Where(x => x.MaDthamQuan == key)
            .Select(x => new
            {
                maDthamQuan = FixedLengthHelper.TrimSafe(x.MaDthamQuan),
                tenDiaDanh = x.TenDiaDanh,
                diaChi = x.DiaChi,
                maKhuVuc = FixedLengthHelper.TrimSafe(x.MaKhuVuc),
                kinhDo = x.KinhDo,
                viDo = x.ViDo,
                mota = x.Mota
            })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound(new { message = $"Không tìm thấy điểm tham quan '{maDthamQuan}'." });

        return Ok(item);
    }

    // POST /api/DiemThamQuan
    [HttpPost]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> CreateDiemThamQuan([FromBody] DiemThamQuanCreateDto dto)
    {
        var maDthamQuan = FixedLengthHelper.PadTo20(dto.MaDthamQuan);

        if (await _context.DiemThamQuans.AnyAsync(x => x.MaDthamQuan == maDthamQuan))
            return Conflict(new { message = "MaDthamQuan đã tồn tại." });

        string? maKhuVuc = null;
        if (!string.IsNullOrWhiteSpace(dto.MaKhuVuc))
        {
            maKhuVuc = FixedLengthHelper.PadTo20(dto.MaKhuVuc);
            if (!await _context.KhuVucs.AnyAsync(k => k.MaKhuVuc == maKhuVuc))
                return BadRequest(new { message = $"MaKhuVuc '{FixedLengthHelper.TrimSafe(maKhuVuc)}' không tồn tại." });
        }

        var entity = new DiemThamQuan
        {
            MaDthamQuan = maDthamQuan,
            TenDiaDanh = dto.TenDiaDanh,
            DiaChi = dto.DiaChi,
            MaKhuVuc = maKhuVuc,
            KinhDo = dto.KinhDo,
            ViDo = dto.ViDo,
            Mota = dto.Mota
        };

        _context.DiemThamQuans.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDiemThamQuan), new { maDthamQuan = FixedLengthHelper.TrimSafe(entity.MaDthamQuan) }, new
        {
            maDthamQuan = FixedLengthHelper.TrimSafe(entity.MaDthamQuan),
            tenDiaDanh = entity.TenDiaDanh,
            diaChi = entity.DiaChi,
            maKhuVuc = FixedLengthHelper.TrimSafe(entity.MaKhuVuc),
            kinhDo = entity.KinhDo,
            viDo = entity.ViDo,
            mota = entity.Mota
        });
    }

    // PUT /api/DiemThamQuan/{maDthamQuan}
    [HttpPut("{maDthamQuan}")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<IActionResult> UpdateDiemThamQuan(string maDthamQuan, [FromBody] DiemThamQuanUpdateDto dto)
    {
        var existing = await _context.DiemThamQuans.FindAsync(FixedLengthHelper.PadTo20(maDthamQuan));
        if (existing == null)
            return NotFound();

        existing.TenDiaDanh = dto.TenDiaDanh;
        existing.DiaChi = dto.DiaChi;
        existing.MaKhuVuc = dto.MaKhuVuc is null ? existing.MaKhuVuc : FixedLengthHelper.PadTo20(dto.MaKhuVuc);
        existing.KinhDo = dto.KinhDo;
        existing.ViDo = dto.ViDo;
        existing.Mota = dto.Mota;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/DiemThamQuan/{maDthamQuan}
    [HttpDelete("{maDthamQuan}")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<IActionResult> DeleteDiemThamQuan(string maDthamQuan)
    {
        var key = FixedLengthHelper.PadTo20(maDthamQuan);
        var existing = await _context.DiemThamQuans.FindAsync(key);
        if (existing == null)
            return NotFound();

        var hasLichTrinh = await _context.LichTrinhs.AnyAsync(l => l.MaDthamQuan == key);
        var hasSanPhamDoiTac = await _context.SanPhamDoiTacs.AnyAsync(s => s.MaDthamQuan == key);

        if (hasLichTrinh || hasSanPhamDoiTac)
            return BadRequest(new { message = "Không thể xóa điểm tham quan vì đã có lịch trình hoặc sản phẩm đối tác tham chiếu." });

        _context.DiemThamQuans.Remove(existing);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
