namespace TourDuLich.Application.Services;

public static class DesignRevisionReason
{
    public static string FromAdmin(string reason) => "[Admin] " + reason.Trim();
    public static string FromCustomer(string reason) => "[KhachHang] " + reason.Trim();
    public static (string? LyDo, string? NguonLyDo) Read(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored)) return (null, null);
        stored = stored.Trim();
        var source = "Admin";
        if (stored.StartsWith("[KhachHang]", StringComparison.Ordinal))
        {
            source = "KhachHang";
            stored = stored["[KhachHang]".Length..].Trim();
        }
        else if (stored.StartsWith("[Admin]", StringComparison.Ordinal))
            stored = stored["[Admin]".Length..].Trim();
        return string.IsNullOrWhiteSpace(stored) ? (null, null) : (stored, source);
    }
}
