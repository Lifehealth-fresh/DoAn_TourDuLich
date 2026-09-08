namespace TourDuLich.Application.Helpers;

/// <summary>Canonical pricing rule for a self-designed tour itinerary.</summary>
public static class TuThietKeTourPricing
{
    // Current product model has no commission or promotion fields. Those must
    // be added here, not reimplemented by callers, when the business model grows.
    public static int CalculateLine(int listedPrice, int quantity)
        => checked(Math.Max(0, listedPrice) * Math.Max(0, quantity));

    public static int CalculateTotal(IEnumerable<int> lineTotals)
        => checked(lineTotals.Sum());
}
