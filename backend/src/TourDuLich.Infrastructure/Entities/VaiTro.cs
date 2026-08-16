using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class VaiTro
{
    public int MaVaiTro { get; set; }

    public string TenVaiTro { get; set; } = null!;

    public string? Mota { get; set; }

    public virtual ICollection<NguoiSuDung> NguoiSuDungs { get; set; } = new List<NguoiSuDung>();

    public virtual ICollection<Quyen> Quyens { get; set; } = new List<Quyen>();
}
