using Microsoft.AspNetCore.Http;

namespace TourDuLich.API.DTOs;

public sealed class AnhTourUploadDto
{
    public IFormFile File { get; set; } = null!;
    public int? ThuTu { get; set; }
    public bool? IsAvatar { get; set; }
}
