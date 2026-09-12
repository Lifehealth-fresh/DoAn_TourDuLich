namespace TourDuLich.Application.Services;

/// <summary>
/// The only permitted lifecycle transitions for a self-designed-tour request
/// and the Tour created from it. Callers map an invalid transition to HTTP 409.
/// </summary>
public static class YeuCauThietKeStateMachine
{
    public const string Moi = "Moi";
    public const string Huy = "Huy";
    public const string DangThietKe = "DangThietKe";
    public const string CanChinhSua = "CanChinhSua";
    public const string ChoKhachXacNhan = "ChoKhachXacNhan";
    public const string ChoDuyet = "ChoDuyet";
    public const string DaDuyet = "DaDuyet";
    public const string Nhap = "Nhap";
    public const string ChoXacNhan = "ChoXacNhan";
    public const string DaXacNhan = "DaXacNhan";

    public static bool CanCancel(string? requestState) => requestState == Moi;
    public static bool CanGenerateProposals(string? requestState) => requestState == Moi;
    public static bool CanChooseProposal(string? requestState, bool hasCreatedTour)
        => requestState == Moi && !hasCreatedTour;
    public static bool CanEditSchedule(string? requestState, string? tourState)
        => tourState == Nhap && (requestState == DangThietKe || requestState == CanChinhSua);
    public static bool CanSubmitForApproval(string? requestState, string? tourState)
        => tourState == Nhap && (requestState == DangThietKe || requestState == CanChinhSua);
    public static bool CanCustomerRespond(string? requestState, string? tourState)
        => requestState == ChoKhachXacNhan && tourState == ChoXacNhan;
    public static bool CanReject(string? requestState, string? tourState)
        => requestState == ChoDuyet && tourState == ChoXacNhan;
    public static bool CanApprove(string? requestState, string? tourState)
        => requestState == ChoDuyet && tourState == ChoXacNhan;

    public static string Describe(string? requestState, string? tourState = null)
        => tourState is null
            ? $"Yêu cầu đang ở trạng thái '{requestState}'."
            : $"Yêu cầu đang ở trạng thái '{requestState}', tour ở trạng thái '{tourState}'.";
}
