using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class DanhGiaTour
{
    public string MaDanhGiaTour { get; set; } = null!;

    public string? MaUser { get; set; }

    public string? MaTour { get; set; }

    public DateTime? ThoiGian { get; set; }

    public int? SaoDanhGia { get; set; }

    public string? NhanXet { get; set; }

    public virtual Tour? MaTourNavigation { get; set; }

    public virtual NguoiSuDung? MaUserNavigation { get; set; }

    public virtual ICollection<MediaDanhGiaTour> MediaDanhGiaTours { get; set; } = new List<MediaDanhGiaTour>();
}
