using Microsoft.AspNetCore.Authorization;
using TourDuLich.API.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TourController : ControllerBase
{
    private readonly AppDbContext _context;

    public TourController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/Tour
    [HttpGet]
    public async Task<ActionResult> GetTours(
        [FromQuery] string? loaiTour = null,
        [FromQuery] string? trangThai = null)
    {
        var query = _context.Tours.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(loaiTour))
            query = query.Where(t => t.LoaiTour == FixedLengthHelper.PadTo20(loaiTour));

        if (!string.IsNullOrWhiteSpace(trangThai))
            query = query.Where(t => t.TrangThai == FixedLengthHelper.PadTo20(trangThai));

        var result = await query
            .Select(t => new
            {
                maTour = FixedLengthHelper.TrimSafe(t.MaTour),
                tenTour = t.TenTour,
                mota = t.Mota,
                thoiGian = t.ThoiGian,
                dieuKhoan = t.DieuKhoan,
                giaTour = t.GiaTour,
                slkhach = t.Slkhach,
                slhuongDanVien = t.SlhuongDanVien,
                loaiTour = FixedLengthHelper.TrimSafe(t.LoaiTour),
                trangThai = FixedLengthHelper.TrimSafe(t.TrangThai)
            })
            .ToListAsync();

        return Ok(result);
    }

    // GET /api/Tour/{maTour}
    [HttpGet("{maTour}")]
    public async Task<ActionResult> GetTour(string maTour)
    {
        var key = FixedLengthHelper.PadTo20(maTour);

        var tour = await _context.Tours
            .AsNoTracking()
            .Where(t => t.MaTour == key)
            .Select(t => new
            {
                maTour = FixedLengthHelper.TrimSafe(t.MaTour),
                tenTour = t.TenTour,
                mota = t.Mota,
                thoiGian = t.ThoiGian,
                dieuKhoan = t.DieuKhoan,
                giaTour = t.GiaTour,
                slkhach = t.Slkhach,
                slhuongDanVien = t.SlhuongDanVien,
                loaiTour = FixedLengthHelper.TrimSafe(t.LoaiTour),
                trangThai = FixedLengthHelper.TrimSafe(t.TrangThai)
            })
            .FirstOrDefaultAsync();

        if (tour == null)
            return NotFound(new { message = $"Không tìm thấy tour '{maTour}'." });

        return Ok(tour);
    }

    // GET /api/Tour/{maTour}/lich-khoi-hanh
    [HttpGet("{maTour}/lich-khoi-hanh")]
    public async Task<ActionResult> GetLichKhoiHanh(string maTour)
    {
        var key = FixedLengthHelper.PadTo20(maTour);

        if (!await _context.Tours.AnyAsync(t => t.MaTour == key))
            return NotFound(new { message = $"Không tìm thấy tour '{maTour}'." });

        var result = await _context.LichKhoiHanhs
            .AsNoTracking()
            .Where(x => x.MaTour == key)
            .Select(x => new
            {
                maKhoiHanh = FixedLengthHelper.TrimSafe(x.MaKhoiHanh),
                maTour = FixedLengthHelper.TrimSafe(x.MaTour),
                ngayKhoiHanh = x.NgayKhoiHanh,
                ngayKetThuc = x.NgayKetThuc,
                diaDiem = x.DiaDiem
            })
            .ToListAsync();

        return Ok(result);
    }

    // GET /api/Tour/{maTour}/anh
    [HttpGet("{maTour}/anh")]
    public async Task<ActionResult> GetAnhTour(string maTour)
    {
        var key = FixedLengthHelper.PadTo20(maTour);

        if (!await _context.Tours.AnyAsync(t => t.MaTour == key))
            return NotFound(new { message = $"Không tìm thấy tour '{maTour}'." });

        var result = await _context.AnhTours
            .AsNoTracking()
            .Where(x => x.MaTour == key)
            .OrderBy(x => x.ThuTu)
            .Select(x => new
            {
                maAnhTour = FixedLengthHelper.TrimSafe(x.MaAnhTour),
                maTour = FixedLengthHelper.TrimSafe(x.MaTour),
                imageUrl = x.ImageUrl,
                thuTu = x.ThuTu,
                isAvatar = x.IsAvatar
            })
            .ToListAsync();

        return Ok(result);
    }

    // POST /api/Tour
    [HttpPost]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> CreateTour([FromBody] TourCreateDto dto)
    {
        var maTour = FixedLengthHelper.PadTo20(dto.MaTour);

        if (await _context.Tours.AnyAsync(t => t.MaTour == maTour))
            return Conflict(new { message = "MaTour đã tồn tại." });

        var tour = new Tour
        {
            MaTour = maTour,
            TenTour = dto.TenTour,
            Mota = dto.Mota,
            ThoiGian = dto.ThoiGian,
            DieuKhoan = dto.DieuKhoan,
            GiaTour = dto.GiaTour,
            Slkhach = dto.Slkhach,
            SlhuongDanVien = dto.SlhuongDanVien,
            LoaiTour = FixedLengthHelper.PadTo20(string.IsNullOrWhiteSpace(dto.LoaiTour) ? "Chuan" : dto.LoaiTour),
            TrangThai = FixedLengthHelper.PadTo20(string.IsNullOrWhiteSpace(dto.TrangThai) ? "HoatDong" : dto.TrangThai)
        };

        _context.Tours.Add(tour);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTour), new { maTour = FixedLengthHelper.TrimSafe(tour.MaTour) }, new
        {
            maTour = FixedLengthHelper.TrimSafe(tour.MaTour),
            tenTour = tour.TenTour,
            mota = tour.Mota,
            thoiGian = tour.ThoiGian,
            dieuKhoan = tour.DieuKhoan,
            giaTour = tour.GiaTour,
            slkhach = tour.Slkhach,
            slhuongDanVien = tour.SlhuongDanVien,
            loaiTour = FixedLengthHelper.TrimSafe(tour.LoaiTour),
            trangThai = FixedLengthHelper.TrimSafe(tour.TrangThai)
        });
    }

    // PUT /api/Tour/{maTour}
    [HttpPut("{maTour}")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<IActionResult> UpdateTour(string maTour, [FromBody] TourUpdateDto dto)
    {
        var existing = await _context.Tours.FindAsync(FixedLengthHelper.PadTo20(maTour));
        if (existing == null)
            return NotFound();

        existing.TenTour = dto.TenTour;
        existing.Mota = dto.Mota;
        existing.ThoiGian = dto.ThoiGian;
        existing.DieuKhoan = dto.DieuKhoan;
        existing.GiaTour = dto.GiaTour;
        existing.Slkhach = dto.Slkhach;
        existing.SlhuongDanVien = dto.SlhuongDanVien;
        existing.LoaiTour = FixedLengthHelper.PadTo20(dto.LoaiTour);
        existing.TrangThai = dto.TrangThai is null ? existing.TrangThai : FixedLengthHelper.PadTo20(dto.TrangThai);

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/Tour/{maTour}
    [HttpDelete("{maTour}")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<IActionResult> DeleteTour(string maTour)
    {
        var existing = await _context.Tours.FindAsync(FixedLengthHelper.PadTo20(maTour));
        if (existing == null)
            return NotFound();

        var hasBooking = await _context.DatDichVus.AnyAsync(d => d.MaTour == existing.MaTour);
        if (hasBooking)
        {
            existing.TrangThai = FixedLengthHelper.PadTo20("An");
            await _context.SaveChangesAsync();
            return Ok(new { message = "Tour đã có booking — đã chuyển sang trạng thái ẩn." });
        }

        _context.Tours.Remove(existing);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/Tour/tu-thiet-ke
    [HttpPost("tu-thiet-ke")]
    [Authorize]
    public async Task<ActionResult> CreateSelfDesignedTour(
        TuThietKeRequestDto request)
    {
        var maUser = User.FindFirst("MaUser")?.Value;

        if (maUser is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.MaYeuCau))
        {
            return BadRequest(new
            {
                message = "Mã yêu cầu thiết kế không được để trống."
            });
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maYeuCauDb = FixedLengthHelper.PadTo20(request.MaYeuCau);

        var requestData = await _context.YeuCauThietKes
            .Where(item =>
                item.MaYeuCau == maYeuCauDb &&
                item.MaUser == maUserDb)
            .Select(item => new
            {
                Request = item,
                Destination = item.DiemDenMongMuon,
                SoNguoiLon = item.SoNguoiLon,
                SoTreEm = item.SoTreEm,
                TrangThai = item.TrangThai,
                MaTourTao = item.MaTourTao
            })
            .FirstOrDefaultAsync();

        if (requestData is null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy yêu cầu thiết kế của bạn."
            });
        }

        if (FixedLengthHelper.TrimSafe(requestData.TrangThai) != "Moi")
        {
            return BadRequest(new
            {
                message = "Chỉ yêu cầu đang ở trạng thái Moi mới có thể tạo tour."
            });
        }

        if (!string.IsNullOrWhiteSpace(requestData.MaTourTao))
        {
            return Conflict(new
            {
                message = "Yêu cầu này đã được tạo tour."
            });
        }

        var soNguoiLon = requestData.SoNguoiLon ?? 0;
        var soTreEm = requestData.SoTreEm ?? 0;

        string maTour;
        string maTourDb;

        do
        {
            maTour = $"TD{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            maTourDb = FixedLengthHelper.PadTo20(maTour);
        }
        while (await _context.Tours
            .AnyAsync(item => item.MaTour == maTourDb));

        var tenTour = $"Tour tự thiết kế - {requestData.Destination}".Trim();

        if (tenTour.Length > 150)
        {
            tenTour = tenTour[..150];
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var tour = new Tour
        {
            MaTour = maTourDb,
            TenTour = tenTour,
            GiaTour = 0,
            Slkhach = soNguoiLon + soTreEm,
            LoaiTour = FixedLengthHelper.PadTo20("TuThietKe"),
            TrangThai = FixedLengthHelper.PadTo20("Nhap")
        };

        _context.Tours.Add(tour);

        requestData.Request.MaTourTao = maTourDb;
        requestData.Request.TrangThai =
            FixedLengthHelper.PadTo20("DangThietKe");

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            maTour = FixedLengthHelper.TrimSafe(tour.MaTour),
            maYeuCau = FixedLengthHelper.TrimSafe(requestData.Request.MaYeuCau),
            tenTour = tour.TenTour,
            loaiTour = FixedLengthHelper.TrimSafe(tour.LoaiTour),
            trangThai = FixedLengthHelper.TrimSafe(tour.TrangThai),
            slkhach = tour.Slkhach,
            giaTour = tour.GiaTour
        });
    }
}
