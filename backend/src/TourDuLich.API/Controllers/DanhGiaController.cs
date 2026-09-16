using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourDuLich.API.Authorization;
using TourDuLich.API.DTOs;
using TourDuLich.API.Services;
using TourDuLich.Application.Helpers;
using TourDuLich.Application.Services;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DanhGiaController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHanhViLogger _hanhViLogger;
    private readonly ITourMediaStorage? _storage;

    public DanhGiaController(AppDbContext context, IHanhViLogger hanhViLogger, ITourMediaStorage? storage = null)
    {
        _context = context;
        _hanhViLogger = hanhViLogger;
        _storage = storage;
    }

    // GET /api/DanhGia/tour/{maTour}
    [HttpGet("tour/{maTour}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetTourReviews(string maTour, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var maTourDb = FixedLengthHelper.PadTo20(maTour);

        var tour = await _context.Tours
            .AsNoTracking()
            .Select(item => new { item.MaTour, item.LoaiTour, item.TenTour })
            .FirstOrDefaultAsync(item => item.MaTour == maTourDb);

        if (tour is null)
        {
            return NotFound(new { message = $"Không tìm thấy tour '{maTour}'." });
        }

        var isSelfDesigned = ReviewDashboardBuilder.IsSelfDesigned(tour.LoaiTour);
        var isStaff = User.IsInRole("Admin") || User.IsInRole("Sale");
        var query = _context.DanhGiaTours
            .AsNoTracking()
            .Where(item => item.MaTour == maTourDb);
        var publicQuery = query.Where(item => item.CongKhai);
        var listQuery = isStaff ? query : publicQuery;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var tongDanhGia = await listQuery.CountAsync();
        var diemTrungBinh = tongDanhGia == 0
            ? (double?)null
            : await listQuery.AverageAsync(item => (double?)(item.SaoDanhGia ?? 0));

        var rawDanhGias = await listQuery
            .OrderByDescending(item => item.ThoiGian)
            .ThenBy(item => item.MaDanhGiaTour)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new
            {
                item.MaDanhGiaTour,
                item.SaoDanhGia,
                item.NhanXet,
                item.ThoiGian,
                item.MaUser,
                item.CongKhai,
                media = item.MediaDanhGiaTours.OrderBy(media => media.ThuTu).Select(media => new
                {
                    url = media.Url,
                    loaiMedia = media.LoaiMedia,
                    thuTu = media.ThuTu
                })
            })
            .ToListAsync();

        var userIds = rawDanhGias.Select(item => item.MaUser).Where(id => id != null).Distinct().ToList();
        var guests = await _context.KhachHangs.AsNoTracking()
            .Where(kh => userIds.Contains(kh.MaUser))
            .Select(kh => new { kh.MaUser, kh.Ho, kh.Ten })
            .ToListAsync();
        var guestMap = guests
            .GroupBy(kh => kh.MaUser)
            .ToDictionary(g => g.Key, g => ReviewDashboardBuilder.GuestName(g.First().Ho, g.First().Ten));

        var danhGias = rawDanhGias.Select(item => new
        {
            maDanhGiaTour = FixedLengthHelper.TrimSafe(item.MaDanhGiaTour),
            saoDanhGia = item.SaoDanhGia,
            nhanXet = item.NhanXet,
            thoiGian = item.ThoiGian,
            tenKhachHang = item.MaUser != null && guestMap.TryGetValue(item.MaUser, out var ten)
                ? ten
                : "Khách ẩn danh",
            congKhai = item.CongKhai,
            media = item.media.Select(media => new
            {
                url = media.url,
                loaiMedia = FixedLengthHelper.TrimSafe(media.loaiMedia),
                thuTu = media.thuTu
            })
        }).ToList();

        // Return only the viewer's review separately, including when it is outside this page.
        var maUser = GetCurrentMaUser();
        var maUserDb = string.IsNullOrWhiteSpace(maUser) ? null : FixedLengthHelper.PadTo20(maUser);
        var danhGiaCuaToi = maUserDb is null ? null : await query
            .Where(item => item.MaUser == maUserDb)
            .Select(item => new
            {
                maDanhGiaTour = FixedLengthHelper.TrimSafe(item.MaDanhGiaTour),
                saoDanhGia = item.SaoDanhGia,
                nhanXet = item.NhanXet,
                thoiGian = item.ThoiGian,
                thoiGianSua = item.ThoiGianSua,
                congKhai = item.CongKhai,
                coTheSua = ReviewDashboardBuilder.CanEdit(item.ThoiGian, DateTime.UtcNow),
                hanSua = ReviewDashboardBuilder.EditDeadline(item.ThoiGian),
                media = item.MediaDanhGiaTours.OrderBy(media => media.ThuTu).Select(media => new
                {
                    url = media.Url,
                    loaiMedia = media.LoaiMedia
                })
            }).FirstOrDefaultAsync();

        return Ok(new
        {
            maTour = FixedLengthHelper.TrimSafe(maTourDb),
            tenTour = tour.TenTour,
            loaiTour = FixedLengthHelper.TrimSafe(tour.LoaiTour),
            noiBo = isSelfDesigned,
            danhGiaCuaToi,
            diemTrungBinh,
            tongDanhGia,
            page,
            pageSize,
            danhGias
        });
    }

    // POST /api/DanhGia/tour
    [HttpPost("tour")]
    [Authorize]
    [Authorize(Roles = "KhachHang")]
    public async Task<ActionResult> CreateTourReview(DanhGiaTourCreateDto request)
    {
        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.MaTour))
        {
            return BadRequest(new { message = "MaTour không được để trống." });
        }

        var saoValidation = ValidateSaoDanhGia(request.SaoDanhGia);

        if (saoValidation is not null)
        {
            return saoValidation;
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maTourDb = FixedLengthHelper.PadTo20(request.MaTour);
        var trangThaiHoanThanh = FixedLengthHelper.PadTo20("HoanThanh");

        var daHoanThanh = await _context.DatDichVus
            .AnyAsync(item =>
                item.MaUser == maUserDb &&
                item.MaTour == maTourDb &&
                item.TrangThai == trangThaiHoanThanh);

        if (!daHoanThanh)
        {
            return BadRequest(new
            {
                message = "Bạn cần hoàn thành tour này trước khi đánh giá."
            });
        }

        var tour = await _context.Tours
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.MaTour == maTourDb);
        if (tour is null)
            return BadRequest(new { message = "Tour không tồn tại." });
        var congKhai = !ReviewDashboardBuilder.IsSelfDesigned(tour.LoaiTour);

        var daDanhGia = await _context.DanhGiaTours
            .AnyAsync(item =>
                item.MaUser == maUserDb &&
                item.MaTour == maTourDb);

        if (daDanhGia)
        {
            return Conflict(new
            {
                message = "Bạn đã đánh giá tour này. Dùng PUT để cập nhật đánh giá."
            });
        }

        var media = ParseMedia(request.MediaUrls, out var mediaError);
        if (mediaError is not null)
            return BadRequest(new { message = mediaError });

        var maDanhGiaTourDb = await GenerateMaDanhGiaTourAsync();

        var danhGia = new DanhGiaTour
        {
            MaDanhGiaTour = maDanhGiaTourDb,
            MaUser = maUserDb,
            MaTour = maTourDb,
            SaoDanhGia = request.SaoDanhGia,
            NhanXet = request.NhanXet?.Trim(),
            ThoiGian = DateTime.UtcNow,
            CongKhai = congKhai
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();
        _context.DanhGiaTours.Add(danhGia);
        await _context.SaveChangesAsync();
        foreach (var item in media)
        {
            _context.MediaDanhGiaTours.Add(new MediaDanhGiaTour
            {
                MaMedia = await GenerateMediaTourIdAsync(),
                MaDanhGiaTour = danhGia.MaDanhGiaTour,
                Url = item.Url,
                LoaiMedia = item.LoaiMedia,
                ThuTu = item.ThuTu
            });
        }
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        await _hanhViLogger.LogAsync(maUserDb, maTourDb, "DanhGiaTour");

        return StatusCode(StatusCodes.Status201Created, new
        {
            maDanhGiaTour = FixedLengthHelper.TrimSafe(danhGia.MaDanhGiaTour),
            maTour = FixedLengthHelper.TrimSafe(danhGia.MaTour),
            saoDanhGia = danhGia.SaoDanhGia,
            nhanXet = danhGia.NhanXet,
            thoiGian = danhGia.ThoiGian,
            congKhai = danhGia.CongKhai
        });
    }

    // POST /api/DanhGia/media/upload
    [HttpPost("media/upload")]
    [Authorize(Roles = "KhachHang")]
    [RequestSizeLimit(104_857_600)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> UploadReviewMedia([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Sale") || User.IsInRole("Admin"))
            return Forbid();
        var maUser = GetCurrentMaUser();
        if (maUser is null) return Unauthorized();
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Chưa chọn file ảnh hoặc video." });
        if (_storage is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Chưa cấu hình lưu media." });
        var validation = await MediaUploadRules.ValidateAsync(file, allowVideo: true, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new { message = validation.Error });
        try
        {
            var uploaded = await _storage.UploadAsync(
                file, $"reviews/{FixedLengthHelper.TrimSafe(maUser)}", validation.Kind, cancellationToken);
            return Ok(new
            {
                url = uploaded.SecureUrl,
                loaiMedia = validation.Kind == TourMediaKind.Image ? "Anh" : "Video",
                publicId = uploaded.PublicId
            });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }

    // PUT /api/DanhGia/tour/{maDanhGiaTour}
    [HttpPut("tour/{maDanhGiaTour}")]
    [Authorize]
    [Authorize(Roles = "KhachHang")]
    public async Task<IActionResult> UpdateTourReview(
        string maDanhGiaTour,
        DanhGiaTourUpdateDto request)
    {
        if (User.IsInRole("Sale") || User.IsInRole("Admin"))
            return Forbid();

        var maUser = GetCurrentMaUser();

        if (maUser is null)
        {
            return Unauthorized();
        }

        var saoValidation = ValidateSaoDanhGia(request.SaoDanhGia);

        if (saoValidation is not null)
        {
            return saoValidation;
        }

        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        var maDanhGiaTourDb = FixedLengthHelper.PadTo20(maDanhGiaTour);

        var danhGia = await _context.DanhGiaTours
            .FirstOrDefaultAsync(item =>
                item.MaDanhGiaTour == maDanhGiaTourDb &&
                item.MaUser == maUserDb);

        if (danhGia is null)
        {
            return NotFound(new { message = "Không tìm thấy đánh giá của bạn." });
        }

        if (!ReviewDashboardBuilder.CanEdit(danhGia.ThoiGian, DateTime.UtcNow))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = $"Chỉ được sửa đánh giá trong {ReviewDashboardBuilder.ReviewEditDays} ngày sau khi gửi."
            });
        }

        danhGia.SaoDanhGia = request.SaoDanhGia;
        danhGia.NhanXet = request.NhanXet?.Trim();
        danhGia.ThoiGianSua = DateTime.UtcNow;

        var media = ParseMedia(request.MediaUrls, out var mediaError);
        if (mediaError is not null)
            return BadRequest(new { message = mediaError });

        await using var transaction = await _context.Database.BeginTransactionAsync();
        if (request.MediaUrls is not null)
        {
            _context.MediaDanhGiaTours.RemoveRange(_context.MediaDanhGiaTours
                .Where(item => item.MaDanhGiaTour == danhGia.MaDanhGiaTour));
            foreach (var item in media)
            {
                _context.MediaDanhGiaTours.Add(new MediaDanhGiaTour
                {
                    MaMedia = await GenerateMediaTourIdAsync(),
                    MaDanhGiaTour = danhGia.MaDanhGiaTour,
                    Url = item.Url,
                    LoaiMedia = item.LoaiMedia,
                    ThuTu = item.ThuTu
                });
            }
        }
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return NoContent();
    }

    [HttpGet("quan-ly")]
    [Authorize(Roles = "Admin,Sale")]
    [RequirePermission(PermissionCatalog.DanhGia, PermissionCatalog.Xem)]
    public async Task<ActionResult> SearchReviews(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var needle = (q ?? string.Empty).Trim();

        var tours = _context.Tours.AsNoTracking();
        var reviews = _context.DanhGiaTours.AsNoTracking();
        var guests = _context.KhachHangs.AsNoTracking();

        IQueryable<string> matchedTours = tours.Select(t => t.MaTour);
        if (!string.IsNullOrEmpty(needle))
        {
            matchedTours = (
                from t in tours
                join r in reviews on t.MaTour equals r.MaTour into tr
                from r in tr.DefaultIfEmpty()
                join k in guests on r.MaUser equals k.MaUser into gk
                from k in gk.DefaultIfEmpty()
                where (t.MaTour != null && t.MaTour.Contains(needle))
                    || (t.TenTour != null && t.TenTour.Contains(needle))
                    || (k.MaKhachHang != null && k.MaKhachHang.Contains(needle))
                    || (k.Ho != null && k.Ho.Contains(needle))
                    || (k.Ten != null && k.Ten.Contains(needle))
                    || ((k.Ho ?? "") + " " + (k.Ten ?? "")).Contains(needle)
                select t.MaTour
            ).Distinct();
        }
        else
        {
            matchedTours = reviews.Select(r => r.MaTour!).Distinct();
        }

        var totalCount = await matchedTours.CountAsync();
        var pageIds = await matchedTours
            .OrderBy(id => id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var stats = await (
            from t in tours
            where pageIds.Contains(t.MaTour)
            join r in reviews on t.MaTour equals r.MaTour into tr
            select new
            {
                maTour = t.MaTour,
                tenTour = t.TenTour,
                loaiTour = t.LoaiTour,
                soDanhGia = tr.Count(),
                soCongKhai = tr.Count(x => x.CongKhai),
                soNoiBo = tr.Count(x => !x.CongKhai),
                diemTrungBinh = tr.Where(x => x.SaoDanhGia != null).Average(x => (double?)x.SaoDanhGia),
                tyLeTieuCuc = tr.Count() == 0 ? 0 : tr.Count(x => (x.SaoDanhGia ?? 0) <= 2) / (double)tr.Count(),
                tyLeTichCuc = tr.Count() == 0 ? 0 : tr.Count(x => (x.SaoDanhGia ?? 0) >= 4) / (double)tr.Count()
            }).ToListAsync();

        var items = pageIds.Select(id =>
        {
            var row = stats.FirstOrDefault(s => s.maTour == id);
            return new
            {
                maTour = FixedLengthHelper.TrimSafe(id),
                tenTour = row?.tenTour,
                loaiTour = FixedLengthHelper.TrimSafe(row?.loaiTour),
                noiBo = ReviewDashboardBuilder.IsSelfDesigned(row?.loaiTour),
                soDanhGia = row?.soDanhGia ?? 0,
                soCongKhai = row?.soCongKhai ?? 0,
                soNoiBo = row?.soNoiBo ?? 0,
                diemTrungBinh = row?.diemTrungBinh is null ? (double?)null : Math.Round(row.diemTrungBinh.Value, 2),
                tyLeTieuCuc = Math.Round(row?.tyLeTieuCuc ?? 0, 4),
                tyLeTichCuc = Math.Round(row?.tyLeTichCuc ?? 0, 4)
            };
        }).ToList();

        return Ok(new { q = needle, page, pageSize, totalCount, items });
    }

    [HttpGet("quan-ly/{maTour}")]
    [Authorize(Roles = "Admin,Sale")]
    [RequirePermission(PermissionCatalog.DanhGia, PermissionCatalog.Xem)]
    public async Task<ActionResult> GetTourReviewsForStaff(string maTour)
    {
        var maTourDb = FixedLengthHelper.PadTo20(maTour);
        var tour = await _context.Tours.AsNoTracking()
            .FirstOrDefaultAsync(item => item.MaTour == maTourDb);
        if (tour is null)
            return NotFound(new { message = $"Không tìm thấy tour '{maTour}'." });

        var raw = await _context.DanhGiaTours.AsNoTracking()
            .Where(item => item.MaTour == maTourDb)
            .OrderByDescending(item => item.ThoiGian)
            .Select(item => new
            {
                item.MaDanhGiaTour,
                item.ThoiGian,
                item.ThoiGianSua,
                item.MaUser,
                item.SaoDanhGia,
                item.NhanXet,
                item.CongKhai,
                media = item.MediaDanhGiaTours.OrderBy(m => m.ThuTu).Select(m => new
                {
                    url = m.Url,
                    loaiMedia = m.LoaiMedia
                })
            }).ToListAsync();

        var userIds = raw.Select(item => item.MaUser).Where(id => id != null).Distinct().ToList();
        var guests = await _context.KhachHangs.AsNoTracking()
            .Where(kh => userIds.Contains(kh.MaUser))
            .Select(kh => new { kh.MaUser, kh.MaKhachHang, kh.Ho, kh.Ten })
            .ToListAsync();
        var guestMap = guests.GroupBy(kh => kh.MaUser)
            .ToDictionary(g => g.Key, g => g.First());

        var danhGias = raw.Select(item =>
        {
            guestMap.TryGetValue(item.MaUser ?? "", out var kh);
            return new
            {
                maDanhGiaTour = FixedLengthHelper.TrimSafe(item.MaDanhGiaTour),
                thoiGian = item.ThoiGian,
                thoiGianSua = item.ThoiGianSua,
                maKhachHang = FixedLengthHelper.TrimSafe(kh?.MaKhachHang),
                tenKhachHang = ReviewDashboardBuilder.GuestName(kh?.Ho, kh?.Ten),
                saoDanhGia = item.SaoDanhGia,
                nhanXet = item.NhanXet,
                congKhai = item.CongKhai,
                media = item.media
            };
        }).ToList();

        return Ok(new
        {
            maTour = FixedLengthHelper.TrimSafe(tour.MaTour),
            tenTour = tour.TenTour,
            loaiTour = FixedLengthHelper.TrimSafe(tour.LoaiTour),
            noiBo = ReviewDashboardBuilder.IsSelfDesigned(tour.LoaiTour),
            soDanhGia = danhGias.Count,
            danhGias
        });
    }

    [HttpGet("thong-ke")]
    [Authorize(Roles = "Admin,Sale")]
    [RequirePermission(PermissionCatalog.DanhGia, PermissionCatalog.Xem)]
    public async Task<ActionResult> ThongKe(CancellationToken cancellationToken)
    {
        var rows = await _context.DanhGiaTours.AsNoTracking()
            .Where(r => r.SaoDanhGia != null && r.ThoiGian != null && r.MaTour != null)
            .Select(r => new ReviewFact(r.SaoDanhGia!.Value, r.ThoiGian!.Value, r.MaTour!, r.CongKhai))
            .ToListAsync(cancellationToken);
        var names = await _context.Tours.AsNoTracking()
            .Select(t => new { t.MaTour, t.TenTour })
            .ToListAsync(cancellationToken);
        var map = names
            .GroupBy(t => t.MaTour)
            .ToDictionary(g => g.Key, g => g.First().TenTour);
        return Ok(ReviewDashboardBuilder.Build(rows, map, DateTime.UtcNow));
    }

    private string? GetCurrentMaUser()
    {
        return User.FindFirst("MaUser")?.Value;
    }

    private static ActionResult? ValidateSaoDanhGia(int saoDanhGia)
    {
        if (saoDanhGia < 1 || saoDanhGia > 5)
        {
            return new BadRequestObjectResult(new
            {
                message = "SaoDanhGia phải nằm trong khoảng 1-5."
            });
        }

        return null;
    }

    private async Task<string> GenerateMaDanhGiaTourAsync()
    {
        string maDanhGiaTourDb;

        do
        {
            var maDanhGiaTour = $"DG{Guid.NewGuid():N}"[..20].ToUpperInvariant();
            maDanhGiaTourDb = FixedLengthHelper.PadTo20(maDanhGiaTour);
        }
        while (await _context.DanhGiaTours
            .AnyAsync(item => item.MaDanhGiaTour == maDanhGiaTourDb));

        return maDanhGiaTourDb;
    }



    private static List<MediaInput> ParseMedia(List<MediaItemDto>? items, out string? error)
    {
        var result = new List<MediaInput>();
        if (items is null)
        {
            error = null;
            return result;
        }
        if (items.Count > 6)
        {
            error = "Tối đa 6 ảnh/video mỗi bài đánh giá.";
            return result;
        }

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Url))
            {
                error = "MediaUrl không được để trống.";
                return result;
            }
            if (item.LoaiMedia is not ("Anh" or "Video"))
            {
                error = "LoaiMedia chỉ nhận Anh hoặc Video.";
                return result;
            }
            result.Add(new MediaInput(item.Url.Trim(), item.LoaiMedia, result.Count));
        }

        error = null;
        return result;
    }

    private async Task<string> GenerateMediaTourIdAsync() => await GenerateMediaIdAsync(
        id => _context.MediaDanhGiaTours.AnyAsync(item => item.MaMedia == id), "MT");


    private sealed record MediaInput(string Url, string LoaiMedia, int ThuTu);
}


