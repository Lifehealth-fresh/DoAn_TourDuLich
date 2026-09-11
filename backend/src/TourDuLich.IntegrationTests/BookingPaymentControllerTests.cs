using System.Collections;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using TourDuLich.API.Controllers;
using TourDuLich.API.DTOs;
using TourDuLich.Application.Helpers;
using TourDuLich.Application.Services;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.IntegrationTests;

// Controller unit tests. Query/transaction doubles never connect to SQL Server.
public sealed class BookingPaymentControllerTests
{
    [Theory]
    [InlineData("ChoXacNhan")]
    [InlineData("TuChoi")]
    [InlineData("DaHuy")]
    [InlineData(null)]
    public async Task UnconfirmedPayments_DoNotAllowBookingConfirmation(string? paymentState)
    {
        using var context = Context("ChoXacNhan", Payment(100000, paymentState));
        var result = Assert.IsType<BadRequestObjectResult>(await Controller(context).UpdateStatus("BK1", Status("DaXacNhan")));
        Assert.Equal("Khách chưa thanh toán (cọc hoặc hết), không thể xác nhận.", Json(result).GetProperty("message").GetString());
        Assert.Equal("ChoXacNhan", context.Bookings[0].TrangThai!.Trim());
        Assert.Equal(0, context.Saves);
    }

    [Theory]
    [InlineData("DaXacNhan")]
    [InlineData("ThanhCong")]
    public async Task ConfirmedDeposit_AllowsConfirmation_ButNotPaidStatus(string paymentState)
    {
        using var context = Context("ChoXacNhan", Payment(30000, paymentState), Payment(70000, "ChoXacNhan"));
        var controller = Controller(context);
        var confirmed = Assert.IsType<OkObjectResult>(await controller.UpdateStatus("BK1", Status("DaXacNhan")));
        Assert.Equal(30000, Json(confirmed).GetProperty("tongDaThanhToan").GetInt64());
        Assert.Equal(70000, Json(confirmed).GetProperty("conLai").GetInt64());
        var refused = Assert.IsType<BadRequestObjectResult>(await controller.UpdateStatus("BK1", Status("DaThanhToan")));
        Assert.Equal("Khách chưa thanh toán đủ. Đã trả 30000, còn 70000.", Json(refused).GetProperty("message").GetString());
        Assert.Equal("DaXacNhan", context.Bookings[0].TrangThai!.Trim());
        Assert.Equal(1, context.Saves);
    }

    [Theory]
    [InlineData(70000, 0)]
    [InlineData(80000, -10000)]
    public async Task FullOrExcessConfirmedPayment_AllowsPaidStatus(int secondAmount, int remaining)
    {
        using var context = Context("DaXacNhan", Payment(30000, "DaXacNhan"), Payment(secondAmount, "ThanhCong"));
        var result = Assert.IsType<OkObjectResult>(await Controller(context).UpdateStatus("BK1", Status("DaThanhToan")));
        Assert.Equal("DaThanhToan", Json(result).GetProperty("trangThai").GetString());
        Assert.Equal(remaining, Json(result).GetProperty("conLai").GetInt64());
        Assert.Equal(1, context.Saves);
    }

    [Theory]
    [InlineData("ChoXacNhan", "DaHuy")]
    [InlineData("DaXacNhan", "DaHuy")]
    [InlineData("DaThanhToan", "HoanThanh")]
    public async Task CancelAndComplete_KeepTheirExistingRules(string from, string to)
    {
        using var context = Context(from);
        var logger = new BehaviorLogger();
        var result = Assert.IsType<OkObjectResult>(await Controller(context, logger).UpdateStatus("BK1", Status(to)));
        Assert.Equal(to, Json(result).GetProperty("trangThai").GetString());
        Assert.Equal(to == "HoanThanh" ? 1 : 0, logger.Calls);
    }

    [Fact]
    public async Task OtherBookingsAndNullAmounts_DoNotCount()
    {
        using var context = Context("ChoXacNhan", Payment(null, "DaXacNhan"));
        context.Payments.Add(new ThanhToan { MaBooking = Key("OTHER"), SoTien = 100000, TrangThai = Key("ThanhCong") });
        Assert.IsType<BadRequestObjectResult>(await Controller(context).UpdateStatus("BK1", Status("DaXacNhan")));
        var detail = Json(Assert.IsType<OkObjectResult>(await Controller(context).GetMyBooking("BK1")));
        Assert.Equal(0, detail.GetProperty("tongDaThanhToan").GetInt64());
        Assert.Equal(100000, detail.GetProperty("conLai").GetInt64());
    }

    [Fact]
    public async Task ListAndDetail_ExposeSameConfirmedBalance_AndPreserveExistingFields()
    {
        using var context = Context("ChoXacNhan", Payment(20000, "DaXacNhan"), Payment(10000, "ThanhCong"),
            Payment(70000, "ChoXacNhan"), Payment(100000, "TuChoi"), Payment(100000, "DaHuy"));
        var controller = Controller(context);
        var list = Json(Assert.IsType<OkObjectResult>(await controller.GetAllForStaff()));
        var detail = Json(Assert.IsType<OkObjectResult>(await controller.GetMyBooking("BK1")));
        foreach (var item in new[] { list.GetProperty("items")[0], detail })
        {
            Assert.Equal("BK1", item.GetProperty("maBooking").GetString());
            Assert.Equal("Tour kiểm thử", item.GetProperty("tenTour").GetString());
            Assert.Equal(100000, item.GetProperty("thanhTien").GetInt32());
            Assert.Equal(30000, item.GetProperty("tongDaThanhToan").GetInt64());
            Assert.Equal(70000, item.GetProperty("conLai").GetInt64());
            Assert.Equal("ChoXacNhan", item.GetProperty("trangThai").GetString());
        }
        Assert.True(detail.TryGetProperty("soTienPhatHuy", out _));
        Assert.True(list.TryGetProperty("totalCount", out _));
    }

    [Fact]
    public async Task ZeroConfirmedPayments_ReturnZero_AndNullTotalIsTreatedAsZero()
    {
        using var context = Context("DaXacNhan");
        context.Bookings[0].ThanhTien = null;
        var detail = Json(Assert.IsType<OkObjectResult>(await Controller(context).GetMyBooking("BK1")));
        Assert.Equal(0, detail.GetProperty("tongDaThanhToan").GetInt64());
        Assert.Equal(0, detail.GetProperty("conLai").GetInt64());
        Assert.IsType<OkObjectResult>(await Controller(context).UpdateStatus("BK1", Status("DaThanhToan")));
    }

    [Fact]
    public async Task SumUses64BitAmounts_AndInvalidTransitionStaysRejected()
    {
        using var context = Context("ChoXacNhan", Payment(int.MaxValue, "DaXacNhan"), Payment(1, "ThanhCong"));
        var result = Json(Assert.IsType<OkObjectResult>(await Controller(context).GetMyBooking("BK1")));
        Assert.Equal((long)int.MaxValue + 1, result.GetProperty("tongDaThanhToan").GetInt64());
        Assert.IsType<BadRequestObjectResult>(await Controller(context).UpdateStatus("BK1", Status("HoanThanh")));
        Assert.Equal(0, context.Saves);
    }

    private static string Key(string value) => FixedLengthHelper.PadTo20(value);
    private static JsonElement Json(ObjectResult result) => JsonSerializer.SerializeToElement(result.Value);
    private static DatDichVuTrangThaiDto Status(string value) => new() { TrangThai = value };
    private static ThanhToan Payment(int? amount, string? state) => new() { MaTt = Key(Guid.NewGuid().ToString("N")[..20]), MaBooking = Key("BK1"), SoTien = amount, TrangThai = state is null ? null : Key(state), PhuongThuc = Key("TienMat") };
    private static MemoryContext Context(string state, params ThanhToan[] payments)
    {
        var context = new MemoryContext();
        context.Payments.AddRange(payments);
        context.Bookings.Add(new DatDichVu
        {
            MaBooking = Key("BK1"), MaUser = Key("USER1"), MaTour = Key("TOUR1"), ThanhTien = 100000,
            TrangThai = Key(state), ThanhToans = payments.ToList(),
            MaTourNavigation = new Tour { TenTour = "Tour kiểm thử" }, MaUserNavigation = new NguoiSuDung { SoDienThoai = "0900000000" }
        });
        return context;
    }
    private static DatDichVuController Controller(MemoryContext context, BehaviorLogger? logger = null) => new(context, logger ?? new BehaviorLogger())
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("MaUser", "USER1"), new Claim(ClaimTypes.Role, "Sale")], "test"))
        } }
    };
    private sealed class BehaviorLogger : IHanhViLogger
    {
        public int Calls { get; private set; }
        public Task LogAsync(string user, string? tour, string action) { Calls++; return Task.CompletedTask; }
    }
    private sealed class MemoryContext : AppDbContext
    {
        public List<DatDichVu> Bookings { get; } = [];
        public List<ThanhToan> Payments { get; } = [];
        public int Saves { get; private set; }
        public MemoryContext() : base(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=true;Connect Timeout=1").Options)
        {
            DatDichVus = new MemorySet<DatDichVu>(Bookings);
            ThanhToans = new MemorySet<ThanhToan>(Payments);
        }
        public override DatabaseFacade Database => new MemoryDatabase(this);
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { Saves++; return Task.FromResult(1); }
    }
    private sealed class MemoryDatabase(DbContext context) : DatabaseFacade(context)
    {
        public override Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IDbContextTransaction>(new MemoryTransaction());
    }
    private sealed class MemoryTransaction : IDbContextTransaction
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public void Commit() { }
        public void Rollback() { }
        public void Dispose() { }
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
    private sealed class MemorySet<T>(List<T> items) : DbSet<T>, IQueryable<T> where T : class
    {
        public override Microsoft.EntityFrameworkCore.Metadata.IEntityType EntityType => throw new NotSupportedException();
        private IQueryable<T> Query => new AsyncQuery<T>(items);
        Type IQueryable.ElementType => typeof(T);
        Expression IQueryable.Expression => Query.Expression;
        IQueryProvider IQueryable.Provider => Query.Provider;
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
    }
    private sealed class AsyncQuery<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public AsyncQuery(IEnumerable<T> items) : base(items) { }
        public AsyncQuery(Expression expression) : base(expression) { }
        IQueryProvider IQueryable.Provider => new AsyncProvider(this);
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new AsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }
    private sealed class AsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        public T Current => inner.Current;
        public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());
        public ValueTask DisposeAsync() { inner.Dispose(); return ValueTask.CompletedTask; }
    }
    private sealed class AsyncProvider(IQueryProvider inner) : IAsyncQueryProvider
    {
        public IQueryable CreateQuery(Expression expression) => throw new NotSupportedException();
        public IQueryable<T> CreateQuery<T>(Expression expression) => new AsyncQuery<T>(expression);
        public object? Execute(Expression expression) => inner.Execute(expression);
        public T Execute<T>(Expression expression) => inner.Execute<T>(expression);
        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
            => (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(typeof(TResult).GetGenericArguments()[0]).Invoke(null, [inner.Execute(expression)])!;
    }
}
