using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace TourDuLich.IntegrationTests;

public sealed class PaymentTests : ApiTestBase
{
    private static readonly IReadOnlyDictionary<string, string?> VnPayTestConfiguration =
        new Dictionary<string, string?>
        {
            ["VnPay:TmnCode"] = "TESTCODE",
            ["VnPay:HashSecret"] = "fake-vnpay-secret-for-integration-tests-only",
            ["VnPay:BaseUrl"] = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
            ["VnPay:ReturnUrl"] = "https://api.test.local/api/ThanhToan/vnpay/return",
            ["VnPay:IpnUrl"] = "https://api.test.local/api/ThanhToan/vnpay/ipn",
            ["VnPay:FrontendReturnUrl"] = "https://customer.test.local"
        };

    protected override TestApiFactory CreateFactory() =>
        new(configurationValues: VnPayTestConfiguration);

    private async Task<(AuthResult Customer, AuthResult Sale, string Booking)> CreateBookingAsync()
    {
        var sale = await LoginSaleAsync();
        var tour = await CreateTourAsync(sale);
        var departure = await CreateDepartureAsync(sale, tour, DateTime.UtcNow.AddDays(20));
        var customer = await RegisterAsync();
        UseToken(customer);
        var response = await Client.PostAsJsonAsync("/api/DatDichVu", new { maTour = tour, maKhoiHanh = departure, slnguoiLon = 1, sltreEm = 0 });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (customer, sale, body.GetProperty("maBooking").GetString()!);
    }

    [Fact]
    public async Task CancelledBookingCannotBePaid()
    {
        var data = await CreateBookingAsync();
        UseToken(data.Customer);
        (await Client.PutAsync($"/api/DatDichVu/{data.Booking}/huy", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await Client.PostAsJsonAsync("/api/ThanhToan", new { maBooking = data.Booking, soTien = 1, phuongThuc = "Test", loaiThanhToan = "ThanhToanDu" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PaymentCannotExceedRemainingAndSaleCannotReadDetails()
    {
        var data = await CreateBookingAsync();
        UseToken(data.Customer);
        (await Client.PostAsJsonAsync("/api/ThanhToan", new { maBooking = data.Booking, soTien = 999999999, phuongThuc = "Test", loaiThanhToan = "ThanhToanDu" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        UseToken(data.Sale);
        (await Client.GetAsync($"/api/ThanhToan/theo-booking/{data.Booking}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var summary = await Client.GetAsync($"/api/ThanhToan/theo-booking/{data.Booking}/tong-hop");
        summary.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await summary.Content.ReadFromJsonAsync<JsonElement>();
        json.TryGetProperty("tongTien", out _).Should().BeTrue();
        json.TryGetProperty("daThanhToan", out _).Should().BeTrue();
        json.TryGetProperty("conLai", out _).Should().BeTrue();
    }

    [Fact]
    public async Task SameIdempotencyKey_ReturnsOriginalPaymentWithoutCreatingAnotherRow()
    {
        var data = await CreateBookingAsync();
        UseToken(data.Customer);
        const string idempotencyKey = "payment-retry-integration-test-001";

        async Task<HttpResponseMessage> SendAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/ThanhToan")
            {
                Content = JsonContent.Create(new
                {
                    maBooking = data.Booking,
                    soTien = 10_000,
                    phuongThuc = "ChuyenKhoan",
                    loaiThanhToan = "DatCoc"
                })
            };
            request.Headers.Add("Idempotency-Key", idempotencyKey);
            return await Client.SendAsync(request);
        }

        var first = await SendAsync();
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstBody = await first.Content.ReadFromJsonAsync<JsonElement>();

        var retry = await SendAsync();
        retry.StatusCode.Should().Be(HttpStatusCode.OK);
        var retryBody = await retry.Content.ReadFromJsonAsync<JsonElement>();
        retryBody.GetProperty("maTt").GetString()
            .Should().Be(firstBody.GetProperty("maTt").GetString());

        var payments = await Client.GetFromJsonAsync<JsonElement>(
            $"/api/ThanhToan/theo-booking/{data.Booking}");
        payments.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task TienMat_RemainsPendingForManualConfirmation()
    {
        var data = await CreateBookingAsync();
        UseToken(data.Customer);
        var response = await Client.PostAsJsonAsync("/api/ThanhToan", new
        {
            maBooking = data.Booking,
            soTien = 10_000,
            phuongThuc = "TienMat",
            loaiThanhToan = "DatCoc"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("trangThai").GetString().Should().Be("ChoXacNhan");
    }

    [Fact]
    public async Task SaleCannotManuallyConfirmVnPayPayment()
    {
        var data = await CreateBookingAsync();
        UseToken(data.Customer);
        var create = await Client.PostAsJsonAsync("/api/ThanhToan/tao-phien-cong", new
        {
            maBooking = data.Booking,
            soTien = 10_000,
            phuongThuc = "VNPay",
            loaiThanhToan = "DatCoc"
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var payment = await create.Content.ReadFromJsonAsync<JsonElement>();

        UseToken(data.Sale);
        var confirm = await Client.PutAsync(
            $"/api/ThanhToan/{payment.GetProperty("maTt").GetString()}/xac-nhan", null);
        confirm.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await confirm.Content.ReadAsStringAsync()).Should()
            .Contain("Giao dịch cổng do IPN xác nhận");
    }

    [Fact]
    public async Task InvalidVnPayIpnSignature_DoesNotConfirmPayment()
    {
        var data = await CreateBookingAsync();
        UseToken(data.Customer);
        var create = await Client.PostAsJsonAsync("/api/ThanhToan/tao-phien-cong", new
        {
            maBooking = data.Booking,
            soTien = 10_000,
            phuongThuc = "VNPay",
            loaiThanhToan = "DatCoc"
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var maTt = created.GetProperty("maTt").GetString();

        Client.DefaultRequestHeaders.Authorization = null;
        var ipn = await Client.GetAsync(
            $"/api/ThanhToan/vnpay/ipn?vnp_TxnRef={maTt}" +
            "&vnp_Amount=1000000&vnp_ResponseCode=00&vnp_TransactionStatus=00" +
            "&vnp_TransactionNo=TEST123&vnp_SecureHash=bad-signature");
        ipn.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        UseToken(data.Customer);
        var payments = await Client.GetFromJsonAsync<JsonElement>(
            $"/api/ThanhToan/theo-booking/{data.Booking}");
        payments.EnumerateArray().Single(item => item.GetProperty("maTt").GetString() == maTt)
            .GetProperty("trangThai").GetString().Should().Be("ChoXacNhan");
    }
}

public sealed class MissingGatewayConfigurationPaymentTests : ApiTestBase
{
    protected override TestApiFactory CreateFactory() => new(configurationValues:
        new Dictionary<string, string?>
        {
            ["VnPay:TmnCode"] = "",
            ["VnPay:HashSecret"] = "",
            ["VnPay:ReturnUrl"] = "",
            ["VnPay:IpnUrl"] = "",
            ["VnPay:FrontendReturnUrl"] = ""
        });

    [Fact]
    public async Task CreateGatewaySessionWithoutConfiguration_Returns503()
    {
        var sale = await LoginSaleAsync();
        var tour = await CreateTourAsync(sale);
        var departure = await CreateDepartureAsync(sale, tour, DateTime.UtcNow.AddDays(20));
        var customer = await RegisterAsync();
        UseToken(customer);
        var bookingResponse = await Client.PostAsJsonAsync("/api/DatDichVu", new
        {
            maTour = tour,
            maKhoiHanh = departure,
            slnguoiLon = 1,
            sltreEm = 0
        });
        var booking = await bookingResponse.Content.ReadFromJsonAsync<JsonElement>();

        var response = await Client.PostAsJsonAsync("/api/ThanhToan/tao-phien-cong", new
        {
            maBooking = booking.GetProperty("maBooking").GetString(),
            soTien = 10_000,
            phuongThuc = "VNPay",
            loaiThanhToan = "DatCoc"
        });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
