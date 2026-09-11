using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourDuLich.API.DTOs;
using TourDuLich.Application.Helpers;
using TourDuLich.API.Services;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnhTourController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITourMediaStorage _storage;
    private readonly ILogger<AnhTourController> _logger;

    public AnhTourController(AppDbContext context, ITourMediaStorage storage, ILogger<AnhTourController> logger)
    {
        _context = context;
        _storage = storage;
        _logger = logger;
    }

    [HttpGet("theo-tour/{maTour}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetByTour(string maTour)
    {
        var maTourDb = FixedLengthHelper.PadTo20(maTour);
        if (!await _context.Tours.AsNoTracking().AnyAsync(item => item.MaTour == maTourDb))
            return NotFound(new { message = "Tour không tồn tại." });

        var result = await _context.AnhTours.AsNoTracking()
            .Where(item => item.MaTour == maTourDb)
            .OrderBy(item => item.ThuTu)
            .Select(item => new
            {
                maAnhTour = FixedLengthHelper.TrimSafe(item.MaAnhTour),
                maTour = FixedLengthHelper.TrimSafe(item.MaTour),
                url = item.Url ?? item.ImageUrl,
                loaiMedia = FixedLengthHelper.TrimSafe(item.LoaiMedia),
                thuTu = item.ThuTu,
                isAvatar = item.IsAvatar
            }).ToListAsync();

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> Create(AnhTourCreateDto request)
    {
        return StatusCode(StatusCodes.Status410Gone, new
        {
            message = "Không nhận URL tự do. Dùng POST /api/AnhTour/theo-tour/{maTour}/upload để upload file đã được kiểm tra."
        });
    }

    [HttpPost("theo-tour/{maTour}/upload")]
    [Authorize(Roles = "Sale,Admin")]
    [RequestSizeLimit(104_857_600)]
    public async Task<ActionResult> Upload(string maTour, [FromForm] AnhTourUploadDto request, CancellationToken cancellationToken)
    {
        if (!await _context.Tours.AnyAsync(item => item.MaTour == FixedLengthHelper.PadTo20(maTour), cancellationToken))
            return BadRequest(new { message = "Tour không tồn tại." });

        var validation = await ValidateUploadAsync(request.File, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new { message = validation.Error });

        var maTourDb = FixedLengthHelper.PadTo20(maTour);
        StoredTourMedia uploaded;
        try
        {
            uploaded = await _storage.UploadAsync(request.File, FixedLengthHelper.TrimSafe(maTourDb)!, validation.Kind, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        var media = new AnhTour
        {
            MaAnhTour = await GenerateIdAsync(),
            MaTour = maTourDb,
            Url = uploaded.SecureUrl,
            ImageUrl = uploaded.SecureUrl,
            LoaiMedia = validation.Kind == TourMediaKind.Image ? "Anh" : "Video",
            CloudPublicId = uploaded.PublicId,
            CloudResourceType = validation.Kind == TourMediaKind.Image ? "image" : "video",
            ThuTu = request.ThuTu,
            IsAvatar = request.IsAvatar
        };

        try
        {
            _context.AnhTours.Add(media);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await TryDeleteUploadedAssetAsync(uploaded, cancellationToken);
            throw;
        }

        return CreatedAtAction(nameof(GetByTour), new { maTour = FixedLengthHelper.TrimSafe(maTourDb) }, ToResponse(media));
    }

    [HttpPut("{maAnhTour}")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<ActionResult> Update(string maAnhTour, AnhTourUpdateDto request)
    {
        return StatusCode(StatusCodes.Status410Gone, new
        {
            message = "Không nhận URL tự do. Dùng PUT /api/AnhTour/{maAnhTour}/upload để thay file đã được kiểm tra."
        });
    }

    [HttpPut("{maAnhTour}/upload")]
    [Authorize(Roles = "Sale,Admin")]
    [RequestSizeLimit(104_857_600)]
    public async Task<ActionResult> ReplaceUpload(string maAnhTour, [FromForm] AnhTourUploadDto request, CancellationToken cancellationToken)
    {
        var validation = await ValidateUploadAsync(request.File, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new { message = validation.Error });

        var key = FixedLengthHelper.PadTo20(maAnhTour);
        var media = await _context.AnhTours.FirstOrDefaultAsync(item => item.MaAnhTour == key, cancellationToken);
        if (media is null)
            return NotFound(new { message = "Không tìm thấy media tour." });

        StoredTourMedia uploaded;
        try
        {
            uploaded = await _storage.UploadAsync(request.File, FixedLengthHelper.TrimSafe(media.MaTour)!, validation.Kind, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        var oldPublicId = media.CloudPublicId;
        var oldKind = media.CloudResourceType == "video" ? TourMediaKind.Video : TourMediaKind.Image;
        media.Url = uploaded.SecureUrl;
        media.ImageUrl = uploaded.SecureUrl;
        media.LoaiMedia = validation.Kind == TourMediaKind.Image ? "Anh" : "Video";
        media.CloudPublicId = uploaded.PublicId;
        media.CloudResourceType = validation.Kind == TourMediaKind.Image ? "image" : "video";
        media.ThuTu = request.ThuTu;
        media.IsAvatar = request.IsAvatar;
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await TryDeleteUploadedAssetAsync(uploaded, cancellationToken);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(oldPublicId))
            await TryDeleteAssetAsync(oldPublicId, oldKind, cancellationToken);
        return Ok(ToResponse(media));
    }

    [HttpDelete("{maAnhTour}")]
    [Authorize(Roles = "Sale,Admin")]
    public async Task<IActionResult> Delete(string maAnhTour)
    {
        var key = FixedLengthHelper.PadTo20(maAnhTour);
        var media = await _context.AnhTours.FirstOrDefaultAsync(item => item.MaAnhTour == key);
        if (media is null)
            return NotFound(new { message = "Không tìm thấy media tour." });

        _context.AnhTours.Remove(media);
        await _context.SaveChangesAsync();
        if (!string.IsNullOrWhiteSpace(media.CloudPublicId))
            await TryDeleteAssetAsync(media.CloudPublicId, media.CloudResourceType == "video" ? TourMediaKind.Video : TourMediaKind.Image, CancellationToken.None);
        return NoContent();
    }

    private async Task<string> GenerateIdAsync()
    {
        string id;
        do
        {
            id = FixedLengthHelper.PadTo20($"AT{Guid.NewGuid():N}"[..20].ToUpperInvariant());
        } while (await _context.AnhTours.AnyAsync(item => item.MaAnhTour == id));
        return id;
    }

    private static async Task<UploadValidation> ValidateUploadAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return new UploadValidation(false, TourMediaKind.Image, "File không được để trống.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var expected = extension switch
        {
            ".jpg" or ".jpeg" => (TourMediaKind.Image, "image/jpeg", 10L * 1024 * 1024),
            ".png" => (TourMediaKind.Image, "image/png", 10L * 1024 * 1024),
            ".webp" => (TourMediaKind.Image, "image/webp", 10L * 1024 * 1024),
            ".mp4" => (TourMediaKind.Video, "video/mp4", 100L * 1024 * 1024),
            ".webm" => (TourMediaKind.Video, "video/webm", 100L * 1024 * 1024),
            _ => ((TourMediaKind?)null, string.Empty, 0L)
        };
        if (expected.Item1 is null)
            return new UploadValidation(false, TourMediaKind.Image, "Chỉ nhận JPEG/JPG, PNG, WebP, MP4 hoặc WebM.");
        var kind = expected.Item1.Value;
        if (file.Length > expected.Item3)
            return new UploadValidation(false, kind, kind == TourMediaKind.Image ? "Ảnh tối đa 10 MB." : "Video tối đa 100 MB.");
        var mime = file.ContentType ?? string.Empty;
        var mimeOk = string.IsNullOrWhiteSpace(mime)
            || mime.Equals(expected.Item2, StringComparison.OrdinalIgnoreCase)
            || mime.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase)
            || mime.StartsWith(kind == TourMediaKind.Image ? "image/" : "video/", StringComparison.OrdinalIgnoreCase);
        if (!mimeOk)
            return new UploadValidation(false, kind, $"MIME type phải là {expected.Item2}.");

        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(), cancellationToken);
        var signatureValid = extension switch
        {
            ".jpg" or ".jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => read >= 8 && header.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".webp" => read >= 12 && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP",
            ".mp4" => read >= 8 && System.Text.Encoding.ASCII.GetString(header, 4, 4) == "ftyp",
            ".webm" => read >= 4 && header.Take(4).SequenceEqual(new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }),
            _ => false
        };
        if (!signatureValid)
            return new UploadValidation(false, kind, "Nội dung file không khớp với định dạng khai báo.");
        return new UploadValidation(true, kind, string.Empty);
    }

    private async Task TryDeleteUploadedAssetAsync(StoredTourMedia uploaded, CancellationToken cancellationToken) =>
        await TryDeleteAssetAsync(uploaded.PublicId, uploaded.Kind, cancellationToken);

    private async Task TryDeleteAssetAsync(string publicId, TourMediaKind kind, CancellationToken cancellationToken)
    {
        try
        {
            await _storage.DeleteAsync(publicId, kind, cancellationToken);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            _logger.LogError(error, "Không thể dọn Cloudinary asset {PublicId}.", publicId);
        }
    }

    private static object ToResponse(AnhTour item) => new
    {
        maAnhTour = FixedLengthHelper.TrimSafe(item.MaAnhTour),
        maTour = FixedLengthHelper.TrimSafe(item.MaTour),
        url = item.Url ?? item.ImageUrl,
        loaiMedia = FixedLengthHelper.TrimSafe(item.LoaiMedia),
        thuTu = item.ThuTu,
        isAvatar = item.IsAvatar
    };

    private sealed record UploadValidation(bool IsValid, TourMediaKind Kind, string Error);
}
