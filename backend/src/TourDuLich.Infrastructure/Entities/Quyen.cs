using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class Quyen
{
    public int MaQuyen { get; set; }

    public string TenQuyen { get; set; } = null!;

    public string? Mota { get; set; }

    public int MaVaiTro { get; set; }

    public virtual VaiTro MaVaiTroNavigation { get; set; } = null!;
}
