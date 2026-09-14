using System.Text.RegularExpressions;

namespace TourDuLich.Application.Helpers;

public static class CredentialRules
{
    private static readonly Regex PhonePattern = new(@"^0\d{9}$", RegexOptions.Compiled);

    public static string? ValidatePhone(string? soDienThoai)
    {
        var phone = soDienThoai?.Trim() ?? string.Empty;
        if (!PhonePattern.IsMatch(phone))
            return "Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0.";
        return null;
    }

    public static string? ValidatePassword(string? matKhau)
    {
        if (string.IsNullOrEmpty(matKhau) || matKhau.Length < 8)
            return "Mật khẩu phải có ít nhất 8 ký tự.";
        if (!matKhau.Any(char.IsLetter) || !matKhau.Any(char.IsDigit))
            return "Mật khẩu phải gồm cả chữ và số.";
        return null;
    }

    public static string? Validate(string? soDienThoai, string? matKhau) =>
        ValidatePhone(soDienThoai) ?? ValidatePassword(matKhau);
}
