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
}

public interface IDeXuatLichTrinhService
{
    Task<IReadOnlyList<LichTrinhDeXuat>> GenerateAsync(
        YeuCauThietKe request, DesignPlannerContext? extras = null, CancellationToken cancellationToken = default);
}
