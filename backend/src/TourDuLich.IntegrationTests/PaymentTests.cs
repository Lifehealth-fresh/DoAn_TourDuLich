using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;

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

    private async Task<(string MaTt, int Amount)> CreateVnPayPaymentAsync(
        AuthResult customer,
        string booking,
        int? amount = null)
    {
        UseToken(customer);
        if (amount is null)
        {
            var summary = await Client.GetFromJsonAsync<JsonElement>(
                $"/api/ThanhToan/theo-booking/{booking}/tong-hop");
            amount = summary.GetProperty("tongTien").GetInt32();
        }

        var create = await Client.PostAsJsonAsync("/api/ThanhToan/tao-phien-cong", new
        {
            maBooking = booking,
            soTien = amount.Value,
            phuongThuc = "VNPay",
            loaiThanhToan = "DatCoc"
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var payment = await create.Content.ReadFromJsonAsync<JsonElement>();
        return (payment.GetProperty("maTt").GetString()!, amount.Value);
    }

    private Task<HttpResponseMessage> SendValidVnPayIpnAsync(
        string maTt,
        int amount,
        string transactionNo) => SendVnPayCallbackAsync("ipn", maTt, amount, transactionNo);

    private async Task<HttpResponseMessage> SendVnPayCallbackAsync(
        string endpoint,
        string maTt,
        int amount,
        string transactionNo,
        bool validSignature = true,
        string responseCode = "00",
        string transactionStatus = "00")
    {
        var parameters = new Dictionary<string, string>
        {
            ["vnp_Amount"] = ((long)amount * 100L).ToString(),
            ["vnp_ResponseCode"] = responseCode,
            ["vnp_TmnCode"] = "TESTCODE",
            ["vnp_TransactionNo"] = transactionNo,
            ["vnp_TransactionStatus"] = transactionStatus,
            ["vnp_TxnRef"] = maTt
        };
        var query = BuildVnPayQuery(parameters);
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(
            VnPayTestConfiguration["VnPay:HashSecret"]!));
        var signature = Convert.ToHexString(
            hmac.ComputeHash(Encoding.UTF8.GetBytes(query))).ToLowerInvariant();

        if (!validSignature)
            signature = (signature[0] == '0' ? "1" : "0") + signature[1..];

        // Giữ lại redirect để kiểm tra Location; không truy cập frontend/cổng thật.
        using var callbackClient = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        return await callbackClient.GetAsync(
            $"/api/ThanhToan/vnpay/{endpoint}?{query}&vnp_SecureHash={signature}");
    }

    private static string BuildVnPayQuery(IEnumerable<KeyValuePair<string, string>> parameters) =>
        string.Join("&", parameters
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => $"{EncodeVnPay(item.Key)}={EncodeVnPay(item.Value)}"));

    private static string EncodeVnPay(string value)
    {
        var encoded = WebUtility.UrlEncode(value);
        var result = new StringBuilder(encoded.Length);
        for (var index = 0; index < encoded.Length; index++)
        {
            if (encoded[index] == '%' && index + 2 < encoded.Length)
            {
                result.Append('%');
                result.Append(char.ToUpperInvariant(encoded[index + 1]));
                result.Append(char.ToUpperInvariant(encoded[index + 2]));
                index += 2;
                continue;
            }
            result.Append(encoded[index]);
        }
        return result.ToString();
    }

    private async Task<string> GetPaymentStatusAsync(
        AuthResult customer,
        string booking,
        string maTt)
    {
        UseToken(customer);
        var payments = await Client.GetFromJsonAsync<JsonElement>(
            $"/api/ThanhToan/theo-booking/{booking}");
        return payments.EnumerateArray()
            .Single(item => item.GetProperty("maTt").GetString() == maTt)
            .GetProperty("trangThai").GetString()!;
    }

    private async Task SetBookingTotalAsync(string booking, int total)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var maBookingDb = FixedLengthHelper.PadTo20(booking);
        var entity = await context.DatDichVus
            .SingleAsync(item => item.MaBooking == maBookingDb);
        entity.ThanhTien = total;
        await context.SaveChangesAsync();
    }

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

    [Fact]
    public async Task ValidVnPayIpn_ForCancelledBooking_DoesNotConfirmPayment()
    {
        var data = await CreateBookingAsync();
        var payment = await CreateVnPayPaymentAsync(data.Customer, data.Booking, 10_000);

        UseToken(data.Customer);
        (await Client.PutAsync($"/api/DatDichVu/{data.Booking}/huy", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var ipn = await SendValidVnPayIpnAsync(payment.MaTt, payment.Amount, "VNP-CANCELLED");
        ipn.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ipn.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("rspCode").GetString().Should().NotBe("00");
        (await GetPaymentStatusAsync(data.Customer, data.Booking, payment.MaTt))
            .Should().NotBe("DaXacNhan");
    }

    [Fact]
    public async Task ValidVnPayIpn_ThatWouldOverpay_DoesNotConfirmPayment()
    {
        var data = await CreateBookingAsync();
        var payment = await CreateVnPayPaymentAsync(data.Customer, data.Booking, 10_000);
        await SetBookingTotalAsync(data.Booking, payment.Amount - 1);

        var ipn = await SendValidVnPayIpnAsync(payment.MaTt, payment.Amount, "VNP-OVERPAY");
        ipn.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ipn.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("rspCode").GetString().Should().Be("04");
        (await GetPaymentStatusAsync(data.Customer, data.Booking, payment.MaTt))
            .Should().Be("ChoXacNhan");
    }

    [Fact]
    public async Task ValidVnPayIpn_ForFullAmount_ConfirmsPaymentAndMarksBookingPaid()
    {
        var data = await CreateBookingAsync();
        var payment = await CreateVnPayPaymentAsync(data.Customer, data.Booking);

        var ipn = await SendValidVnPayIpnAsync(payment.MaTt, payment.Amount, "VNP-PAID-FULL");
        ipn.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ipn.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("rspCode").GetString().Should().Be("00");
        (await GetPaymentStatusAsync(data.Customer, data.Booking, payment.MaTt))
            .Should().Be("DaXacNhan");

        UseToken(data.Customer);
        var booking = await Client.GetFromJsonAsync<JsonElement>(
            $"/api/DatDichVu/{data.Booking}");
        booking.GetProperty("trangThai").GetString().Should().Be("DaThanhToan");
    }

    [Theory]
    [InlineData("return", "ipn")]
    [InlineData("ipn", "return")]
    public async Task VnPayReturnAndIpn_ForFullAmount_ConfirmOnlyOnce(
        string firstEndpoint, string secondEndpoint)
    {
        var data = await CreateBookingAsync();
        var payment = await CreateVnPayPaymentAsync(data.Customer, data.Booking);
        const string transactionNo = "VNP-RETURN-PAID";

        var first = await SendVnPayCallbackAsync(
            firstEndpoint, payment.MaTt, payment.Amount, transactionNo);
        await AssertVnPayCallbackAcceptedAsync(first, firstEndpoint, data.Booking, "00");
        (await GetPaymentStatusAsync(data.Customer, data.Booking, payment.MaTt))
            .Should().Be("DaXacNhan");
        var firstPayments = await Client.GetFromJsonAsync<JsonElement>(
            $"/api/ThanhToan/theo-booking/{data.Booking}");
        var paidAt = firstPayments[0].GetProperty("paidAt").GetDateTime();

        var second = await SendVnPayCallbackAsync(
            secondEndpoint, payment.MaTt, payment.Amount, transactionNo);
        await AssertVnPayCallbackAcceptedAsync(second, secondEndpoint, data.Booking, "02");
        UseToken(data.Customer);
        var payments = await Client.GetFromJsonAsync<JsonElement>(
            $"/api/ThanhToan/theo-booking/{data.Booking}");
        payments.GetArrayLength().Should().Be(1);
        payments[0].GetProperty("trangThai").GetString().Should().Be("DaXacNhan");
        payments[0].GetProperty("gatewayTxnId").GetString().Should().Be(transactionNo);
        payments[0].GetProperty("paidAt").GetDateTime().Should().Be(paidAt);
        var booking = await Client.GetFromJsonAsync<JsonElement>(
            $"/api/DatDichVu/{data.Booking}");
        booking.GetProperty("trangThai").GetString().Should().Be("DaThanhToan");
        var summary = await Client.GetFromJsonAsync<JsonElement>(
            $"/api/ThanhToan/theo-booking/{data.Booking}/tong-hop");
        summary.GetProperty("daThanhToan").GetInt32().Should().Be(payment.Amount);
    }

    private static async Task AssertVnPayCallbackAcceptedAsync(
        HttpResponseMessage response, string endpoint, string booking, string ipnCode)
    {
        if (endpoint == "return")
        {
            response.StatusCode.Should().Be(HttpStatusCode.Redirect);
            response.Headers.Location!.AbsoluteUri.Should().Be(
                $"https://customer.test.local/booking/{booking}?maBooking={booking}&status=success&paid=1");
            return;
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("rspCode").GetString().Should().Be(ipnCode);
    }

    [Fact]
    public async Task InvalidVnPayReturnSignature_DoesNotChangePaymentOrBooking()
    {
        var data = await CreateBookingAsync();
        var payment = await CreateVnPayPaymentAsync(data.Customer, data.Booking);

        var response = await SendVnPayCallbackAsync(
            "return", payment.MaTt, payment.Amount, "VNP-BAD-RETURN", validSignature: false);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.AbsoluteUri.Should().Be(
            "https://customer.test.local/?status=invalid&paid=0");
        (await GetPaymentStatusAsync(data.Customer, data.Booking, payment.MaTt))
            .Should().Be("ChoXacNhan");
        var payments = await Client.GetFromJsonAsync<JsonElement>(
            $"/api/ThanhToan/theo-booking/{data.Booking}");
        payments[0].GetProperty("gatewayTxnId").ValueKind.Should().Be(JsonValueKind.Null);
        payments[0].GetProperty("paidAt").ValueKind.Should().Be(JsonValueKind.Null);
        var booking = await Client.GetFromJsonAsync<JsonElement>(
            $"/api/DatDichVu/{data.Booking}");
        booking.GetProperty("trangThai").GetString().Should().Be("ChoXacNhan");
    }

    [Fact]
    public async Task ValidVnPayReturn_ForCancelledBooking_DoesNotConfirmPayment()
    {
        var data = await CreateBookingAsync();
        var payment = await CreateVnPayPaymentAsync(data.Customer, data.Booking, 10_000);
        (await Client.PutAsync($"/api/DatDichVu/{data.Booking}/huy", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await SendVnPayCallbackAsync(
            "return", payment.MaTt, payment.Amount, "VNP-RETURN-CANCELLED");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.Query.Should().Contain("status=invalid&paid=0");
        (await GetPaymentStatusAsync(data.Customer, data.Booking, payment.MaTt))
            .Should().NotBe("DaXacNhan");
        var booking = await Client.GetFromJsonAsync<JsonElement>(
            $"/api/DatDichVu/{data.Booking}");
        booking.GetProperty("trangThai").GetString().Should().Be("DaHuy");
    }

    [Fact]
    public async Task ValidVnPayReturn_ThatWouldOverpay_DoesNotConfirmPayment()
    {
        var data = await CreateBookingAsync();
        var payment = await CreateVnPayPaymentAsync(data.Customer, data.Booking, 10_000);
        await SetBookingTotalAsync(data.Booking, payment.Amount - 1);

        var response = await SendVnPayCallbackAsync(
            "return", payment.MaTt, payment.Amount, "VNP-RETURN-OVERPAY");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.Query.Should().Contain("status=invalid&paid=0");
        (await GetPaymentStatusAsync(data.Customer, data.Booking, payment.MaTt))
            .Should().Be("ChoXacNhan");
    }

    [Theory]
    [InlineData("24", "00")]
    [InlineData("00", "02")]
    public async Task SignedVnPayReturn_WithUnsuccessfulResult_DoesNotConfirmPayment(
        string responseCode, string transactionStatus)
    {
        var data = await CreateBookingAsync();
        var payment = await CreateVnPayPaymentAsync(data.Customer, data.Booking, 10_000);

        var response = await SendVnPayCallbackAsync(
            "return", payment.MaTt, payment.Amount, "VNP-RETURN-FAILED",
            responseCode: responseCode, transactionStatus: transactionStatus);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.Query.Should().Contain("status=failed&paid=0");
        (await GetPaymentStatusAsync(data.Customer, data.Booking, payment.MaTt))
            .Should().Be("ChoXacNhan");
    }
}

public sealed class VnPayReturnGuardTests
{
    [Fact]
    public async Task InvalidSignature_RedirectsBeforeAccessingDatabase()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["VnPay:TmnCode"] = "TESTCODE",
                ["VnPay:HashSecret"] = "fake-vnpay-secret-for-integration-tests-only",
                ["VnPay:BaseUrl"] = "https://gateway.test.local/pay",
                ["VnPay:ReturnUrl"] = "https://api.test.local/api/ThanhToan/vnpay/return",
                ["VnPay:IpnUrl"] = "https://api.test.local/api/ThanhToan/vnpay/ipn",
                ["VnPay:FrontendReturnUrl"] = "https://customer.test.local"
            }).Build();

        // Không cấu hình DB provider: test sẽ lỗi nếu callback đụng tới database.
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().Options);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString(
            "?vnp_TxnRef=TEST&vnp_Amount=1000000&vnp_TmnCode=TESTCODE" +
            "&vnp_ResponseCode=00&vnp_TransactionStatus=00&vnp_TransactionNo=TEST123" +
            "&vnp_SecureHash=bad-signature");
        var controller = new TourDuLich.API.Controllers.ThanhToanController(
            context, null!, configuration, null!)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var response = await controller.VnPayReturn();

        response.Should().BeOfType<RedirectResult>().Which.Url.Should().Be(
            "https://customer.test.local?status=invalid&paid=0");
    }
}

public sealed class VnPayEncodingTests
{
    [Fact]
    public void KnownRawQuery_ProducesExpectedSignature()
    {
        var parameters = new Dictionary<string, string>
        {
            ["vnp_TxnRef"] = "UNKNOWN",
            ["vnp_TmnCode"] = "TESTCODE",
            ["vnp_ReturnUrl"] = "https://customer.test.local/booking/BK 01",
            ["vnp_OrderInfo"] = "Thanh toan tour Da Lat: 30%",
            ["vnp_Amount"] = "1000000"
        };
        const string expectedRawQuery =
            "vnp_Amount=1000000&vnp_OrderInfo=Thanh+toan+tour+Da+Lat%3A+30%25" +
            "&vnp_ReturnUrl=https%3A%2F%2Fcustomer.test.local%2Fbooking%2FBK+01" +
            "&vnp_TmnCode=TESTCODE&vnp_TxnRef=UNKNOWN";
        const string expectedHash =
            "e87fe3733a48f040c04c6de62260e6336247f3d6627ee9ca1575814bab24ce49" +
            "d6488291d8c771aff3255fac1fb8d93f497872301243cc398c2a457c72e93af4";

        var controllerType = typeof(TourDuLich.API.Controllers.ThanhToanController);
        var buildQuery = controllerType.GetMethod(
            "BuildVnPayQuery", BindingFlags.Static | BindingFlags.NonPublic)!;
        var computeHmac = controllerType.GetMethod(
            "ComputeHmacHex", BindingFlags.Static | BindingFlags.NonPublic)!;

        var rawQuery = (string)buildQuery.Invoke(null, [parameters])!;
        var hash = (string)computeHmac.Invoke(null,
            [HashAlgorithmName.SHA512,
             "fake-vnpay-secret-for-integration-tests-only", rawQuery])!;

        rawQuery.Should().Be(expectedRawQuery);
        hash.Should().Be(expectedHash);
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
