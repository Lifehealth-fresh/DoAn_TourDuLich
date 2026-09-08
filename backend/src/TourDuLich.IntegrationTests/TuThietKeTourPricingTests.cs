using FluentAssertions;
using TourDuLich.Application.Helpers;

namespace TourDuLich.IntegrationTests;

public sealed class TuThietKeTourPricingTests
{
    [Fact]
    public void CanonicalFormula_UsesListedPriceTimesQuantity_AndSumsLines()
    {
        var first = TuThietKeTourPricing.CalculateLine(250_000, 2);
        var second = TuThietKeTourPricing.CalculateLine(100_000, 3);

        TuThietKeTourPricing.CalculateTotal(new[] { first, second })
            .Should().Be(800_000);
    }
}
