using System;
using System.Collections.Generic;
using System.Text;

namespace TourDuLich.Application.Helpers;

public static class FixedLengthHelper
{
    public static string PadTo20(string? value) => (value ?? string.Empty).Trim().PadRight(20);

    public static string? TrimSafe(string? value) => value?.Trim();
}
