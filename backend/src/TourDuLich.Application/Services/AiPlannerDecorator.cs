using System.Text.Json;
using Microsoft.Extensions.Logging;
using TourDuLich.Infrastructure.Entities;

namespace TourDuLich.Application.Services;

public sealed class AiPlannerDecorator : IDeXuatLichTrinhService
{
    private readonly DeXuatLichTrinhService _inner;
    private readonly GeminiLlmClient _llm;
    private readonly PlannerCatalog _catalog;
    private readonly ILogger<AiPlannerDecorator> _logger;

    public AiPlannerDecorator(
        DeXuatLichTrinhService inner,
        GeminiLlmClient llm,
        PlannerCatalog catalog,
        ILogger<AiPlannerDecorator> logger)
    {
        _inner = inner;
        _llm = llm;
        _catalog = catalog;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LichTrinhDeXuat>> GenerateAsync(
        YeuCauThietKe request, DesignPlannerContext? extras = null, CancellationToken cancellationToken = default)
    {
        extras ??= DesignPlannerContext.From(request);
        if (_llm.IsEnabled)
        {
            try
            {
                var prompt = $"""
                    Form khách:
                    - Xuất phát: {extras.MaTinhXuatPhat}
                    - Đến: {request.DiemDenMongMuon} / {extras.MaTinhDen}
                    - Ngày đi: {request.NgayDuKienDi} {extras.GioKhoiHanh}
                    - Số ngày: {request.SoNgay}, NL {request.SoNguoiLon}, TE {request.SoTreEm}
                    - Ngân sách: {request.NganSachDuKien}
                    - Mục đích: {request.MucDich}
                    - Ghi chú: {request.SoThichGhiChu}
                    Hãy gọi search_sights, search_hotels, search_meals, calculate_route với đúng tỉnh.
                    Cuối cùng trả JSON thuần (không markdown):
                    {{"usedCatalog":true,"notes":"...","ready":true}}
                    Không bịa ID. Nếu thiếu KS hoặc điểm thì ready=false.
                    """;
                var text = await _llm.CompleteWithToolsAsync(
                    """
                    Bạn là bộ lập lịch tour Việt Nam. Chỉ dùng dữ liệu function.
                    Không tự tạo placeId/giá/khoảng cách. Không đặt tour.
                    Lịch không hoạt động sau 20:00 (tới muộn chỉ check-in).
                    3 mức ngân sách: tiết kiệm / cân bằng / cao cấp (±15%).
                    """,
                    prompt,
                    PlannerCatalog.PlannerDeclarations(),
                    _catalog.ExecuteAsync,
                    cancellationToken);
                if (!string.IsNullOrWhiteSpace(text))
                    _logger.LogInformation("LLM planner notes: {Notes}", text.Length > 400 ? text[..400] : text);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "LLM planner failed; falling back to CSDL generator.");
            }
        }

        return await _inner.GenerateAsync(request, extras, cancellationToken);
    }
}
