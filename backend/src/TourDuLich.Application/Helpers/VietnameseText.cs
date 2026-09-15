using System.Globalization;
using System.Text;

namespace TourDuLich.Application.Helpers;

public static class VietnameseText
{
    public static string Fold(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        var form = value.Trim().ToLowerInvariant().Replace('đ', 'd').Replace('Đ', 'd');
        var normalized = form.Normalize(NormalizationForm.FormD);
        var buffer = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
                buffer.Append(ch);
        }
        return string.Join(' ', buffer.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    public static bool ContainsFold(string? haystack, string? needle)
    {
        var h = Fold(haystack);
        var n = Fold(needle);
        return n.Length > 0 && h.Contains(n, StringComparison.Ordinal);
    }

    public static bool EqualsFold(string? left, string? right)
        => Fold(left) == Fold(right);
}
