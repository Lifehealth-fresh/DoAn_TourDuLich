using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class AigoiY
{
    public string MaRecommodation { get; set; } = null!;

    public string? MaUser { get; set; }

    public string? MaTour { get; set; }

    public double? DiemPhuHop { get; set; }

    public string? LyDo { get; set; }

    public DateTime? NgayGoiY { get; set; }

    public virtual Tour? MaTourNavigation { get; set; }

    public virtual NguoiSuDung? MaUserNavigation { get; set; }

    public virtual ICollection<YeuCauThietKe> YeuCauThietKes { get; set; } = new List<YeuCauThietKe>();
}
