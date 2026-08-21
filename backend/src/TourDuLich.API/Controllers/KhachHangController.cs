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
[Authorize]
public class KhachHangController : ControllerBase
{
    private readonly AppDbContext _context;

    public KhachHangController(AppDbContext context)
    {
        _context = context;
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
    [Authorize(Roles = "Sale")]
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
    [Authorize(Roles = "Sale")]
    public async Task<ActionResult> SaleCreate(string maUser, KhachHangCreateDto request)
    {
        var maUserDb = FixedLengthHelper.PadTo20(maUser);
        if (!await _context.NguoiSuDungs.AnyAsync(item => item.MaUser == maUserDb))
            return NotFound(new { message = "Không tìm thấy tài khoản khách hàng." });

        return await CreateForUser(maUserDb, request);
    }

    [HttpPut("sale/{maKhachHang}")]
    [Authorize(Roles = "Sale")]
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
    [Authorize(Roles = "Sale")]
    public async Task<ActionResult> SaleAddDocument(string maKhachHang, GiayToCreateDto request)
    {
        var profile = await _context.KhachHangs.FirstOrDefaultAsync(item =>
            item.MaKhachHang == FixedLengthHelper.PadTo20(maKhachHang));

        if (profile is null) return NotFound(new { message = "Không tìm thấy hồ sơ khách hàng." });
        return await AddDocumentToProfile(profile, request);
    }

    [HttpPut("sale/{maKhachHang}/giay-to/{maGiayTo}")]
    [Authorize(Roles = "Sale")]
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
    [Authorize(Roles = "Sale")]
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
            noiCap = document.NoiCap
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
