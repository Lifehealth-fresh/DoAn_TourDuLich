using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class KhuVuc
{
    public string MaKhuVuc { get; set; } = null!;

    public string? TenKhuVuc { get; set; }

    public string? QuocGia { get; set; }

    public decimal? ViDo { get; set; }

    public decimal? KinhDo { get; set; }

    public string? MuiGio { get; set; }

    public string? TrangThai { get; set; }

    public virtual ICollection<DiemThamQuan> DiemThamQuans { get; set; } = new List<DiemThamQuan>();

    public virtual ICollection<DoiTac> DoiTacs { get; set; } = new List<DoiTac>();
}
