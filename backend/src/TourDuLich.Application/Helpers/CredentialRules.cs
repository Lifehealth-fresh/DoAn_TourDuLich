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

    public static string? ValidateConfirm(string? matKhau, string? xacNhan)
    {
        if (!string.Equals(matKhau, xacNhan, StringComparison.Ordinal))
            return "Xác nhận mật khẩu không khớp.";
        return null;
    }

    public static string? ValidateCccd(string? soCccd)
    {
        var value = soCccd?.Trim() ?? string.Empty;
        if (!Regex.IsMatch(value, @"^\d{9}$|^\d{12}$"))
            return "Số CCCD phải gồm 9 hoặc 12 chữ số.";
        return null;
    }

    public static string? ValidateName(string? ho, string? ten)
    {
        if (string.IsNullOrWhiteSpace(ho) || string.IsNullOrWhiteSpace(ten))
            return "Họ và tên không được để trống.";
        return null;
    }

    public static string? Validate(string? soDienThoai, string? matKhau) =>
        ValidatePhone(soDienThoai) ?? ValidatePassword(matKhau);
}
