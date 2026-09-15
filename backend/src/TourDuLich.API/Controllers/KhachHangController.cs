using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourDuLich.API.Authorization;
using TourDuLich.API.DTOs;
using TourDuLich.API.Services;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KhachHangController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITourMediaStorage? _storage;

    public KhachHangController(AppDbContext context, ITourMediaStorage? storage = null)
    {
        _context = context;
        _storage = storage;
    }

    [HttpGet("quan-ly")]
    [Authorize(Roles = "Sale,Admin")]
    [RequirePermission(PermissionCatalog.KhachHang, PermissionCatalog.Xem)]
    public async Task<ActionResult> SearchStaff([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var needle = (q ?? string.Empty).Trim();
        var query = _context.KhachHangs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(needle))
        {
            var padded = FixedLengthHelper.PadTo20(needle);
            query = query.Where(item =>
                item.MaKhachHang == padded ||
                (item.SoDienThoai != null && item.SoDienThoai.Contains(needle)) ||
                ((item.Ho ?? "") + " " + (item.Ten ?? "")).Contains(needle) ||
                (item.Email != null && item.Email.Contains(needle)));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(item => item.Ho).ThenBy(item => item.Ten)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new
            {
                maKhachHang = FixedLengthHelper.TrimSafe(item.MaKhachHang),
                ho = item.Ho,
                ten = item.Ten,
                email = item.Email,
                soDienThoai = FixedLengthHelper.TrimSafe(item.SoDienThoai),
                quocTich = item.QuocTich,
                ngaySinh = item.NgaySinh,
                soGiayTo = item.GiayTos.Count(),
                soChuyenDi = _context.DatDichVus.Count(b => b.MaKhachHang == item.MaKhachHang || b.MaUser == item.MaUser)
            }).ToListAsync();
        return Ok(new { q = needle, page, pageSize, totalCount, items });
    }

    [HttpGet("quan-ly/{maKhachHang}")]
    [Authorize(Roles = "Sale,Admin")]
    [RequirePermission(PermissionCatalog.KhachHang, PermissionCatalog.Xem)]
    public async Task<ActionResult> StaffDetail(string maKhachHang)
    {
        var key = FixedLengthHelper.PadTo20(maKhachHang);
        var profile = await _context.KhachHangs.AsNoTracking()
            .Include(item => item.GiayTos)
            .FirstOrDefaultAsync(item => item.MaKhachHang == key);
        if (profile is null)
            return NotFound(new { message = "Không tìm thấy hồ sơ khách hàng." });

        var trips = await (
            from b in _context.DatDichVus.AsNoTracking()
            join t in _context.Tours.AsNoTracking() on b.MaTour equals t.MaTour
            where b.MaKhachHang == profile.MaKhachHang || b.MaUser == profile.MaUser
            orderby b.NgayDat descending
            select new
            {
                maBooking = FixedLengthHelper.TrimSafe(b.MaBooking),
                maTour = FixedLengthHelper.TrimSafe(b.MaTour),
                tenTour = t.TenTour,
                maKhoiHanh = FixedLengthHelper.TrimSafe(b.MaKhoiHanh),
                ngayDat = b.NgayDat,
                trangThai = FixedLengthHelper.TrimSafe(b.TrangThai),
                slnguoiLon = b.SlnguoiLon,
                sltreEm = b.SltreEm,
                thanhTien = b.ThanhTien
            }).ToListAsync();

        return Ok(new
        {
            hoSo = ToProfile(profile),
            chuyenDi = trips
        });
    }

    [HttpGet]
    [Authorize(Roles = "KhachHang")]
    public async Task<ActionResult> GetMine()
    {
        var maUserDb = CurrentUserDb();
        if (maUserDb is null) return Unauthorized();

        var profiles = await _context.KhachHangs
            .AsNoTracking()
            .Include(item => item.GiayTos)
            .Where(item => item.MaUser == maUserDb)
            .OrderBy(item => item.MaKhachHang)
            .ToListAsync();

        return Ok(profiles.Select(ToProfile));
    }

    [HttpGet("{maKhachHang}")]
    [Authorize(Roles = "KhachHang")]
    public async Task<ActionResult> GetOne(string maKhachHang)
    {
        var maUserDb = CurrentUserDb();
        if (maUserDb is null) return Unauthorized();

        var profile = await _context.KhachHangs
            .AsNoTracking()
            .Include(item => item.GiayTos)
            .FirstOrDefaultAsync(item =>
                item.MaKhachHang == FixedLengthHelper.PadTo20(maKhachHang) &&
                item.MaUser == maUserDb);

        return profile is null
            ? NotFound(new { message = "Không tìm thấy hồ sơ thuộc tài khoản của bạn." })
            : Ok(ToProfile(profile));
    }

    [HttpPost]
    [Authorize(Roles = "KhachHang")]
    public async Task<ActionResult> Create(KhachHangCreateDto request)
    {
        var maUserDb = CurrentUserDb();
        if (maUserDb is null) return Unauthorized();

        return await CreateForUser(maUserDb, request);
    }

    [HttpPut("{maKhachHang}")]
    [Authorize(Roles = "KhachHang")]
    public async Task<ActionResult> Update(
        string maKhachHang,
        KhachHangUpdateDto request)
    {
        var maUserDb = CurrentUserDb();
        if (maUserDb is null) return Unauthorized();

        var validation = ValidateProfile(request.Ho, request.Ten);
        if (validation is not null) return validation;

        var profile = await _context.KhachHangs.FirstOrDefaultAsync(item =>
            item.MaKhachHang == FixedLengthHelper.PadTo20(maKhachHang) &&
            item.MaUser == maUserDb);

        if (profile is null)
        {
            return NotFound(new { message = "Không tìm thấy hồ sơ thuộc tài khoản của bạn." });
        }

        Apply(profile, request);
        await _context.SaveChangesAsync();
        return Ok(ToProfile(profile));
    }

    [HttpDelete("{maKhachHang}")]
    [Authorize(Roles = "KhachHang")]
    public async Task<IActionResult> Delete(string maKhachHang)
    {
        var maUserDb = CurrentUserDb();
        if (maUserDb is null) return Unauthorized();

        var profile = await _context.KhachHangs.FirstOrDefaultAsync(item =>
            item.MaKhachHang == FixedLengthHelper.PadTo20(maKhachHang) &&
            item.MaUser == maUserDb);

        if (profile is null)
        {
            return NotFound(new { message = "Không tìm thấy hồ sơ thuộc tài khoản của bạn." });
        }

        var documents = await _context.GiayTos
            .Where(item => item.MaKhachHang == profile.MaKhachHang)
            .ToListAsync();

        _context.GiayTos.RemoveRange(documents);
        _context.KhachHangs.Remove(profile);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{maKhachHang}/giay-to")]
    [Authorize(Roles = "KhachHang")]
    public async Task<ActionResult> AddDocument(
        string maKhachHang,
        GiayToCreateDto request)
    {
        var maUserDb = CurrentUserDb();
        if (maUserDb is null) return Unauthorized();

        var profile = await OwnedProfile(maKhachHang, maUserDb);
        if (profile is null) return NotFound(new { message = "Không tìm thấy hồ sơ thuộc tài khoản của bạn." });

        return await AddDocumentToProfile(profile, request);
    }

    [HttpGet("{maKhachHang}/giay-to")]
    [Authorize(Roles = "KhachHang")]
    public async Task<ActionResult> GetDocuments(string maKhachHang)
    {
        var maUserDb = CurrentUserDb();
        if (maUserDb is null) return Unauthorized();

        var profile = await OwnedProfile(maKhachHang, maUserDb);
        if (profile is null) return NotFound(new { message = "Không tìm thấy hồ sơ thuộc tài khoản của bạn." });

        var documents = await _context.GiayTos
            .Where(item => item.MaKhachHang == profile.MaKhachHang)
            .ToListAsync();

        return Ok(documents.Select(ToDocument));
    }

    [HttpPut("{maKhachHang}/giay-to/{maGiayTo}")]
    [Authorize(Roles = "KhachHang")]
    public async Task<ActionResult> UpdateDocument(
        string maKhachHang,
        string maGiayTo,
        GiayToUpdateDto request)
    {
        var maUserDb = CurrentUserDb();
        if (maUserDb is null) return Unauthorized();

        var validation = ValidateDocument(request);
        if (validation is not null) return validation;

        var document = await _context.GiayTos.FirstOrDefaultAsync(item =>
            item.MaGiayTo == FixedLengthHelper.PadTo20(maGiayTo) &&
            item.MaKhachHang == FixedLengthHelper.PadTo20(maKhachHang) &&
            item.MaKhachHangNavigation.MaUser == maUserDb);

        if (document is null)
        {
            return NotFound(new { message = "Không tìm thấy giấy tờ thuộc hồ sơ của bạn." });
        }

        Apply(document, request);
        await _context.SaveChangesAsync();
        return Ok(ToDocument(document));
    }

    [HttpDelete("{maKhachHang}/giay-to/{maGiayTo}")]
    [Authorize(Roles = "KhachHang")]
    public async Task<IActionResult> DeleteDocument(string maKhachHang, string maGiayTo)
    {
        var maUserDb = CurrentUserDb();
        if (maUserDb is null) return Unauthorized();

        var document = await _context.GiayTos.FirstOrDefaultAsync(item =>
            item.MaGiayTo == FixedLengthHelper.PadTo20(maGiayTo) &&
            item.MaKhachHang == FixedLengthHelper.PadTo20(maKhachHang) &&
            item.MaKhachHangNavigation.MaUser == maUserDb);

        if (document is null)
        {
            return NotFound(new { message = "Không tìm thấy giấy tờ thuộc hồ sơ của bạn." });
        }

        _context.GiayTos.Remove(document);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("tim-kiem")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> Search([FromQuery] string soDienThoai)
    {
        if (string.IsNullOrWhiteSpace(soDienThoai))
            return BadRequest(new { message = "Số điện thoại không được để trống." });

        var phoneDb = FixedLengthHelper.PadTo20(soDienThoai.Trim());
        var user = await _context.NguoiSuDungs.FirstOrDefaultAsync(item => item.SoDienThoai == phoneDb);

        if (user is null) return NotFound(new { message = "Không tìm thấy tài khoản." });

        var profiles = await _context.KhachHangs
            .AsNoTracking()
            .Include(item => item.GiayTos)
            .Where(item => item.MaUser == user.MaUser)
            .ToListAsync();

        return Ok(profiles.Select(ToProfile));
    }

    [HttpPost("sale/{maUser}")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> SaleCreate(string maUser, KhachHangCreateDto request)
    {
        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        if (!await _context.NguoiSuDungs.AnyAsync(item => item.MaUser == maUserDb))
            return NotFound(new { message = "Không tìm thấy tài khoản khách hàng." });

        return await CreateForUser(maUserDb, request);
    }

    [HttpPut("sale/{maKhachHang}")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> SaleUpdate(string maKhachHang, KhachHangUpdateDto request)
    {
        var validation = ValidateProfile(request.Ho, request.Ten);
        if (validation is not null) return validation;

        var profile = await _context.KhachHangs.FirstOrDefaultAsync(item =>
            item.MaKhachHang == FixedLengthHelper.PadTo20(maKhachHang));

        if (profile is null) return NotFound(new { message = "Không tìm thấy hồ sơ khách hàng." });

        Apply(profile, request);
        await _context.SaveChangesAsync();
        return Ok(ToProfile(profile));
    }

    [HttpPost("sale/{maKhachHang}/giay-to")]
    [Authorize(Roles = "Sale,Admin")]
    [RequirePermission(PermissionCatalog.KhachHang, PermissionCatalog.Them)]
    public async Task<ActionResult> SaleAddDocument(string maKhachHang, GiayToCreateDto request)
    {
        var profile = await _context.KhachHangs.FirstOrDefaultAsync(item =>
            item.MaKhachHang == FixedLengthHelper.PadTo20(maKhachHang));

        if (profile is null) return NotFound(new { message = "Không tìm thấy hồ sơ khách hàng." });
        return await AddDocumentToProfile(profile, request);
    }

    [HttpPut("sale/{maKhachHang}/giay-to/{maGiayTo}")]
    [Authorize(Roles = "Sale,Admin")]
    [RequirePermission(PermissionCatalog.KhachHang, PermissionCatalog.Sua)]
    public async Task<ActionResult> SaleUpdateDocument(
        string maKhachHang,
        string maGiayTo,
        GiayToUpdateDto request)
    {
        var validation = ValidateDocument(request);
        if (validation is not null) return validation;

        var document = await _context.GiayTos.FirstOrDefaultAsync(item =>
            item.MaGiayTo == FixedLengthHelper.PadTo20(maGiayTo) &&
            item.MaKhachHang == FixedLengthHelper.PadTo20(maKhachHang));

        if (document is null) return NotFound(new { message = "Không tìm thấy giấy tờ." });

        Apply(document, request);
        await _context.SaveChangesAsync();
        return Ok(ToDocument(document));
    }

    [HttpDelete("sale/{maKhachHang}/giay-to/{maGiayTo}")]
    [Authorize(Roles = "Sale,Admin")]
    [RequirePermission(PermissionCatalog.KhachHang, PermissionCatalog.Xoa)]
    public async Task<IActionResult> SaleDeleteDocument(string maKhachHang, string maGiayTo)
    {
        var document = await _context.GiayTos.FirstOrDefaultAsync(item =>
            item.MaGiayTo == FixedLengthHelper.PadTo20(maGiayTo) &&
            item.MaKhachHang == FixedLengthHelper.PadTo20(maKhachHang));

        if (document is null) return NotFound(new { message = "Không tìm thấy giấy tờ." });

        _context.GiayTos.Remove(document);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{maKhachHang}/giay-to/{maGiayTo}/anh")]
    [Authorize(Roles = "KhachHang")]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult> UploadDocumentImage(
        string maKhachHang, string maGiayTo, [FromForm] IFormFile file, [FromQuery] string mat = "Truoc",
        CancellationToken cancellationToken = default)
    {
        var maUserDb = CurrentUserDb();
        if (maUserDb is null) return Unauthorized();
        var document = await _context.GiayTos.FirstOrDefaultAsync(item =>
            item.MaGiayTo == FixedLengthHelper.PadTo20(maGiayTo) &&
            item.MaKhachHang == FixedLengthHelper.PadTo20(maKhachHang) &&
            item.MaKhachHangNavigation.MaUser == maUserDb, cancellationToken);
        if (document is null)
            return NotFound(new { message = "Không tìm thấy giấy tờ thuộc hồ sơ của bạn." });
        return await SaveDocumentImage(document, file, mat, cancellationToken);
    }

    [HttpPost("sale/{maKhachHang}/giay-to/{maGiayTo}/anh")]
    [Authorize(Roles = "Sale,Admin")]
    [RequirePermission(PermissionCatalog.KhachHang, PermissionCatalog.Sua)]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult> StaffUploadDocumentImage(
        string maKhachHang, string maGiayTo, [FromForm] IFormFile file, [FromQuery] string mat = "Truoc",
        CancellationToken cancellationToken = default)
    {
        var document = await _context.GiayTos.FirstOrDefaultAsync(item =>
            item.MaGiayTo == FixedLengthHelper.PadTo20(maGiayTo) &&
            item.MaKhachHang == FixedLengthHelper.PadTo20(maKhachHang), cancellationToken);
        if (document is null)
            return NotFound(new { message = "Không tìm thấy giấy tờ." });
        return await SaveDocumentImage(document, file, mat, cancellationToken);
    }

    private async Task<ActionResult> SaveDocumentImage(
        GiayTo document, IFormFile file, string mat, CancellationToken cancellationToken)
    {
        var side = (mat ?? "Truoc").Trim();
        if (side is not ("Truoc" or "Sau"))
            return BadRequest(new { message = "mat chỉ nhận Truoc hoặc Sau." });
        if (_storage is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Chưa cấu hình lưu media." });
        var validation = await MediaUploadRules.ValidateAsync(file, allowVideo: false, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new { message = validation.Error });
        StoredTourMedia uploaded;
        try
        {
            uploaded = await _storage.UploadAsync(
                file, $"giay-to/{FixedLengthHelper.TrimSafe(document.MaGiayTo)}", validation.Kind, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }

        if (side == "Truoc")
        {
            document.AnhMatTruoc = uploaded.SecureUrl;
            document.CloudPublicIdTruoc = uploaded.PublicId;
        }
        else
        {
            document.AnhMatSau = uploaded.SecureUrl;
            document.CloudPublicIdSau = uploaded.PublicId;
        }
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ToDocument(document));
    }

    private async Task<ActionResult> CreateForUser(string maUserDb, KhachHangCreateDto request)
    {
        var validation = ValidateProfile(request.Ho, request.Ten);
        if (validation is not null) return validation;

        var user = await _context.NguoiSuDungs.FirstOrDefaultAsync(item => item.MaUser == maUserDb);
        if (user is null) return Unauthorized();

        var phone = FixedLengthHelper.TrimSafe(user.SoDienThoai) ?? string.Empty;
        if (phone.Length > 15)
            return BadRequest(new { message = "Số điện thoại tài khoản vượt quá giới hạn 15 ký tự của hồ sơ khách hàng." });

        var profile = new KhachHang
        {
            MaKhachHang = await GenerateIdAsync("KH", _context.KhachHangs.Select(item => item.MaKhachHang)),
            Ho = request.Ho.Trim(),
            Ten = request.Ten.Trim(),
            HoGiayTo = request.HoGiayTo?.Trim(),
            TenGiayTo = request.TenGiayTo?.Trim(),
            QuocTich = request.QuocTich?.Trim(),
            DanhXung = request.DanhXung?.Trim(),
            GioiTinh = request.GioiTinh?.Trim(),
            NgaySinh = request.NgaySinh,
            Email = request.Email?.Trim(),
            SoDienThoai = phone,
            MaUser = maUserDb
        };

        _context.KhachHangs.Add(profile);
        await _context.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created, ToProfile(profile));
    }

    private async Task<ActionResult> AddDocumentToProfile(KhachHang profile, GiayToCreateDto request)
    {
        var validation = ValidateDocument(request);
        if (validation is not null) return validation;

        var document = new GiayTo
        {
            MaGiayTo = await GenerateIdAsync("GT", _context.GiayTos.Select(item => item.MaGiayTo)),
            LoaiGiayTo = request.LoaiGiayTo.Trim(),
            SoTrenGiayTo = request.SoTrenGiayTo.Trim(),
            NgayCap = request.NgayCap,
            NgayHetHan = request.NgayHetHan,
            NoiCap = request.NoiCap.Trim(),
            MaKhachHang = profile.MaKhachHang
        };

        _context.GiayTos.Add(document);
        await _context.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created, ToDocument(document));
    }

    private async Task<KhachHang?> OwnedProfile(string maKhachHang, string maUserDb)
    {
        return await _context.KhachHangs.FirstOrDefaultAsync(item =>
            item.MaKhachHang == FixedLengthHelper.PadTo20(maKhachHang) &&
            item.MaUser == maUserDb);
    }

    private string? CurrentUserDb()
    {
        var maUser = User.FindFirst("MaUser")?.Value;
        return string.IsNullOrWhiteSpace(maUser) ? null : FixedLengthHelper.PadTo20(maUser);
    }

    private static ActionResult? ValidateProfile(string? ho, string? ten)
    {
        return string.IsNullOrWhiteSpace(ho) || string.IsNullOrWhiteSpace(ten)
            ? new BadRequestObjectResult(new { message = "Họ và tên không được để trống." })
            : null;
    }

    private static ActionResult? ValidateDocument(GiayToCreateDto request)
    {
        if (string.IsNullOrWhiteSpace(request.LoaiGiayTo) ||
            string.IsNullOrWhiteSpace(request.SoTrenGiayTo) ||
            string.IsNullOrWhiteSpace(request.NoiCap))
            return new BadRequestObjectResult(new { message = "Thông tin giấy tờ không được để trống." });

        return request.NgayHetHan < request.NgayCap
            ? new BadRequestObjectResult(new { message = "Ngày hết hạn phải sau hoặc bằng ngày cấp." })
            : null;
    }

    private static ActionResult? ValidateDocument(GiayToUpdateDto request)
    {
        if (string.IsNullOrWhiteSpace(request.LoaiGiayTo) ||
            string.IsNullOrWhiteSpace(request.SoTrenGiayTo) ||
            string.IsNullOrWhiteSpace(request.NoiCap))
            return new BadRequestObjectResult(new { message = "Thông tin giấy tờ không được để trống." });

        return request.NgayHetHan < request.NgayCap
            ? new BadRequestObjectResult(new { message = "Ngày hết hạn phải sau hoặc bằng ngày cấp." })
            : null;
    }

    private static void Apply(KhachHang profile, KhachHangCreateDto request)
    {
        profile.Ho = request.Ho.Trim();
        profile.Ten = request.Ten.Trim();
        profile.HoGiayTo = request.HoGiayTo?.Trim();
        profile.TenGiayTo = request.TenGiayTo?.Trim();
        profile.QuocTich = request.QuocTich?.Trim();
        profile.DanhXung = request.DanhXung?.Trim();
        profile.GioiTinh = request.GioiTinh?.Trim();
        profile.NgaySinh = request.NgaySinh;
        profile.Email = request.Email?.Trim();
    }

    private static void Apply(KhachHang profile, KhachHangUpdateDto request)
    {
        profile.Ho = request.Ho.Trim();
        profile.Ten = request.Ten.Trim();
        profile.HoGiayTo = request.HoGiayTo?.Trim();
        profile.TenGiayTo = request.TenGiayTo?.Trim();
        profile.QuocTich = request.QuocTich?.Trim();
        profile.DanhXung = request.DanhXung?.Trim();
        profile.GioiTinh = request.GioiTinh?.Trim();
        profile.NgaySinh = request.NgaySinh;
        profile.Email = request.Email?.Trim();
    }

    private static void Apply(GiayTo document, GiayToCreateDto request)
    {
        document.LoaiGiayTo = request.LoaiGiayTo.Trim();
        document.SoTrenGiayTo = request.SoTrenGiayTo.Trim();
        document.NgayCap = request.NgayCap;
        document.NgayHetHan = request.NgayHetHan;
        document.NoiCap = request.NoiCap.Trim();
    }

    private static void Apply(GiayTo document, GiayToUpdateDto request)
    {
        document.LoaiGiayTo = request.LoaiGiayTo.Trim();
        document.SoTrenGiayTo = request.SoTrenGiayTo.Trim();
        document.NgayCap = request.NgayCap;
        document.NgayHetHan = request.NgayHetHan;
        document.NoiCap = request.NoiCap.Trim();
    }

    private static object ToProfile(KhachHang profile)
    {
        return new
        {
            maKhachHang = FixedLengthHelper.TrimSafe(profile.MaKhachHang),
            ho = profile.Ho,
            ten = profile.Ten,
            hoGiayTo = profile.HoGiayTo,
            tenGiayTo = profile.TenGiayTo,
            quocTich = profile.QuocTich,
            danhXung = FixedLengthHelper.TrimSafe(profile.DanhXung),
            gioiTinh = FixedLengthHelper.TrimSafe(profile.GioiTinh),
            ngaySinh = profile.NgaySinh,
            email = profile.Email,
            soDienThoai = FixedLengthHelper.TrimSafe(profile.SoDienThoai),
            giayTos = profile.GiayTos.Select(ToDocument)
        };
    }

    private static object ToDocument(GiayTo document)
    {
        return new
        {
            maGiayTo = FixedLengthHelper.TrimSafe(document.MaGiayTo),
            loaiGiayTo = document.LoaiGiayTo,
            soTrenGiayTo = document.SoTrenGiayTo,
            ngayCap = document.NgayCap,
            ngayHetHan = document.NgayHetHan,
            noiCap = document.NoiCap,
            anhMatTruoc = document.AnhMatTruoc,
            anhMatSau = document.AnhMatSau
        };
    }

    private async Task<string> GenerateIdAsync(string prefix, IQueryable<string> existingIds)
    {
        string id;
        do
        {
            id = FixedLengthHelper.PadTo20(
                $"{prefix}{Guid.NewGuid():N}"[..20].ToUpperInvariant());
        }
        while (await existingIds.AnyAsync(item => item == id));

        return id;
    }
}
