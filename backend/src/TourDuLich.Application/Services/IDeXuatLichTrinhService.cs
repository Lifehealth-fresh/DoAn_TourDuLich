using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.Application.Services;

public interface IDeXuatLichTrinhService
{
    Task<IReadOnlyList<LichTrinhDeXuat>> GenerateAsync(
        YeuCauThietKe request, CancellationToken cancellationToken = default);
}
