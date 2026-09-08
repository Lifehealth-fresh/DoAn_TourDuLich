namespace TourDuLich.Application.Services;

public interface IHanhViLogger
{
    Task LogAsync(string maUserDb, string? maTourDb, string hanhDong);
}
