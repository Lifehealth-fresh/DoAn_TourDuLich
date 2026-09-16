namespace TourDuLich.Application.Services;

public static class ItineraryKinds
{
    public const string CheckIn = "CheckIn";
    public const string CheckOut = "CheckOut";
    public const string AnSang = "AnSang";
    public const string AnTrua = "AnTrua";
    public const string AnToi = "AnToi";
    public const string ThamQuan = "ThamQuan";
    public const string TuTuc = "TuTuc";
    public const string NghiDem = "NghiDem";

    public static bool IsMeal(string? kind) =>
        kind is AnSang or AnTrua or AnToi;

    public static bool IsVisit(string? kind) => kind == ThamQuan;

    public static string Infer(string? mota, string? maDiem, bool isHotel, bool isDining)
    {
        var text = (mota ?? "").ToLowerInvariant();
        if (text.Contains("check-in") || text.Contains("check in")) return CheckIn;
        if (text.Contains("check-out") || text.Contains("check out") || text.Contains("trả phòng")) return CheckOut;
        if (text.Contains("ăn sáng")) return AnSang;
        if (text.Contains("ăn trưa")) return AnTrua;
        if (text.Contains("ăn tối")) return AnToi;
        if (text.Contains("tự túc")) return TuTuc;
        if (text.Contains("nghỉ đêm") || text.Contains("đi ngủ")) return NghiDem;
        if (!string.IsNullOrWhiteSpace(maDiem) || text.Contains("tham quan") || text.Contains("vui chơi"))
            return ThamQuan;
        if (isDining) return AnTrua;
        if (isHotel) return NghiDem;
        return ThamQuan;
    }
}
