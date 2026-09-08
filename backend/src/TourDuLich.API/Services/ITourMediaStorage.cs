using Microsoft.AspNetCore.Http;

namespace TourDuLich.API.Services;

public interface ITourMediaStorage
{
    Task<StoredTourMedia> UploadAsync(IFormFile file, string maTour, TourMediaKind kind, CancellationToken cancellationToken);
    Task DeleteAsync(string publicId, TourMediaKind kind, CancellationToken cancellationToken);
}

public enum TourMediaKind { Image, Video }

public sealed record StoredTourMedia(string SecureUrl, string PublicId, TourMediaKind Kind);
