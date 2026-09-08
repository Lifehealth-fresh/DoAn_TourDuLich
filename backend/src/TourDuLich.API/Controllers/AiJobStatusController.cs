using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourDuLich.Infrastructure;

namespace TourDuLich.API.Controllers;

[ApiController]
[Route("api/ai-jobs")]
[Authorize(Roles = "Admin")]
public sealed class AiJobStatusController(AppDbContext context) : ControllerBase
{
    [HttpGet("gan-nhat")]
    public async Task<ActionResult> GetLatest(CancellationToken cancellationToken)
    {
        var latest = await context.JobRunLogs.AsNoTracking()
            .Where(item => item.TenJob == "AiRecommendationRefresh")
            .OrderByDescending(item => item.ThoiDiemBatDau)
            .Select(item => new
            {
                maJobRun = item.MaJobRun,
                thoiDiemBatDau = item.ThoiDiemBatDau,
                thoiDiemKetThuc = item.ThoiDiemKetThuc,
                soBanGhi = item.SoBanGhi,
                trangThai = item.TrangThai,
                loi = item.Loi
            })
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new { job = latest });
    }
}
