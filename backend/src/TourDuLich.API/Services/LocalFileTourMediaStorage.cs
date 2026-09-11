using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace TourDuLich.API.Services;

public sealed class LocalFileTourMediaStorage : ITourMediaStorage
{
    private readonly IWebHostEnvironment _env;
    private readonly IHttpContextAccessor _http;

    public LocalFileTourMediaStorage(IWebHostEnvironment env, IHttpContextAccessor http)
    {
        _env = env;
        _http = http;
    }

    public async Task<StoredTourMedia> UploadAsync(
        IFormFile file, string maTour, TourMediaKind kind, CancellationToken cancellationToken)
    {
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext))
            ext = kind == TourMediaKind.Video ? ".mp4" : ".png";
        var publicId = $"tours/{maTour.Trim()}/{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? Path.Combine(_env.ContentRootPath, "wwwroot")
            : _env.WebRootPath;
        var fullPath = Path.Combine(webRoot, "uploads", publicId.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using (var stream = File.Create(fullPath))
            await file.CopyToAsync(stream, cancellationToken);

        var request = _http.HttpContext?.Request;
        var origin = request is null
            ? ""
            : $"{request.Scheme}://{request.Host}{request.PathBase}".TrimEnd('/');
        var url = string.IsNullOrEmpty(origin)
            ? $"/uploads/{publicId}"
            : $"{origin}/uploads/{publicId}";
        return new StoredTourMedia(url, publicId, kind);
    }

    public Task DeleteAsync(string publicId, TourMediaKind kind, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(publicId) || publicId.Contains(".."))
            return Task.CompletedTask;
        var webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? Path.Combine(_env.ContentRootPath, "wwwroot")
            : _env.WebRootPath;
        var fullPath = Path.Combine(webRoot, "uploads", publicId.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }
}

public sealed class TourMediaStorage : ITourMediaStorage
{
    private readonly ITourMediaStorage _inner;

    public TourMediaStorage(
        IConfiguration configuration,
        IWebHostEnvironment env,
        IHttpContextAccessor http)
    {
        var url = Environment.GetEnvironmentVariable("CLOUDINARY_URL")
            ?? configuration["Cloudinary:Url"];
        _inner = string.IsNullOrWhiteSpace(url)
            ? new LocalFileTourMediaStorage(env, http)
            : new CloudinaryTourMediaStorage(configuration);
    }

    public Task<StoredTourMedia> UploadAsync(
        IFormFile file, string maTour, TourMediaKind kind, CancellationToken cancellationToken)
        => _inner.UploadAsync(file, maTour, kind, cancellationToken);

    public Task DeleteAsync(string publicId, TourMediaKind kind, CancellationToken cancellationToken)
        => _inner.DeleteAsync(publicId, kind, cancellationToken);
}
