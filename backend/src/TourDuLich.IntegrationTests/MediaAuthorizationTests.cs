using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace TourDuLich.IntegrationTests;

public sealed class MediaAuthorizationTests : ApiTestBase
{
    [Fact]
    public async Task AnhTour_Get_IsPublic()
    {
        SkipIfNoConnection();
        Client.DefaultRequestHeaders.Authorization = null;
        var response = await Client.GetAsync("/api/AnhTour/theo-tour/TOUR001");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task KhachHang_CannotWriteAnhTour()
    {
        SkipIfNoConnection();
        var customer = await RegisterAsync();
        UseToken(customer);
        var response = await Client.PostAsJsonAsync("/api/AnhTour", new
        {
            maTour = "TOUR001",
            url = "https://example.com/photo.jpg",
            loaiMedia = "Anh"
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        response = await Client.PutAsJsonAsync("/api/AnhTour/UNKNOWNMEDIA", new
        {
            url = "https://example.com/photo.jpg",
            loaiMedia = "Anh"
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        response = await Client.DeleteAsync("/api/AnhTour/UNKNOWNMEDIA");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SaleAdmin_CannotUpdateCustomerReviewOrMedia()
    {
        SkipIfNoConnection();
        var sale = await LoginSaleAsync();
        UseToken(sale);
        var response = await Client.PutAsJsonAsync("/api/DanhGia/tour/UNKNOWNREVIEW", new
        {
            saoDanhGia = 5,
            nhanXet = "not allowed",
            mediaUrls = Array.Empty<object>()
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
