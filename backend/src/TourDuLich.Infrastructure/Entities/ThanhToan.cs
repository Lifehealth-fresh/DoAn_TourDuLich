using System;
using System.Collections.Generic;

namespace TourDuLich.Infrastructure.Entities;

public partial class ThanhToan
{
    public string MaTt { get; set; } = null!;

    public string MaBooking { get; set; } = null!;

    public int? SoTien { get; set; }

    public DateTime? NgayTt { get; set; }

    public string? TrangThai { get; set; }

    public string? PhuongThuc { get; set; }

    public string? LoaiThanhToan { get; set; }

    public string? IdempotencyKey { get; set; }

    public string? Gateway { get; set; }

    public string? GatewayTxnId { get; set; }

    public string? GatewayOrderId { get; set; }

    public string? PayUrl { get; set; }

    public DateTime? PaidAt { get; set; }

    public virtual DatDichVu MaBookingNavigation { get; set; } = null!;
}
