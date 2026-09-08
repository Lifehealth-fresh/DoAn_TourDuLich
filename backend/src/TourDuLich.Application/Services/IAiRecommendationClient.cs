namespace TourDuLich.Application.Services;

public interface IAiRecommendationClient
{
    Task<IReadOnlyList<AiRecommendationResult>> GetRecommendationsAsync(
        string maUser, int soLuong, double alpha, CancellationToken cancellationToken = default);
}

public sealed record AiRecommendationResult(string MaTour, double DiemPhuHop, string LyDo);
