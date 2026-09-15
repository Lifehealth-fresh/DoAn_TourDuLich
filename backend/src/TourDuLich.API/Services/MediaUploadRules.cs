using Microsoft.AspNetCore.Http;

namespace TourDuLich.API.Services;

public static class MediaUploadRules
{
    public static async Task<UploadCheck> ValidateAsync(
        IFormFile? file, bool allowVideo, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return new UploadCheck(false, TourMediaKind.Image, "File không được để trống.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var expected = extension switch
        {
            ".jpg" or ".jpeg" => (TourMediaKind.Image, "image/jpeg", 10L * 1024 * 1024),
            ".png" => (TourMediaKind.Image, "image/png", 10L * 1024 * 1024),
            ".webp" => (TourMediaKind.Image, "image/webp", 10L * 1024 * 1024),
            ".mp4" when allowVideo => (TourMediaKind.Video, "video/mp4", 100L * 1024 * 1024),
            ".webm" when allowVideo => (TourMediaKind.Video, "video/webm", 100L * 1024 * 1024),
            _ => ((TourMediaKind?)null, string.Empty, 0L)
        };
        if (expected.Item1 is null)
            return new UploadCheck(false, TourMediaKind.Image,
                allowVideo ? "Chỉ nhận JPEG/JPG, PNG, WebP, MP4 hoặc WebM." : "Chỉ nhận JPEG/JPG, PNG hoặc WebP.");
        var kind = expected.Item1.Value;
        if (file.Length > expected.Item3)
            return new UploadCheck(false, kind, kind == TourMediaKind.Image ? "Ảnh tối đa 10 MB." : "Video tối đa 100 MB.");
        var mime = file.ContentType ?? string.Empty;
        var mimeOk = string.IsNullOrWhiteSpace(mime)
            || mime.Equals(expected.Item2, StringComparison.OrdinalIgnoreCase)
            || mime.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase)
            || mime.StartsWith(kind == TourMediaKind.Image ? "image/" : "video/", StringComparison.OrdinalIgnoreCase);
        if (!mimeOk)
            return new UploadCheck(false, kind, $"MIME type phải là {expected.Item2}.");

        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(), cancellationToken);
        var signatureValid = extension switch
        {
            ".jpg" or ".jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => read >= 8 && header.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".webp" => read >= 12 && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF"
                && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP",
            ".mp4" => read >= 8 && System.Text.Encoding.ASCII.GetString(header, 4, 4) == "ftyp",
            ".webm" => read >= 4 && header.Take(4).SequenceEqual(new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }),
            _ => false
        };
        if (!signatureValid)
            return new UploadCheck(false, kind, "Nội dung file không khớp với định dạng khai báo.");
        return new UploadCheck(true, kind, string.Empty);
    }
}

public sealed record UploadCheck(bool IsValid, TourMediaKind Kind, string Error);
