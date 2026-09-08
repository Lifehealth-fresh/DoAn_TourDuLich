using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class AnhTour
{
    public string MaAnhTour { get; set; } = null!;

    public string MaTour { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public string? Url { get; set; }

    public string LoaiMedia { get; set; } = "Anh";

    public string? CloudPublicId { get; set; }

    public string? CloudResourceType { get; set; }

    public int? ThuTu { get; set; }

    public bool? IsAvatar { get; set; }

    public virtual Tour MaTourNavigation { get; set; } = null!;
}
