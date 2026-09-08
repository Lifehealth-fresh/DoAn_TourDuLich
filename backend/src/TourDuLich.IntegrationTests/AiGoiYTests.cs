using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TourDuLich.Application.Services;
using Xunit;

namespace TourDuLich.IntegrationTests;

public sealed class AiGoiYTests : ApiTestBase
{
    protected override TestApiFactory CreateFactory() => new(services =>
    {
        services.RemoveAll<IAiRecommendationClient>();
        services.AddScoped<IAiRecommendationClient>(_ => new FakeAiRecommendationClient(false));
    });

    [Fact]
    public async Task Generate_WithoutToken_ReturnsUnauthorized()
    {
        SkipIfNoConnection();
        Client.DefaultRequestHeaders.Authorization = null;
        var response = await Client.PostAsJsonAsync("/api/AiGoiY/sinh-goi-y", new { soLuong = 5, alpha = 0.5 });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Customer_CannotGenerateForAnotherUser()
    {
        SkipIfNoConnection();
        var customer = await RegisterAsync();
        var anotherCustomer = await RegisterAsync();
        UseToken(customer);
        var response = await Client.PostAsJsonAsync("/api/AiGoiY/sinh-goi-y", new
        {
            maUser = anotherCustomer.MaUser,
            soLuong = 5,
            alpha = 0.5
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Generate_WithFakeClient_Returns200AndPersistsRecommendations()
    {
        SkipIfNoConnection();
        var customer = await RegisterAsync();
        UseToken(customer);
        var response = await Client.PostAsJsonAsync("/api/AiGoiY/sinh-goi-y", new { soLuong = 2, alpha = 0.5 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var saved = await Client.GetFromJsonAsync<JsonElement>("/api/AiGoiY/cua-toi");
        Assert.Equal(2, saved.GetProperty("items").GetArrayLength());
    }

    private sealed class FakeAiRecommendationClient : IAiRecommendationClient
    {
        private readonly bool _serviceDown;

        public FakeAiRecommendationClient(bool serviceDown) => _serviceDown = serviceDown;

        public Task<IReadOnlyList<AiRecommendationResult>> GetRecommendationsAsync(
            string maUser, int soLuong, double alpha, CancellationToken cancellationToken = default)
        {
            if (_serviceDown)
                throw new HttpRequestException("fake service down");
            IReadOnlyList<AiRecommendationResult> result =
            [
                new("TOUR001", 0.95, "Test content recommendation"),
                new("TOUR002", 0.85, "Test collaborative recommendation")
            ];
            return Task.FromResult(result);
        }
    }
}

public sealed class AiGoiYServiceDownTests : ApiTestBase
{
    protected override TestApiFactory CreateFactory() => new(services =>
    {
        services.RemoveAll<IAiRecommendationClient>();
        services.AddScoped<IAiRecommendationClient>(_ => new FailingAiRecommendationClient());
    });

    [Fact]
    public async Task Generate_WhenAiServiceFails_Returns503()
    {
        SkipIfNoConnection();
        var customer = await RegisterAsync();
        UseToken(customer);
        var response = await Client.PostAsJsonAsync("/api/AiGoiY/sinh-goi-y", new { soLuong = 2, alpha = 0.5 });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private sealed class FailingAiRecommendationClient : IAiRecommendationClient
    {
        public Task<IReadOnlyList<AiRecommendationResult>> GetRecommendationsAsync(
            string maUser, int soLuong, double alpha, CancellationToken cancellationToken = default)
            => throw new HttpRequestException("fake service down");
    }
}
