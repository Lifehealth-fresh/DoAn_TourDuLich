using System.Collections;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using TourDuLich.API.Controllers;
using TourDuLich.API.DTOs;
using TourDuLich.Application.Helpers;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.IntegrationTests;

// Controller unit tests: query/transaction doubles never connect to a real database.
public sealed class TourScheduleManagementTests
{
    public static IEnumerable<object[]> LockedCases()
    {
        foreach (var role in new[] { "Sale", "Admin" })
        foreach (var action in new[] { "Create", "Update", "Delete" })
        {
            yield return [role, action, "An", false, "Tour đang ẩn (An)"];
            yield return [role, action, "ChoDuyet", false, "Nhap hoặc HoatDong"];
            yield return [role, action, "Nhap", true, "hợp đồng đã ký (DaKy)"];
            yield return [role, action, "HoatDong", true, "hợp đồng đã ký (DaKy)"];
        }
    }

    [Theory]
    [MemberData(nameof(LockedCases))]
    public async Task LockedTour_RejectsAllMutations_WithoutSaving(string role, string action, string state, bool signed, string expected)
    {
        using var context = new MemoryContext(state);
        if (signed) context.Contracts.Add(Contract("TOUR1", "DaKy"));
        var original = context.Rows.Single();
        var result = Assert.IsType<BadRequestObjectResult>(await Invoke(Controller(context, role), action));
        Assert.Contains(expected, Message(result));
        Assert.Equal(0, context.Saves);
        Assert.Same(original, Assert.Single(context.Rows));
        Assert.Equal("Lịch trình gốc", original.Mota);
        Assert.Equal(1000, context.Tour.GiaTour);
    }

    public static IEnumerable<object[]> EditableCases()
    {
        foreach (var role in new[] { "Sale", "Admin" })
        foreach (var action in new[] { "Create", "Update" })
        foreach (var state in new[] { "Nhap", "HoatDong" })
            yield return [role, action, state];
    }

    [Theory]
    [MemberData(nameof(EditableCases))]
    public async Task EditableTour_ReachesDestinationValidation(string role, string action, string state)
    {
        using var context = new MemoryContext(state);
        // Neither unsigned contracts nor a signed contract for a different tour may lock this tour.
        context.Contracts.Add(Contract("TOUR1", "Nhap"));
        context.Contracts.Add(Contract("OTHER", "DaKy"));
        var result = Assert.IsType<BadRequestObjectResult>(await Invoke(Controller(context, role), action));
        Assert.Equal("Điểm tham quan không tồn tại.", Message(result));
        Assert.Equal(0, context.Saves);
    }

    [Theory]
    [InlineData("Sale", "Nhap")]
    [InlineData("Sale", "HoatDong")]
    [InlineData("Admin", "Nhap")]
    [InlineData("Admin", "HoatDong")]
    public async Task EditableTour_DeleteSucceeds_AndRecalculatesPrice(string role, string state)
    {
        using var context = new MemoryContext(state);
        context.Contracts.Add(Contract("TOUR1", "Nhap"));
        context.Contracts.Add(Contract("OTHER", "DaKy"));
        var result = Assert.IsType<OkObjectResult>(await Controller(context, role).Delete("LT1"));
        Assert.Equal("Đã xóa dòng lịch trình.", Message(result));
        Assert.Empty(context.Rows);
        Assert.Equal(0, context.Tour.GiaTour);
        Assert.Equal(2, context.Saves);
        Assert.True(context.Transaction.Committed);
    }

    [Theory]
    [InlineData("Create")]
    [InlineData("Update")]
    [InlineData("Delete")]
    public void MutationEndpoints_AuthorizeOnlySaleAndAdmin(string action)
    {
        var attribute = Assert.Single(typeof(LichTrinhController).GetMethod(action)!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal("Sale,Admin", attribute.Roles);
    }

    [Fact]
    public async Task NonOwnerCustomer_CannotMutate_WithoutWrites()
    {
        using var context = new MemoryContext("HoatDong");
        foreach (var action in new[] { "Create", "Update", "Delete" })
            Assert.IsType<ForbidResult>(await Invoke(Controller(context, "KhachHang"), action));
        Assert.Equal(0, context.Saves);
    }

    [Fact]
    public void SignedContractPredicate_TranslatesToSql_WithoutOpeningConnection()
    {
        using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=true").Options);
        var key = Key("TOUR1");
        var state = Key("DaKy");
        var sql = context.HopDongs.Where(item => item.MaBookingNavigation.MaTour == key && item.TrangThai == state).ToQueryString();
        Assert.Contains("JOIN", sql);
        Assert.Contains("MaTour", sql);
        Assert.Contains("TrangThai", sql);
    }

    private static string Key(string value) => FixedLengthHelper.PadTo20(value);
    private static string Message(ObjectResult result) => JsonSerializer.SerializeToElement(result.Value).GetProperty("message").GetString()!;
    private static HopDong Contract(string tour, string state) => new()
    {
        MaHopDong = Key(Guid.NewGuid().ToString("N")[..20]), TrangThai = Key(state),
        MaBookingNavigation = new DatDichVu { MaTour = Key(tour) }
    };
    private static Task<IActionResult> Invoke(LichTrinhController controller, string action) => action switch
    {
        "Create" => Create(controller),
        "Update" => controller.Update("LT1", new LichTrinhUpdateDto { MaDthamQuan = "MISSING", SoLuong = 1, Mota = "Mới" }),
        _ => controller.Delete("LT1")
    };
    private static async Task<IActionResult> Create(LichTrinhController controller) =>
        await controller.Create(new LichTrinhCreateDto { MaTour = "TOUR1", MaDthamQuan = "MISSING", SoLuong = 1, NgayThu = 1, ThuTuTrongNgay = 1 });
    private static LichTrinhController Controller(MemoryContext context, string role) => new(context)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("MaUser", "STAFF1"), new Claim(ClaimTypes.Role, role)], "test"))
        } }
    };
    private sealed class MemoryContext : AppDbContext
    {
        public Tour Tour { get; }
        public List<LichTrinh> Rows { get; } = [];
        public List<HopDong> Contracts { get; } = [];
        public MemoryTransaction Transaction { get; } = new();
        public int Saves { get; private set; }
        public MemoryContext(string state) : base(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=true;Connect Timeout=1").Options)
        {
            Tour = new Tour { MaTour = Key("TOUR1"), TenTour = "Tour test", LoaiTour = Key("Chuan"), TrangThai = Key(state), GiaTour = 1000 };
            Rows.Add(new LichTrinh { MaLichTrinh = Key("LT1"), MaTour = Tour.MaTour, MaDthamQuan = Key("POINT1"), Mota = "Lịch trình gốc", ThanhTien = 1000 });
            Tours = new MemorySet<Tour>([Tour]);
            LichTrinhs = new MemorySet<LichTrinh>(Rows);
            HopDongs = new MemorySet<HopDong>(Contracts);
            DiemThamQuans = new MemorySet<DiemThamQuan>([]);
            YeuCauThietKes = new MemorySet<YeuCauThietKe>([]);
        }
        public override DatabaseFacade Database => new MemoryDatabase(this, Transaction);
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { Saves++; return Task.FromResult(1); }
    }
    private sealed class MemoryDatabase(DbContext context, MemoryTransaction transaction) : DatabaseFacade(context)
    {
        public override Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IDbContextTransaction>(transaction);
    }
    private sealed class MemoryTransaction : IDbContextTransaction
    {
        public bool Committed { get; private set; }
        public Guid TransactionId { get; } = Guid.NewGuid();
        public void Commit() => Committed = true;
        public void Rollback() { }
        public void Dispose() { }
        public Task CommitAsync(CancellationToken cancellationToken = default) { Committed = true; return Task.CompletedTask; }
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
        public override EntityEntry<T> Remove(T entity) { items.Remove(entity); return null!; }
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
