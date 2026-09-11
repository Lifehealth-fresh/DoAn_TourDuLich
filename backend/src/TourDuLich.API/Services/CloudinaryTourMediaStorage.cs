using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;

namespace TourDuLich.API.Services;

public sealed class CloudinaryTourMediaStorage : ITourMediaStorage
{
    private readonly string? _cloudinaryUrl;

    public CloudinaryTourMediaStorage(IConfiguration configuration)
    {
        _cloudinaryUrl = Environment.GetEnvironmentVariable("CLOUDINARY_URL")
            ?? configuration["Cloudinary:Url"];
    }

    public async Task<StoredTourMedia> UploadAsync(
        IFormFile file, string maTour, TourMediaKind kind, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var folder = $"tour-du-lich/tours/{maTour.Trim()}";
        var publicId = Guid.NewGuid().ToString("N");

        if (kind == TourMediaKind.Image)
        {
            var result = await GetClient().UploadAsync(new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                PublicId = publicId,
                UseFilename = false,
                UniqueFilename = false,
                Overwrite = false
            }, cancellationToken);
            EnsureSuccess(result.Error, result.SecureUrl);
            return new StoredTourMedia(result.SecureUrl!.ToString(), result.PublicId, kind);
        }

        var videoResult = await GetClient().UploadAsync(new VideoUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            PublicId = publicId,
            UseFilename = false,
            UniqueFilename = false,
            Overwrite = false
        }, cancellationToken);
        EnsureSuccess(videoResult.Error, videoResult.SecureUrl);
        return new StoredTourMedia(videoResult.SecureUrl!.ToString(), videoResult.PublicId, kind);
    }

    public async Task DeleteAsync(string publicId, TourMediaKind kind, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await GetClient().DestroyAsync(new DeletionParams(publicId)
        {
            ResourceType = kind == TourMediaKind.Video ? ResourceType.Video : ResourceType.Image,
            Invalidate = true
        });
        if (result.Error is not null)
            throw new InvalidOperationException($"Cloudinary không thể xóa media: {result.Error.Message}");
    }

    private static void EnsureSuccess(Error? error, Uri? secureUrl)
    {
        if (error is not null || secureUrl is null)
            throw new InvalidOperationException($"Cloudinary upload thất bại: {error?.Message ?? "không nhận được URL an toàn"}");
    }

    private Cloudinary GetClient()
    {
        if (string.IsNullOrWhiteSpace(_cloudinaryUrl))
            throw new InvalidOperationException("Cloudinary chưa được cấu hình. Đặt CLOUDINARY_URL hoặc Cloudinary:Url trong User Secrets.");
        var client = new Cloudinary(_cloudinaryUrl);
        client.Api.Secure = true;
        return client;
    }
}
