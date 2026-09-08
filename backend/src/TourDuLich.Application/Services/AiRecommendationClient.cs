using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace TourDuLich.Application.Services;

public sealed class AiRecommendationClient : IAiRecommendationClient
{
    private readonly HttpClient _httpClient;

    public AiRecommendationClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(configuration["AiService:BaseUrl"]
            ?? throw new InvalidOperationException("AiService:BaseUrl chưa được cấu hình."));
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
        _httpClient.DefaultRequestHeaders.Add("X-Internal-Api-Key", configuration["AiService:ApiKey"] ?? string.Empty);
    }

    public async Task<IReadOnlyList<AiRecommendationResult>> GetRecommendationsAsync(
        string maUser, int soLuong, double alpha, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("goi-y", new
        {
            maUser,
            soLuong,
            alpha
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<AiRecommendationResponse>>(cancellationToken: cancellationToken);
        return result?.Select(item => new AiRecommendationResult(
            item.MaTour, item.DiemPhuHop, item.LyDo)).ToList()
            ?? [];
    }

    private sealed class AiRecommendationResponse
    {
        public string MaTour { get; set; } = string.Empty;
        public double DiemPhuHop { get; set; }
        public string LyDo { get; set; } = string.Empty;
    }
}
