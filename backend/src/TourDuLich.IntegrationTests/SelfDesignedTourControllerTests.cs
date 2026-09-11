using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using TourDuLich.API.Controllers;
using TourDuLich.API.DTOs;
using TourDuLich.Application.Helpers;
using TourDuLich.Application.Services;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.IntegrationTests;

// Controller unit tests with in-process query/transaction doubles, not SQL Server tests.
// No database connection, external AI, or production/test credentials are used.
public sealed class SelfDesignedTourControllerTests
{
    [Theory]
    [InlineData("success")]
    [InlineData("empty")]
    [InlineData("failure")]
    public async Task Create_SavesBeforeGenerating_AndKeeps201WhenGenerationFails(string outcome)
    {
        using var context = new MemoryContext();
        var service = new ProposalService((request) =>
        {
            Assert.Equal(1, context.Saves);
            Assert.Same(request, Assert.Single(context.Requests));
            if (outcome == "failure") throw new InvalidOperationException("Fake generation failure");
            return outcome == "empty" ? [] : [new LichTrinhDeXuat { MaDeXuat = Key("DX1"), MaYeuCau = request.MaYeuCau }];
        });
        var controller = Controller(context, service);
        var result = Assert.IsType<ObjectResult>(await controller.CreateRequest(new YeuCauThietKeCreateDto
        {
            DiemDenMongMuon = "Đà Nẵng", SoNgay = 2, SoNguoiLon = 1,
            NgayDuKienDi = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))
        }));
        Assert.Equal(201, result.StatusCode);
        Assert.Equal(1, service.Calls);
        Assert.Equal("Moi", Assert.Single(context.Requests).TrangThai?.Trim());
        Assert.Equal(1, context.Saves);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(0)]
    [InlineData(-30)]
    public async Task Approve_ActivatesTourAndCreatesFutureDeparture(int requestedDaysAhead)
    {
        using var context = new MemoryContext();
        var request = ReviewRequest();
        request.NgayDuKienDi = requestedDaysAhead == 0 ? null : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(requestedDaysAhead));
        context.Requests.Add(request);
        var before = DateTime.UtcNow;
        var controller = Controller(context);
        Assert.IsType<OkObjectResult>(await controller.Approve("YC1"));
        Assert.Equal("DaDuyet", request.TrangThai?.Trim());
        Assert.Equal("HoatDong", request.MaTourTaoNavigation!.TrangThai?.Trim());
        Assert.Equal(2, request.MaTourTaoNavigation.ThoiGian);
        var departure = Assert.Single(context.Departures);
        Assert.Equal(Key("TD1"), departure.MaTour);
        Assert.Equal("Đà Nẵng", departure.DiaDiem);
        Assert.Equal(20, departure.MaKhoiHanh.Length);
        Assert.True(departure.NgayKhoiHanh > before);
        Assert.Equal(departure.NgayKhoiHanh!.Value.AddDays(1), departure.NgayKetThuc);
        if (requestedDaysAhead > 0)
            Assert.Equal(request.NgayDuKienDi, DateOnly.FromDateTime(departure.NgayKhoiHanh.Value));
        else
            Assert.InRange(departure.NgayKhoiHanh.Value, before.AddDays(14), DateTime.UtcNow.AddDays(14));
        Assert.Equal(1, context.Saves);
        Assert.IsType<ConflictObjectResult>(await controller.Approve("YC1"));
        Assert.Single(context.Departures);
    }

    [Fact]
    public async Task Approve_PreservesExistingDeparture()
    {
        using var context = new MemoryContext();
        context.Requests.Add(ReviewRequest());
        var departure = new LichKhoiHanh { MaKhoiHanh = Key("KH1"), MaTour = Key("TD1"), NgayKhoiHanh = DateTime.UtcNow.AddDays(40), DiaDiem = "Địa điểm đã thỏa thuận" };
        context.Departures.Add(departure);
        Assert.IsType<OkObjectResult>(await Controller(context).Approve("YC1"));
        Assert.Same(departure, Assert.Single(context.Departures));
        Assert.Equal("Địa điểm đã thỏa thuận", departure.DiaDiem);
    }

    [Fact]
    public async Task Approve_AddsFutureDepartureWhenExistingScheduleHasExpired()
    {
        using var context = new MemoryContext();
        context.Requests.Add(ReviewRequest());
        var expired = new LichKhoiHanh { MaKhoiHanh = Key("KHOLD"), MaTour = Key("TD1"), NgayKhoiHanh = DateTime.UtcNow.AddDays(-1) };
        context.Departures.Add(expired);
        Assert.IsType<OkObjectResult>(await Controller(context).Approve("YC1"));
        Assert.Contains(expired, context.Departures);
        Assert.Single(context.Departures, item => item.NgayKhoiHanh > DateTime.UtcNow);
    }

    [Fact]
    public async Task ApprovedRequest_RejectsInvalidTransitionsWith409_WithoutWriting()
    {
        using var context = new MemoryContext();
        var request = ReviewRequest();
        request.TrangThai = Key("DaDuyet");
        request.MaTourTaoNavigation!.TrangThai = Key("HoatDong");
        context.Requests.Add(request);
        var service = new ProposalService(_ => throw new InvalidOperationException("Must not generate"));
        var controller = Controller(context, service);
        Assert.IsType<ConflictObjectResult>(await controller.CancelRequest("YC1"));
        Assert.IsType<ConflictObjectResult>(await controller.GenerateProposals("YC1", default));
        Assert.IsType<ConflictObjectResult>(await controller.ChooseProposal("YC1", "DX1", default));
        Assert.IsType<ConflictObjectResult>(await controller.EditSchedule("YC1", new SuaLichTrinhDto { ChiTiets = [new() { NgayThu = 1, ThuTuTrongNgay = 1, MaDthamQuan = "DT1", SoLuong = 1 }] }, default));
        Assert.IsType<ConflictObjectResult>(await controller.SubmitForApproval("YC1"));
        Assert.IsType<ConflictObjectResult>(await controller.RejectBySale("YC1", new LyDoTuChoiBoiSaleDto { LyDoTuChoi = "test" }, default));
        Assert.IsType<ConflictObjectResult>(await controller.Approve("YC1"));
        Assert.Equal(0, context.Saves);
        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public async Task Approve_BeforeSubmit_Returns409()
    {
        using var context = new MemoryContext();
        var request = ReviewRequest();
        request.TrangThai = Key("DangThietKe");
        request.MaTourTaoNavigation!.TrangThai = Key("Nhap");
        context.Requests.Add(request);
        Assert.IsType<ConflictObjectResult>(await Controller(context).Approve("YC1"));
        Assert.Empty(context.Departures);
        Assert.Equal(0, context.Saves);
    }

    [Fact]
    public void Proposals_AllowCustomerSaleAndAdmin()
    {
        var attribute = typeof(YeuCauThietKeController).GetMethod(nameof(YeuCauThietKeController.GetProposals))!
            .GetCustomAttribute<AuthorizeAttribute>();
        Assert.Equal(new[] { "Admin", "KhachHang", "Sale" }, attribute!.Roles!.Split(',').OrderBy(value => value));
    }

    private static string Key(string value) => FixedLengthHelper.PadTo20(value);
    private static YeuCauThietKe ReviewRequest() => new()
    {
        MaYeuCau = Key("YC1"), MaUser = Key("USER1"), TrangThai = Key("ChoDuyet"),
        MaTourTao = Key("TD1"), SoNgay = 2, DiemDenMongMuon = "Đà Nẵng",
        MaTourTaoNavigation = new Tour { MaTour = Key("TD1"), TenTour = "Tour thử", LoaiTour = Key("TuThietKe"), TrangThai = Key("ChoXacNhan"), Slkhach = 2 }
    };
    private static YeuCauThietKeController Controller(MemoryContext context, ProposalService? service = null) => new(
        context, new NoBehaviorLogger(), service ?? new ProposalService(_ => []), NullLogger<YeuCauThietKeController>.Instance)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("MaUser", "USER1"), new Claim(ClaimTypes.Role, "Sale")], "test"))
        } }
    };
    private sealed class NoBehaviorLogger : IHanhViLogger
    {
        public Task LogAsync(string user, string? tour, string action) => Task.CompletedTask;
    }
    private sealed class ProposalService(Func<YeuCauThietKe, IReadOnlyList<LichTrinhDeXuat>> generate) : IDeXuatLichTrinhService
    {
        public int Calls { get; private set; }
        public Task<IReadOnlyList<LichTrinhDeXuat>> GenerateAsync(YeuCauThietKe request, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(generate(request));
        }
    }

    private sealed class MemoryContext : AppDbContext
    {
        public List<YeuCauThietKe> Requests { get; } = [];
        public List<LichKhoiHanh> Departures { get; } = [];
        public int Saves { get; private set; }
        public MemoryContext() : base(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=true;Connect Timeout=1").Options)
        {
            YeuCauThietKes = new MemorySet<YeuCauThietKe>(this, Requests);
            LichKhoiHanhs = new MemorySet<LichKhoiHanh>(this, Departures);
        }
        public override DatabaseFacade Database => new MemoryDatabase(this);
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            Saves++;
            return Task.FromResult(1);
        }
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
    private sealed class MemorySet<T>(DbContext context, List<T> items) : DbSet<T>, IQueryable<T> where T : class
    {
        public override Microsoft.EntityFrameworkCore.Metadata.IEntityType EntityType => context.Model.FindEntityType(typeof(T))!;
        private IQueryable<T> Query => new AsyncQuery<T>(items);
        Type IQueryable.ElementType => typeof(T);
        Expression IQueryable.Expression => Query.Expression;
        IQueryProvider IQueryable.Provider => Query.Provider;
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
        public override EntityEntry<T> Add(T entity) { items.Add(entity); return context.Entry(entity); }
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
