using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourDuLich.API.DTOs;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoiTacController : ControllerBase
{
    private static readonly string[] LoaiDoiTacHopLe =
    [
        "LuuTru",
        "VanChuyen",
        "AnUong",
        "HoatDong"
    ];

    private readonly AppDbContext _context;

    public DoiTacController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult> GetDoiTacs(
        [FromQuery] string? loaiDoiTac = null,
        [FromQuery] string? maKhuVuc = null)
    {
        if (!string.IsNullOrWhiteSpace(loaiDoiTac) &&
            !LoaiDoiTacHopLe.Contains(loaiDoiTac.Trim()))
        {
            return BadRequest(new
            {
                message = "Loại đối tác không hợp lệ. Giá trị hợp lệ gồm: LuuTru, VanChuyen, AnUong, HoatDong."
            });
        }

        var query = _context.DoiTacs
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(loaiDoiTac))
        {
            var loaiDoiTacDb = FixedLengthHelper.PadTo20(loaiDoiTac);
            query = query.Where(item => item.LoaiDoiTac == loaiDoiTacDb);
        }

        if (!string.IsNullOrWhiteSpace(maKhuVuc))
        {
            var maKhuVucDb = FixedLengthHelper.PadTo20(maKhuVuc);
            query = query.Where(item => item.MaKhuVuc == maKhuVucDb);
        }

        var result = await query
            .Select(item => new
            {
                maDoiTac = FixedLengthHelper.TrimSafe(item.MaDoiTac),
                tenDoiTac = item.TenDoiTac,
                loaiDoiTac = FixedLengthHelper.TrimSafe(item.LoaiDoiTac),
                nguoiLienHe = item.NguoiLienHe,
                soDienThoai = FixedLengthHelper.TrimSafe(item.SoDienThoai),
                email = item.Email,
                maKhuVuc = FixedLengthHelper.TrimSafe(item.MaKhuVuc),
                tenKhuVuc = item.MaKhuVucNavigation!.TenKhuVuc,
                phanTramHoaHong = item.PhanTramHoaHong,
                trangThai = FixedLengthHelper.TrimSafe(item.TrangThai)
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("{maDoiTac}")]
    public async Task<ActionResult> GetDoiTac(string maDoiTac)
    {
        var maDoiTacDb = FixedLengthHelper.PadTo20(maDoiTac);

        var doiTac = await _context.DoiTacs
            .AsNoTracking()
            .Where(item => item.MaDoiTac == maDoiTacDb)
            .Select(item => new
            {
                maDoiTac = FixedLengthHelper.TrimSafe(item.MaDoiTac),
                tenDoiTac = item.TenDoiTac,
                loaiDoiTac = FixedLengthHelper.TrimSafe(item.LoaiDoiTac),
                nguoiLienHe = item.NguoiLienHe,
                soDienThoai = FixedLengthHelper.TrimSafe(item.SoDienThoai),
                email = item.Email,
                maKhuVuc = FixedLengthHelper.TrimSafe(item.MaKhuVuc),
                tenKhuVuc = item.MaKhuVucNavigation!.TenKhuVuc,
                phanTramHoaHong = item.PhanTramHoaHong,
                trangThai = FixedLengthHelper.TrimSafe(item.TrangThai)
            })
            .FirstOrDefaultAsync();

        if (doiTac is null)
        {
            return NotFound(new
            {
                message = $"Không tìm thấy đối tác '{maDoiTac}'."
            });
        }

        return Ok(doiTac);
    }

    [HttpPost]
    public async Task<ActionResult> CreateDoiTac(DoiTacCreateDto request)
    {
        if (string.IsNullOrWhiteSpace(request.MaDoiTac) ||
            string.IsNullOrWhiteSpace(request.TenDoiTac) ||
            string.IsNullOrWhiteSpace(request.LoaiDoiTac))
        {
            return BadRequest(new
            {
                message = "Mã đối tác, tên đối tác và loại đối tác không được để trống."
            });
        }

        var loaiDoiTac = request.LoaiDoiTac.Trim();

        if (!LoaiDoiTacHopLe.Contains(loaiDoiTac))
        {
            return BadRequest(new
            {
                message = "Loại đối tác không hợp lệ. Giá trị hợp lệ gồm: LuuTru, VanChuyen, AnUong, HoatDong."
            });
        }

        var maDoiTacDb = FixedLengthHelper.PadTo20(request.MaDoiTac);

        if (await _context.DoiTacs.AnyAsync(item => item.MaDoiTac == maDoiTacDb))
        {
            return Conflict(new { message = "Mã đối tác đã tồn tại." });
        }

        string? maKhuVucDb = null;

        if (!string.IsNullOrWhiteSpace(request.MaKhuVuc))
        {
            maKhuVucDb = FixedLengthHelper.PadTo20(request.MaKhuVuc);

            if (!await _context.KhuVucs.AnyAsync(item => item.MaKhuVuc == maKhuVucDb))
            {
                return BadRequest(new
                {
                    message = "Khu vực không tồn tại."
                });
            }
        }

        var doiTac = new DoiTac
        {
            MaDoiTac = maDoiTacDb,
            TenDoiTac = request.TenDoiTac.Trim(),
            LoaiDoiTac = FixedLengthHelper.PadTo20(loaiDoiTac),
            NguoiLienHe = request.NguoiLienHe?.Trim(),
            SoDienThoai = request.SoDienThoai?.Trim(),
            Email = request.Email?.Trim(),
            MaKhuVuc = maKhuVucDb,
            PhanTramHoaHong = request.PhanTramHoaHong,
            TrangThai = string.IsNullOrWhiteSpace(request.TrangThai)
                ? FixedLengthHelper.PadTo20("HoatDong")
                : FixedLengthHelper.PadTo20(request.TrangThai)
        };

        _context.DoiTacs.Add(doiTac);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetDoiTac),
            new { maDoiTac = FixedLengthHelper.TrimSafe(doiTac.MaDoiTac) },
            new
            {
                maDoiTac = FixedLengthHelper.TrimSafe(doiTac.MaDoiTac),
                tenDoiTac = doiTac.TenDoiTac,
                loaiDoiTac = FixedLengthHelper.TrimSafe(doiTac.LoaiDoiTac),
                nguoiLienHe = doiTac.NguoiLienHe,
                soDienThoai = FixedLengthHelper.TrimSafe(doiTac.SoDienThoai),
                email = doiTac.Email,
                maKhuVuc = FixedLengthHelper.TrimSafe(doiTac.MaKhuVuc),
                phanTramHoaHong = doiTac.PhanTramHoaHong,
                trangThai = FixedLengthHelper.TrimSafe(doiTac.TrangThai)
            });
    }

    [HttpPut("{maDoiTac}")]
    public async Task<IActionResult> UpdateDoiTac(
        string maDoiTac,
        DoiTacUpdateDto request)
    {
        var maDoiTacDb = FixedLengthHelper.PadTo20(maDoiTac);

        var doiTac = await _context.DoiTacs
            .FirstOrDefaultAsync(item => item.MaDoiTac == maDoiTacDb);

        if (doiTac is null)
        {
            return NotFound(new
            {
                message = $"Không tìm thấy đối tác '{maDoiTac}'."
            });
        }

        if (string.IsNullOrWhiteSpace(request.TenDoiTac) ||
            string.IsNullOrWhiteSpace(request.LoaiDoiTac))
        {
            return BadRequest(new
            {
                message = "Tên đối tác và loại đối tác không được để trống."
            });
        }

        var loaiDoiTac = request.LoaiDoiTac.Trim();

        if (!LoaiDoiTacHopLe.Contains(loaiDoiTac))
        {
            return BadRequest(new
            {
                message = "Loại đối tác không hợp lệ. Giá trị hợp lệ gồm: LuuTru, VanChuyen, AnUong, HoatDong."
            });
        }

        string? maKhuVucDb = null;

        if (!string.IsNullOrWhiteSpace(request.MaKhuVuc))
        {
            maKhuVucDb = FixedLengthHelper.PadTo20(request.MaKhuVuc);

            if (!await _context.KhuVucs.AnyAsync(item => item.MaKhuVuc == maKhuVucDb))
            {
                return BadRequest(new
                {
                    message = "Khu vực không tồn tại."
                });
            }
        }

        doiTac.TenDoiTac = request.TenDoiTac.Trim();
        doiTac.LoaiDoiTac = FixedLengthHelper.PadTo20(loaiDoiTac);
        doiTac.NguoiLienHe = request.NguoiLienHe?.Trim();
        doiTac.SoDienThoai = request.SoDienThoai?.Trim();
        doiTac.Email = request.Email?.Trim();
        doiTac.MaKhuVuc = maKhuVucDb;
        doiTac.PhanTramHoaHong = request.PhanTramHoaHong;
        doiTac.TrangThai = string.IsNullOrWhiteSpace(request.TrangThai)
            ? doiTac.TrangThai
            : FixedLengthHelper.PadTo20(request.TrangThai);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{maDoiTac}")]
    public async Task<IActionResult> DeleteDoiTac(string maDoiTac)
    {
        var maDoiTacDb = FixedLengthHelper.PadTo20(maDoiTac);

        var doiTac = await _context.DoiTacs
            .FirstOrDefaultAsync(item => item.MaDoiTac == maDoiTacDb);

        if (doiTac is null)
        {
            return NotFound(new
            {
                message = $"Không tìm thấy đối tác '{maDoiTac}'."
            });
        }

        var dangDuocThamChieu = await _context.SanPhamDoiTacs
            .AnyAsync(item => item.MaDoiTac == doiTac.MaDoiTac);

        if (dangDuocThamChieu)
        {
            return BadRequest(new
            {
                message = "Không thể xóa đối tác vì đang có sản phẩm đối tác tham chiếu."
            });
        }

        _context.DoiTacs.Remove(doiTac);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}