using System.Text.Json;
using TourDuLich.API.DTOs;
using TourDuLich.Application.Services;

namespace TourDuLich.IntegrationTests;
public sealed class DesignRevisionReasonTests
{
    [Theory]
    [InlineData(null, null, null)]
    [InlineData("  ", null, null)]
    [InlineData("Lý do cũ", "Lý do cũ", "Admin")]
    [InlineData("[Admin]  Sửa giá  ", "Sửa giá", "Admin")]
    [InlineData("[KhachHang]  Thêm giờ nghỉ  ", "Thêm giờ nghỉ", "KhachHang")]
    public void Read_ParsesSourceWithoutExposingPrefix(string? stored, string? text, string? source)
    {
        Assert.Equal((text, source), DesignRevisionReason.Read(stored));
        var json = JsonSerializer.SerializeToElement(new DesignRequestView { StoredReason = stored }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(text, json.GetProperty("lyDo").GetString());
        Assert.Equal(source, json.GetProperty("nguonLyDo").GetString());
        Assert.False(json.TryGetProperty("storedReason", out _));
        Assert.DoesNotContain("[KhachHang]", json.GetRawText());
        Assert.DoesNotContain("[Admin]", json.GetRawText());
    }
    [Fact]
    public void Write_UsesExactlyOneSourcePrefixAndTrimmedText()
    {
        Assert.Equal("[Admin] Sửa giá", DesignRevisionReason.FromAdmin("  Sửa giá  "));
        Assert.Equal("[KhachHang] Đổi ngày", DesignRevisionReason.FromCustomer("  Đổi ngày  "));
    }
}

