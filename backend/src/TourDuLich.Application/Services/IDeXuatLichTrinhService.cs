using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.Application.Services;

public sealed class DesignPlannerContext
{
    public string? MaTinhXuatPhat { get; init; }
    public string? MaTinhDen { get; init; }
    public TimeSpan? GioKhoiHanh { get; init; }
    public DateOnly? NgayKetThuc { get; init; }
    public TimeSpan? GioKetThuc { get; init; }
    public int? SoSuKienMoiNgay { get; init; }

    public static DesignPlannerContext From(YeuCauThietKe request) => new()
    {
        MaTinhXuatPhat = request.MaTinhXuatPhat,
        MaTinhDen = request.MaTinhDen,
        GioKhoiHanh = request.GioKhoiHanh,
        NgayKetThuc = request.NgayKetThuc,
        GioKetThuc = request.GioKetThuc,
        SoSuKienMoiNgay = request.SoSuKienMoiNgay
    };
}

public interface IDeXuatLichTrinhService
{
    Task<IReadOnlyList<LichTrinhDeXuat>> GenerateAsync(
        YeuCauThietKe request, DesignPlannerContext? extras = null, CancellationToken cancellationToken = default);
}
