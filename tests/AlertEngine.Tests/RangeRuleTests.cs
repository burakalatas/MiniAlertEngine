using AlertEngine.Core.Rules;
using Xunit;

namespace AlertEngine.Tests;

public class RangeRuleTests
{
    [Theory]
    [InlineData(-50.0, true)]   // below min
    [InlineData(0.0, false)]    // on min boundary -> inside
    [InlineData(1750.0, false)] // comfortably inside
    [InlineData(3500.0, false)] // on max boundary -> inside
    [InlineData(4200.0, true)]  // above max
    public void MatchesOnlyOutsideTheBand(double price, bool expectedMatch)
    {
        var points = TestData.Hourly(price);
        var rule = new RangeRule(0, 3500);

        Assert.Equal(expectedMatch, rule.Evaluate(TestData.ContextAt(points, 0)));
    }
}
