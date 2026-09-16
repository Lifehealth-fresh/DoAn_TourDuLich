using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TourDuLich.Infrastructure;
using TourDuLich.Application.Services;
using Microsoft.OpenApi;
using TourDuLich.API.Swagger;
using TourDuLich.API.Authorization;
using TourDuLich.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();

var corsOriginsSection = builder.Configuration.GetSection("Cors:Origins");
var corsOrigins = (corsOriginsSection.Value ?? string.Empty)
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
if (corsOrigins.Length == 0)
{
    corsOrigins = corsOriginsSection.GetChildren()
        .Select(item => item.Value)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value!)
        .ToArray();
}

corsOrigins = corsOrigins
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

if (corsOrigins.Length == 0 && builder.Environment.IsDevelopment())
{
    corsOrigins = ["http://localhost:5173", "http://localhost:5174"];
}
else if (corsOrigins.Length == 0)
{
    corsOrigins =
    [
        "http://localhost:5173",
        "http://localhost:5174",
        "https://calm-smoke-0872fdc00.azurestaticapps.net",
        "https://green-sand-09f811a00.azurestaticapps.net"
    ];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Nhập JWT token nhận được từ /api/Auth/login."
    });

    options.OperationFilter<AuthorizeCheckOperationFilter>();
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IHanhViLogger, HanhViLogger>();
builder.Services.AddScoped<PlannerCatalog>();
builder.Services.AddScoped<ITravelTimeService, TravelTimeService>();
builder.Services.AddScoped<DeXuatLichTrinhService>();
builder.Services.AddScoped<IDeXuatLichTrinhService>(sp => sp.GetRequiredService<DeXuatLichTrinhService>());
builder.Services.AddScoped<IDestinationResolver, DestinationResolver>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ITourMediaStorage, TourMediaStorage>();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IAiRecommendationClient, AiRecommendationClient>();
builder.Services.AddSignalR();
builder.Services.AddHostedService<AiRecommendationRefreshWorker>();

var authPermitLimit = builder.Environment.IsDevelopment() ? 1000 : 10;
var authWindowMinutes = builder.Environment.IsDevelopment() ? 1 : 15;
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            """{"message":"Quá nhiều lần thử. Vui lòng đợi rồi đăng nhập lại."}""",
            token);
    };
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPermitLimit,
                Window = TimeSpan.FromMinutes(authWindowMinutes),
                QueueLimit = 0
            }));
});

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT key is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT issuer is not configured.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT audience is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    context.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TourDuLich.API.Hubs.HoTroHub>("/hubs/hotro").RequireCors("Frontend");

app.Run();

public partial class Program
{
}
