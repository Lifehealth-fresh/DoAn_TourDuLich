using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourDuLich.API.DTOs;
using TourDuLich.Application.Helpers;
using TourDuLich.Application.Services;
using TourDuLich.Infrastructure;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiGoiYController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAiRecommendationClient _aiClient;
    private readonly ILogger<AiGoiYController> _logger;

    public AiGoiYController(
        AppDbContext context,
        IAiRecommendationClient aiClient,
        ILogger<AiGoiYController> logger)
    {
        _context = context;
        _aiClient = aiClient;
        _logger = logger;
    }

    [HttpPost("sinh-goi-y")]
    public async Task<ActionResult> Generate(AiGoiYCreateDto request, CancellationToken cancellationToken)
    {
        var currentUser = User.FindFirst("MaUser")?.Value;
        if (currentUser is null)
            return Unauthorized();

        var requestedUser = string.IsNullOrWhiteSpace(request.MaUser)
            ? currentUser
            : request.MaUser.Trim();
        var isStaff = User.IsInRole("Sale") || User.IsInRole("Admin");
        if (!isStaff && !string.Equals(requestedUser, currentUser, StringComparison.OrdinalIgnoreCase))
            return Forbid();
        if (request.SoLuong is < 1 or > 50)
            return BadRequest(new { message = "SoLuong phải từ 1 đến 50." });
        if (request.Alpha is < 0 or > 1)
            return BadRequest(new { message = "Alpha phải nằm trong khoảng 0 đến 1." });

        var maUserDb = FixedLengthHelper.PadTo20(requestedUser);
        if (!await _context.NguoiSuDungs.AnyAsync(item => item.MaUser == maUserDb, cancellationToken))
            return NotFound(new { message = "Người dùng không tồn tại." });

        IReadOnlyList<AiRecommendationResult> recommendations;
        try
        {
            recommendations = await _aiClient.GetRecommendationsAsync(
                maUserDb.Trim(), request.SoLuong, request.Alpha, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "AI Recommendation Service hiện không khả dụng. Vui lòng thử lại sau."
            });
        }

        var now = DateTime.UtcNow;
        var saved = new List<AigoiY>();
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var oldRecommendations = await _context.AigoiYs
            .Where(item => item.MaUser == maUserDb)
            .ToListAsync(cancellationToken);
        _context.AigoiYs.RemoveRange(oldRecommendations);

        foreach (var recommendation in recommendations
            .Where(item => !string.IsNullOrWhiteSpace(item.MaTour))
            .GroupBy(item => FixedLengthHelper.PadTo20(item.MaTour), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item => item.DiemPhuHop).First()))
        {
            var maTourDb = FixedLengthHelper.PadTo20(recommendation.MaTour);
            if (!await _context.Tours.AnyAsync(item => item.MaTour == maTourDb, cancellationToken))
                continue;
            saved.Add(new AigoiY
            {
                MaRecommodation = await GenerateIdAsync(cancellationToken),
                MaUser = maUserDb,
                MaTour = maTourDb,
                DiemPhuHop = Math.Clamp(recommendation.DiemPhuHop, 0, 1),
                LyDo = recommendation.LyDo?.Trim(),
                NgayGoiY = now
            });
        }

        _context.AigoiYs.AddRange(saved);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(saved.Select(ToResponse));
    }

    [HttpGet("cua-toi")]
    public async Task<ActionResult> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var currentUser = User.FindFirst("MaUser")?.Value;
        if (currentUser is null)
            return Unauthorized();
        var maUserDb = FixedLengthHelper.PadTo20(currentUser);

        try
        {
            var query = _context.AigoiYs.AsNoTracking().Where(item => item.MaUser == maUserDb);
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var totalCount = await query.CountAsync(cancellationToken);
            var result = await query.OrderByDescending(item => item.NgayGoiY).ThenBy(item => item.MaRecommodation)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(ToProjection())
                .ToListAsync(cancellationToken);
            return Ok(new { items = result, page, pageSize, totalCount });
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Không thể đọc gợi ý AI cho user {MaUser}.", maUserDb);
            return Ok(new { items = Array.Empty<object>(), page, pageSize = Math.Clamp(pageSize, 1, 100), totalCount = 0 });
        }
    }

    private async Task<string> GenerateIdAsync(CancellationToken cancellationToken)
    {
        string id;
        do
        {
            id = FixedLengthHelper.PadTo20($"AI{Guid.NewGuid():N}"[..20].ToUpperInvariant());
        } while (await _context.AigoiYs.AnyAsync(item => item.MaRecommodation == id, cancellationToken));
        return id;
    }

    private static object ToResponse(AigoiY item) => new
    {
        maRecommodation = FixedLengthHelper.TrimSafe(item.MaRecommodation),
        maUser = FixedLengthHelper.TrimSafe(item.MaUser),
        maTour = FixedLengthHelper.TrimSafe(item.MaTour),
        diemPhuHop = item.DiemPhuHop,
        lyDo = item.LyDo,
        ngayGoiY = item.NgayGoiY
    };

    private static System.Linq.Expressions.Expression<Func<AigoiY, object>> ToProjection() => item => new
    {
        maRecommodation = FixedLengthHelper.TrimSafe(item.MaRecommodation),
        maUser = FixedLengthHelper.TrimSafe(item.MaUser),
        maTour = FixedLengthHelper.TrimSafe(item.MaTour),
        diemPhuHop = item.DiemPhuHop,
        lyDo = item.LyDo,
        ngayGoiY = item.NgayGoiY
    };
}
