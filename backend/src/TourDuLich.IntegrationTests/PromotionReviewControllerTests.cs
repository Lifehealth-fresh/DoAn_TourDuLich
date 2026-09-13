using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using TourDuLich.API.Controllers;
using TourDuLich.API.DTOs;
using TourDuLich.Application.Helpers;
using TourDuLich.Application.Services;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;
#pragma warning disable EF1001 // Unit-test query doubles; never used in production.

namespace TourDuLich.IntegrationTests;

// Isolated controller tests. SQL commands are recorded, not executed; no DB or payment gateway is contacted.
public sealed class PromotionReviewControllerTests
{
    [Theory]
    [InlineData("ChoXacNhan")]
    [InlineData("DaXacNhan")]
    [InlineData("DaThanhToan")]
    [InlineData("DaHuy")]
    public async Task Review_RequiresCompletedBooking(string state)
    {
        using var db = new MemoryContext();
        db.Booking.TrangThai = Key(state);
        var result = Assert.IsType<BadRequestObjectResult>(await Reviews(db).CreateTourReview(Review()));
        Assert.Contains("hoàn thành", Message(result));
        Assert.Empty(db.ReviewRows); Assert.Equal(0, db.Saves);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Review_RejectsInvalidStars(int stars)
    {
        using var db = new MemoryContext();
        db.Booking.TrangThai = Key("HoanThanh");
        var request = Review(); request.SaoDanhGia = stars;
        Assert.IsType<BadRequestObjectResult>(await Reviews(db).CreateTourReview(request));
        Assert.Empty(db.ReviewRows);
    }

    [Fact]
    public async Task Review_CreateTimestampDuplicateAndEditOwnership()
    {
        using var db = new MemoryContext();
        db.Booking.TrangThai = Key("HoanThanh");
        var controller = Reviews(db);
        var before = DateTime.UtcNow;
        var result = Assert.IsType<ObjectResult>(await controller.CreateTourReview(Review()));
        Assert.Equal(201, result.StatusCode);
        var row = Assert.Single(db.ReviewRows);
        Assert.InRange(row.ThoiGian!.Value, before, DateTime.UtcNow);
        Assert.Equal("Tuyệt vời", row.NhanXet);
        Assert.IsType<ConflictObjectResult>(await controller.CreateTourReview(Review()));
        Assert.IsType<NotFoundObjectResult>(await Reviews(db, "OTHER").UpdateTourReview(row.MaDanhGiaTour.Trim(),
            new DanhGiaTourUpdateDto { SaoDanhGia = 1 }));
        Assert.IsType<NoContentResult>(await controller.UpdateTourReview(row.MaDanhGiaTour.Trim(),
            new DanhGiaTourUpdateDto { SaoDanhGia = 4, NhanXet = "Đã sửa" }));
        Assert.Equal(4, row.SaoDanhGia); Assert.Equal("Đã sửa", row.NhanXet);
    }

    [Fact]
    public async Task Review_OwnReviewIsReturnedOutsideFirstPage_WithoutOtherUserIds()
    {
        using var db = new MemoryContext();
        db.ReviewRows.Add(new DanhGiaTour { MaDanhGiaTour = Key("OLD"), MaTour = Key("TOUR1"), MaUser = Key("USER1"), SaoDanhGia = 3, ThoiGian = DateTime.UtcNow.AddDays(-5) });
        db.ReviewRows.Add(new DanhGiaTour { MaDanhGiaTour = Key("NEW"), MaTour = Key("TOUR1"), MaUser = Key("OTHER"), SaoDanhGia = 5, ThoiGian = DateTime.UtcNow });
        var result = Json(Assert.IsType<OkObjectResult>(await Reviews(db).GetTourReviews("TOUR1", 1, 1)));
        Assert.Equal("OLD", result.GetProperty("danhGiaCuaToi").GetProperty("maDanhGiaTour").GetString());
        Assert.Equal("NEW", result.GetProperty("danhGias")[0].GetProperty("maDanhGiaTour").GetString());
        Assert.False(result.GetProperty("danhGias")[0].TryGetProperty("maUser", out _));
    }

    [Theory]
    [InlineData("WRONG")]
    [InlineData("TOOLONGCODE")]
    [InlineData(" ")]
    public async Task Promotion_InvalidCode_Returns400(string code)
    {
        using var db = new MemoryContext();
        Assert.IsType<BadRequestObjectResult>(await Promotions(db).ApplyPromotion(Apply(code)));
        Assert.Equal(2000000, db.Booking.ThanhTien); Assert.Empty(db.Discounts); Assert.Equal(0, db.Saves);
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("inactive")]
    public async Task Promotion_RejectsInactiveOrOutOfDate(string condition)
    {
        using var db = new MemoryContext();
        if (condition == "expired") db.Promotion.NgayKt = DateTime.UtcNow.AddMinutes(-1);
        if (condition == "future") db.Promotion.NgayBd = DateTime.UtcNow.AddMinutes(1);
        if (condition == "inactive") db.Promotion.TrangThai = Key("NgungHoatDong");
        Assert.IsType<BadRequestObjectResult>(await Promotions(db).ApplyPromotion(Apply()));
        Assert.Empty(db.Discounts);
    }

    [Theory]
    [InlineData("DaHuy")]
    [InlineData("ChoHoanTien")]
    [InlineData("HoanThanh")]
    public async Task Promotion_RejectsFinalOrCancelledBooking(string state)
    {
        using var db = new MemoryContext();
        db.Booking.TrangThai = Key(state);
        Assert.IsType<BadRequestObjectResult>(await Promotions(db).ApplyPromotion(Apply()));
        Assert.Empty(db.Discounts);
    }

    [Fact]
    public async Task Promotion_RejectsOtherUsersBooking()
    {
        using var db = new MemoryContext();
        Assert.IsType<NotFoundObjectResult>(await Promotions(db, user: "OTHER").ApplyPromotion(Apply()));
        Assert.Empty(db.Discounts);
    }

    [Theory]
    [InlineData("ChoXacNhan")]
    [InlineData("DaXacNhan")]
    public async Task Promotion_ValidTrimmedCode_ReducesNetTotal_AndDuplicateIs409(string state)
    {
        using var db = new MemoryContext();
        db.Booking.TrangThai = Key(state);
        var controller = Promotions(db);
        var result = Json(Assert.IsType<OkObjectResult>(await controller.ApplyPromotion(Apply("  DEMO10  "))));
        Assert.Equal(200000, result.GetProperty("soTienGiam").GetInt32());
        Assert.Equal(1800000, db.Booking.ThanhTien); Assert.Equal(200000, db.Booking.TongGiamGia);
        Assert.Equal(540000, Math.Round(db.Booking.ThanhTien!.Value * 0.3));
        Assert.Equal(200000, Assert.Single(db.Discounts).SoTienGiam);
        Assert.Contains("COLUMNPROPERTY", Assert.Single(db.Connection.Commands));
        Assert.Contains("UPDLOCK, HOLDLOCK", db.Connection.Commands[0]);
        Assert.IsType<ConflictObjectResult>(await controller.ApplyPromotion(Apply()));
        Assert.Single(db.Discounts);
    }

    [Theory]
    [InlineData(1000009, 10, 100000)]
    [InlineData(2000000000, 99, 1980000000)]
    public async Task Promotion_PercentageFloors_AndDoesNotOverflow(int total, int percent, int expected)
    {
        using var db = new MemoryContext();
        db.Booking.TongTien = total; db.Booking.ThanhTien = total; db.Promotion.GiamGia = percent;
        Assert.IsType<OkObjectResult>(await Promotions(db).ApplyPromotion(Apply()));
        Assert.Equal(expected, db.Booking.TongGiamGia); Assert.Equal(total - expected, db.Booking.ThanhTien);
    }

    [Fact]
    public async Task Promotion_FixedAndStackedDiscountsAreCapped_AndNonStackingIsSymmetric()
    {
        using var db = new MemoryContext();
        var previous = new KhuyenMai { MaKm = Key("PREV"), CoCongDon = false };
        db.Discounts.Add(new DatDichVuKhuyenMai { MaBooking = Key("BK1"), MaKhuyenMai = previous.MaKm,
            MaKhuyenMaiNavigation = previous, SoTienGiam = 1900000 });
        db.Booking.TongGiamGia = 1900000; db.Booking.ThanhTien = 100000;
        db.Promotion.CoCongDon = true; db.Promotion.DonVi = Key("VND"); db.Promotion.GiamGia = 500000;
        Assert.IsType<BadRequestObjectResult>(await Promotions(db).ApplyPromotion(Apply()));
        previous.CoCongDon = true;
        Assert.IsType<OkObjectResult>(await Promotions(db).ApplyPromotion(Apply()));
        Assert.Equal(2000000, db.Booking.TongGiamGia); Assert.Equal(0, db.Booking.ThanhTien);
        Assert.Equal(100000, db.Discounts.Last().SoTienGiam);
    }

    [Fact]
    public async Task Promotion_EnforcesEveryConditionRow_AndTourScope()
    {
        using var db = new MemoryContext();
        var extra = new DieuKienKm { MaDk = Key("RULE2"), MaKhuyenMai = db.Promotion.MaKm, DonToiThieu = 3000000 };
        db.Conditions.Add(extra);
        Assert.Contains("tối thiểu", Message(Assert.IsType<BadRequestObjectResult>(await Promotions(db).ApplyPromotion(Apply()))));
        db.Conditions.Remove(extra);
        db.TourRules.Add(new KmTour { MaKhuyenMai = db.Promotion.MaKm, MaTour = Key("OTHER") });
        Assert.Contains("tour của vé", Message(Assert.IsType<BadRequestObjectResult>(await Promotions(db).ApplyPromotion(Apply()))));
        db.TourRules[0].MaTour = Key("TOUR1");
        Assert.IsType<OkObjectResult>(await Promotions(db).ApplyPromotion(Apply()));
    }

    [Theory]
    [InlineData("DaHuy", true)]
    [InlineData("ChoHoanTien", false)]
    [InlineData("ChoXacNhan", false)]
    [InlineData("HoanThanh", false)]
    public async Task Promotion_FirstBookingIgnoresOnlyCancelledTickets(string otherState, bool allowed)
    {
        using var db = new MemoryContext();
        db.Conditions[0].LanDatDau = true;
        db.Bookings.Add(new DatDichVu { MaBooking = Key("OTHER"), MaUser = Key("USER1"), TrangThai = Key(otherState) });
        var result = await Promotions(db).ApplyPromotion(Apply());
        if (allowed) Assert.IsType<OkObjectResult>(result);
        else Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Promotion_QuotaCountsAllUsage_NotOnlyCurrentBooking()
    {
        using var db = new MemoryContext();
        db.Conditions[0].SoLuong = 1;
        db.Discounts.Add(new DatDichVuKhuyenMai { MaBooking = Key("OTHER"), MaKhuyenMai = db.Promotion.MaKm });
        Assert.Contains("hết lượt", Message(Assert.IsType<BadRequestObjectResult>(await Promotions(db).ApplyPromotion(Apply()))));
        Assert.Equal(2000000, db.Booking.ThanhTien);
    }

    [Theory]
    [InlineData("DaXacNhan", 1900000)]
    [InlineData("ThanhCong", 1900000)]
    [InlineData("ChoXacNhan", 100000)]
    public async Task Promotion_DoesNotInvalidatePaidOrPendingAmounts(string state, int amount)
    {
        using var db = new MemoryContext();
        db.Payments.Add(new ThanhToan { MaBooking = Key("BK1"), TrangThai = Key(state), SoTien = amount });
        Assert.IsType<BadRequestObjectResult>(await Promotions(db).ApplyPromotion(Apply()));
        Assert.Empty(db.Discounts);
    }

    [Theory]
    [InlineData("Sale", 1, 2)]
    [InlineData("Admin", 1, 2)]
    [InlineData("KhachHang", 1, 1)]
    [InlineData("Sale", 0, 1)]
    public async Task Promotion_ListAllIsStaffOnly_AndIncludesRules(string role, int all, int count)
    {
        using var db = new MemoryContext();
        db.Promos.Add(new KhuyenMai { MaKm = Key("OFF"), TrangThai = Key("NgungHoatDong") });
        db.Promotion.DieuKienKms = db.Conditions;
        db.Promotion.KmTours = [new KmTour { MaTour = Key("TOUR1") }];
        var result = Json(Assert.IsType<OkObjectResult>(await Promotions(db, role).GetActivePromotions(all)));
        Assert.Equal(count, result.GetArrayLength());
        var demo = result.EnumerateArray().Single(item => item.GetProperty("maCode").GetString() == "DEMO10");
        Assert.Equal(1000000, demo.GetProperty("dieuKien")[0].GetProperty("donToiThieu").GetInt32());
        Assert.Equal("TOUR1", demo.GetProperty("maTours")[0].GetString());
    }

    [Fact]
    public async Task Promotion_AdminCrud_WritesConditionsAndTours_AndDeletesUnusedReferences()
    {
        using var db = new MemoryContext();
        var controller = Promotions(db, "Admin");
        var created = Assert.IsType<ObjectResult>(await controller.Create(new KhuyenMaiCreateDto
        {
            TenKm = "Ưu đãi mới", MaCode = "NEW10", NgayBd = DateTime.UtcNow, NgayKt = DateTime.UtcNow.AddDays(5),
            DonVi = "%", GiamGia = 10, TrangThai = "NgungHoatDong",
            DieuKien = new DieuKienKmCreateDto { DonToiThieu = 1000000, LanDatDau = true, SoLuong = 5 }, MaTours = ["TOUR1"]
        }));
        Assert.Equal(201, created.StatusCode);
        var id = Json(created).GetProperty("maKm").GetString()!;
        Assert.Equal(5, db.Conditions.Single(row => row.MaKhuyenMai == Key(id)).SoLuong);
        Assert.Single(db.TourRules, row => row.MaKhuyenMai == Key(id));
        Assert.IsType<NoContentResult>(await controller.Update(id, new KhuyenMaiUpdateDto
        {
            TrangThai = "HoatDong", DieuKien = new DieuKienKmCreateDto { DonToiThieu = 0, LanDatDau = false }, MaTours = []
        }));
        Assert.DoesNotContain(db.TourRules, row => row.MaKhuyenMai == Key(id));
        Assert.Single(db.Conditions, row => row.MaKhuyenMai == Key(id));
        Assert.IsType<NoContentResult>(await controller.Delete(id));
        Assert.DoesNotContain(db.Promos, row => row.MaKm == Key(id));
        Assert.DoesNotContain(db.Conditions, row => row.MaKhuyenMai == Key(id));
    }

    [Fact]
    public async Task Promotion_UsedCodeCannotBeDeleted()
    {
        using var db = new MemoryContext();
        db.Discounts.Add(new DatDichVuKhuyenMai { MaBooking = Key("OTHER"), MaKhuyenMai = db.Promotion.MaKm });
        Assert.IsType<ConflictObjectResult>(await Promotions(db, "Sale").Delete("KM1"));
        Assert.Equal(Key("HoatDong"), db.Promotion.TrangThai);
        Assert.Single(db.Promos);
    }

    [Fact]
    public async Task Promotion_InvalidAdminFieldsDoNotSave()
    {
        using var db = new MemoryContext();
        var request = new KhuyenMaiCreateDto { TenKm = "Tên", MaCode = "MORETHANTEN", NgayBd = DateTime.UtcNow,
            NgayKt = DateTime.UtcNow.AddDays(1), DonVi = "%", GiamGia = 10 };
        Assert.IsType<BadRequestObjectResult>(await Promotions(db, "Admin").Create(request));
        request.MaCode = "NEW";
        request.DieuKien = new DieuKienKmCreateDto { SoLuong = -1 };
        Assert.IsType<BadRequestObjectResult>(await Promotions(db, "Admin").Create(request));
        Assert.Equal(0, db.Saves);
    }

    private static string Key(string value) => FixedLengthHelper.PadTo20(value);
    private static JsonElement Json(ObjectResult result) => JsonSerializer.SerializeToElement(result.Value);
    private static string Message(ObjectResult result) => Json(result).GetProperty("message").GetString()!;
    private static KhuyenMaiApDungDto Apply(string code = "DEMO10") => new() { MaBooking = "BK1", MaCode = code };
    private static DanhGiaTourCreateDto Review() => new() { MaTour = "TOUR1", SaoDanhGia = 5, NhanXet = "  Tuyệt vời  " };
    private static ControllerContext Viewer(string user, string role) => new()
    {
        HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("MaUser", user), new Claim(ClaimTypes.Role, role)], "test")) }
    };
    private static KhuyenMaiController Promotions(MemoryContext db, string role = "KhachHang", string user = "USER1") => new(db) { ControllerContext = Viewer(user, role) };
    private static DanhGiaController Reviews(MemoryContext db, string user = "USER1") => new(db, new NoopLogger()) { ControllerContext = Viewer(user, "KhachHang") };
    private sealed class NoopLogger : IHanhViLogger { public Task LogAsync(string user, string? tour, string action) => Task.CompletedTask; }

    private sealed class MemoryContext : AppDbContext
    {
        public List<DatDichVu> Bookings { get; } = [];
        public List<KhuyenMai> Promos { get; } = [];
        public List<DieuKienKm> Conditions { get; } = [];
        public List<KmTour> TourRules { get; } = [];
        public List<DatDichVuKhuyenMai> Discounts { get; } = [];
        public List<ThanhToan> Payments { get; } = [];
        public List<DanhGiaTour> ReviewRows { get; } = [];
        public RecordingConnection Connection { get; }
        public DatDichVu Booking => Bookings[0];
        public KhuyenMai Promotion => Promos[0];
        public int Saves { get; private set; }
        public MemoryContext() : this(new RecordingConnection()) { }
        private MemoryContext(RecordingConnection connection) : base(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options)
        {
            Connection = connection;
            Bookings.Add(new DatDichVu { MaBooking = Key("BK1"), MaTour = Key("TOUR1"), MaUser = Key("USER1"),
                TrangThai = Key("ChoXacNhan"), TongTien = 2000000, TongGiamGia = 0, ThanhTien = 2000000 });
            Promos.Add(new KhuyenMai { MaKm = Key("KM1"), TenKm = "Demo", MaCode = "DEMO10".PadRight(10), TrangThai = Key("HoatDong"),
                NgayBd = DateTime.UtcNow.AddDays(-1), NgayKt = DateTime.UtcNow.AddDays(1), DonVi = Key("%"), GiamGia = 10, CoCongDon = false });
            Conditions.Add(new DieuKienKm { MaDk = Key("DK1"), MaKhuyenMai = Promotion.MaKm, DonToiThieu = 1000000 });
            DatDichVus = new QuerySet<DatDichVu>(this, Bookings); KhuyenMais = new QuerySet<KhuyenMai>(this, Promos);
            DieuKienKms = new QuerySet<DieuKienKm>(this, Conditions); KmTours = new QuerySet<KmTour>(this, TourRules);
            DatDichVuKhuyenMais = new QuerySet<DatDichVuKhuyenMai>(this, Discounts);
            ThanhToans = new QuerySet<ThanhToan>(this, Payments); DanhGiaTours = new QuerySet<DanhGiaTour>(this, ReviewRows);
            Tours = new QuerySet<Tour>(this, [new Tour { MaTour = Key("TOUR1"), TenTour = "Test", LoaiTour = Key("Chuan") }]);
            NhomKhuyenMais = new QuerySet<NhomKhuyenMai>(this, []);
            connection.OnWrite = command =>
            {
                var values = command.Parameters.Cast<DbParameter>().Select(p => p.Value ?? throw new InvalidOperationException("Null SQL test parameter.")).ToArray();
                if (command.CommandText.Contains("dbo.DatDichVu_KhuyenMai"))
                    Discounts.Add(new DatDichVuKhuyenMai { Stt = Discounts.Count + 1, MaBooking = (string)values[0], MaKhuyenMai = (string)values[1],
                        SoTienGiam = (int)values[2], MaKhuyenMaiNavigation = Promos.Single(row => row.MaKm == (string)values[1]) });
                else if (command.CommandText.Contains("dbo.KM_Tour"))
                    TourRules.Add(new KmTour { Stt = TourRules.Count + 1, MaKhuyenMai = (string)values[0], MaTour = (string)values[1] });
                else throw new InvalidOperationException("Unexpected SQL write.");
            };
        }
        public override DatabaseFacade Database => new FakeDatabase(this);
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { Saves++; return Task.FromResult(1); }
    }
    private sealed class FakeDatabase(DbContext context) : DatabaseFacade(context)
    {
        public override Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IDbContextTransaction>(new FakeTransaction());
    }
    private sealed class FakeTransaction : IDbContextTransaction
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public void Commit() { } public void Rollback() { } public void Dispose() { }
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
    private sealed class RecordingConnection : DbConnection
    {
        public List<string> Commands { get; } = [];
        public Action<DbCommand> OnWrite { get; set; } = _ => { };
        [AllowNull] public override string ConnectionString { get; set; } = "Server=unused;Database=unused;Integrated Security=true";
        public override string Database => "unused";
        public override string DataSource => "unused";
        public override string ServerVersion => "16.0";
        private ConnectionState _state;
        public override ConnectionState State => _state;
        public override void Open() => _state = ConnectionState.Open;
        public override void Close() => _state = ConnectionState.Closed;
        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => new RecordingCommand(this);
    }
    private sealed class RecordingCommand(RecordingConnection connection) : DbCommand
    {
        private readonly SqlCommand _parameters = new();
        [AllowNull] public override string CommandText { get; set; } = "";
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; } = connection;
        protected override DbTransaction? DbTransaction { get; set; }
        protected override DbParameterCollection DbParameterCollection => _parameters.Parameters;
        protected override DbParameter CreateDbParameter() => new SqlParameter();
        public override void Cancel() { } public override void Prepare() { }
        public override int ExecuteNonQuery() { connection.Commands.Add(CommandText); connection.OnWrite(this); return 1; }
        public override object? ExecuteScalar() => throw new NotSupportedException();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) _parameters.Dispose(); base.Dispose(disposing); }
    }
    private sealed class QuerySet<T>(DbContext context, List<T> rows) : DbSet<T>, IQueryable<T> where T : class
    {
        public override Microsoft.EntityFrameworkCore.Metadata.IEntityType EntityType => context.Model.FindEntityType(typeof(T))!;
        Type IQueryable.ElementType => typeof(T);
        Expression IQueryable.Expression => new EntityQueryRootExpression(EntityType);
        IQueryProvider IQueryable.Provider => new QueryProvider(rows.AsQueryable(), EntityType.FindPrimaryKey()!.Properties.Single().Name);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => rows.GetEnumerator(); IEnumerator IEnumerable.GetEnumerator() => rows.GetEnumerator();
        public override EntityEntry<T> Add(T entity) { rows.Add(entity); return context.Entry(entity); }
        public override EntityEntry<T> Remove(T entity) { rows.Remove(entity); return context.Entry(entity); }
        public override void RemoveRange(IEnumerable<T> entities) { foreach (var entity in entities.ToArray()) Remove(entity); }
        private sealed class Roots(IQueryable<T> source, string key) : ExpressionVisitor
        {
            protected override Expression VisitExtension(Expression node)
            {
                if (node is FromSqlQueryRootExpression sql)
                {
                    var args = (object[])((ConstantExpression)sql.Argument).Value!;
                    var column = sql.Sql.Contains("WHERE MaCode", StringComparison.Ordinal) ? "MaCode" : key;
                    var item = Expression.Parameter(typeof(T), "row");
                    return source.Where(Expression.Lambda<Func<T, bool>>(Expression.Equal(Expression.Property(item, column), Expression.Constant(args[0])), item)).Expression;
                }
                return node is EntityQueryRootExpression ? source.Expression : base.VisitExtension(node);
            }
        }
        private sealed class QueryProvider(IQueryable<T> source, string key) : IAsyncQueryProvider
        {
            public IQueryable CreateQuery(Expression expression) => throw new NotSupportedException();
            public IQueryable<TItem> CreateQuery<TItem>(Expression expression) => new AsyncQuery<TItem>(new Roots(source, key).Visit(expression)!);
            public object? Execute(Expression expression) => source.Provider.Execute(new Roots(source, key).Visit(expression)!);
            public TResult Execute<TResult>(Expression expression) => source.Provider.Execute<TResult>(new Roots(source, key).Visit(expression)!);
            public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default) => TaskResult<TResult>(Execute(expression));
        }
    }
    private static TResult TaskResult<TResult>(object? result) => (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
        .MakeGenericMethod(typeof(TResult).GetGenericArguments()[0]).Invoke(null, [result])!;
    private sealed class AsyncQuery<T>(Expression expression) : EnumerableQuery<T>(expression), IAsyncEnumerable<T>, IQueryable<T>
    {
        IQueryProvider IQueryable.Provider => new AsyncProvider(this);
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new Enumerator<T>(this.AsEnumerable().GetEnumerator());
    }
    private sealed class AsyncProvider(IQueryProvider inner) : IAsyncQueryProvider
    {
        public IQueryable CreateQuery(Expression expression) => throw new NotSupportedException();
        public IQueryable<T> CreateQuery<T>(Expression expression) => new AsyncQuery<T>(expression);
        public object? Execute(Expression expression) => inner.Execute(expression);
        public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);
        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default) => TaskResult<TResult>(inner.Execute(expression));
    }
    private sealed class Enumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        public T Current => inner.Current;
        public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());
        public ValueTask DisposeAsync() { inner.Dispose(); return ValueTask.CompletedTask; }
    }
}
