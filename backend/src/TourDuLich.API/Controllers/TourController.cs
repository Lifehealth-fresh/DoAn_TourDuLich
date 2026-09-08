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
    [AllowAnonymous]
    public async Task<ActionResult> GetTours(
        [FromQuery] string? loaiTour = null,
        [FromQuery] string? trangThai = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Tours.AsNoTracking().AsQueryable();

        var loaiTourDb = string.IsNullOrWhiteSpace(loaiTour)
            ? FixedLengthHelper.PadTo20("Chuan")
            : FixedLengthHelper.PadTo20(loaiTour);

        if (string.Equals(loaiTour?.Trim(), "TuThietKe", StringComparison.OrdinalIgnoreCase))
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return Unauthorized();

            if (!User.IsInRole("Sale") && !User.IsInRole("Admin"))
            {
                var maUser = User.FindFirst("MaUser")?.Value;
                if (string.IsNullOrWhiteSpace(maUser))
                    return Unauthorized();

                var maUserDb = FixedLengthHelper.PadTo20(maUser);
                query = query.Where(t => t.LoaiTour == loaiTourDb &&
                    _context.YeuCauThietKes.Any(item =>
                        item.MaUser == maUserDb && item.MaTourTao == t.MaTour));
            }
            else
            {
                query = query.Where(t => t.LoaiTour == loaiTourDb);
            }
        }
        else
        {
            query = query.Where(t => t.LoaiTour == loaiTourDb);
        }

        if (!string.IsNullOrWhiteSpace(trangThai))
            query = query.Where(t => t.TrangThai == FixedLengthHelper.PadTo20(trangThai));

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var totalCount = await query.CountAsync();
        var result = await query.OrderBy(t => t.MaTour)
            .Skip((page - 1) * pageSize).Take(pageSize)
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

        return Ok(new { items = result, page, pageSize, totalCount });
    }

    // GET /api/Tour/{maTour}
    [HttpGet("{maTour}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetTour(string maTour)
    {
        var key = FixedLengthHelper.PadTo20(maTour);

        var tourEntity = await _context.Tours.AsNoTracking()
            .FirstOrDefaultAsync(t => t.MaTour == key);

        if (tourEntity is null ||
            (FixedLengthHelper.TrimSafe(tourEntity.LoaiTour) == "TuThietKe" &&
             !await CanViewPrivateTourAsync(key)))
            return NotFound(new { message = $"Không tìm thấy tour '{maTour}'." });

        var tour = new
        {
            maTour = FixedLengthHelper.TrimSafe(tourEntity.MaTour),
            tenTour = tourEntity.TenTour,
            mota = tourEntity.Mota,
            thoiGian = tourEntity.ThoiGian,
            dieuKhoan = tourEntity.DieuKhoan,
            giaTour = tourEntity.GiaTour,
            slkhach = tourEntity.Slkhach,
            slhuongDanVien = tourEntity.SlhuongDanVien,
            loaiTour = FixedLengthHelper.TrimSafe(tourEntity.LoaiTour),
            trangThai = FixedLengthHelper.TrimSafe(tourEntity.TrangThai)
        };

        return Ok(tour);
    }

    // GET /api/Tour/{maTour}/lich-khoi-hanh
    [HttpGet("{maTour}/lich-khoi-hanh")]
    [AllowAnonymous]
    public async Task<ActionResult> GetLichKhoiHanh(string maTour)
    {
        var key = FixedLengthHelper.PadTo20(maTour);

        var tour = await _context.Tours.AsNoTracking()
            .FirstOrDefaultAsync(t => t.MaTour == key);
        if (tour is null ||
            (FixedLengthHelper.TrimSafe(tour.LoaiTour) == "TuThietKe" &&
             !await CanViewPrivateTourAsync(key)))
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
    [AllowAnonymous]
    public async Task<ActionResult> GetAnhTour(string maTour)
    {
        var key = FixedLengthHelper.PadTo20(maTour);

        var tour = await _context.Tours.AsNoTracking()
            .FirstOrDefaultAsync(t => t.MaTour == key);
        if (tour is null ||
            (FixedLengthHelper.TrimSafe(tour.LoaiTour) == "TuThietKe" &&
             !await CanViewPrivateTourAsync(key)))
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
                url = x.Url ?? x.ImageUrl,
                loaiMedia = FixedLengthHelper.TrimSafe(x.LoaiMedia),
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

    // PUT /api/Tour/{maTour} — chặn sửa GiaTour/DieuKhoan khi đã có HopDong DaKy (snapshot bất biến sau ký)
    [HttpPut("{maTour}")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<IActionResult> UpdateTour(string maTour, [FromBody] TourUpdateDto dto)
    {
        var key = FixedLengthHelper.PadTo20(maTour);
        var existing = await _context.Tours.FindAsync(key);
        if (existing == null)
            return NotFound();

        var daKy = FixedLengthHelper.PadTo20("DaKy");
        var hasSignedContract = await _context.HopDongs
            .AnyAsync(h => h.MaBookingNavigation.MaTour == key && h.TrangThai == daKy);
        if (hasSignedContract && (dto.GiaTour != existing.GiaTour || dto.DieuKhoan != existing.DieuKhoan))
        {
            return BadRequest(new { message = "Tour đã có hợp đồng đã ký (DaKy) — không được đổi Giá/Điều khoản. Hãy lập phụ lục hợp đồng thay vì sửa Tour gốc." });
        }

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

    private async Task<bool> CanViewPrivateTourAsync(string maTourDb)
    {
        if (User.IsInRole("Sale") || User.IsInRole("Admin"))
            return true;

        var maUser = User.FindFirst("MaUser")?.Value;
        if (string.IsNullOrWhiteSpace(maUser))
            return false;

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        return await _context.YeuCauThietKes.AnyAsync(item =>
            item.MaUser == maUserDb && item.MaTourTao == maTourDb);
    }
}
