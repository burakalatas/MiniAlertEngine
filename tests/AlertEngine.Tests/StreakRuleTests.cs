using AlertEngine.Core.Rules;
using Xunit;

namespace AlertEngine.Tests;

public class StreakRuleTests
{
    [Fact]
    public void Matches_AfterThreeConsecutiveRises()
    {
        var points = TestData.Hourly(100, 110, 120, 130);
        var rule = new StreakRule("up", 3);

        Assert.False(rule.Evaluate(TestData.ContextAt(points, 0)));
        Assert.False(rule.Evaluate(TestData.ContextAt(points, 1)));
        Assert.False(rule.Evaluate(TestData.ContextAt(points, 2))); // only 2 rises so far
        Assert.True(rule.Evaluate(TestData.ContextAt(points, 3)));  // 3 rises: 100<110<120<130
    }

    [Fact]
    public void DoesNotMatch_WhenAStepGoesTheWrongWay()
    {
        var points = TestData.Hourly(100, 110, 105, 130);
        var rule = new StreakRule("up", 3);

        Assert.False(rule.Evaluate(TestData.ContextAt(points, 3)));
    }

    [Fact]
    public void DoesNotMatch_OnFlatStep()
    {
        var points = TestData.Hourly(100, 110, 110, 130);
        var rule = new StreakRule("up", 3);

        Assert.False(rule.Evaluate(TestData.ContextAt(points, 3)));
    }

    [Fact]
    public void Matches_ForDownDirection()
    {
        var points = TestData.Hourly(130, 120, 110, 100);
        var rule = new StreakRule("down", 3);

        Assert.True(rule.Evaluate(TestData.ContextAt(points, 3)));
    }

    [Fact]
    public void DoesNotMatch_WhenNotEnoughHistory()
    {
        var points = TestData.Hourly(100, 110);
        var rule = new StreakRule("up", 3);

        Assert.False(rule.Evaluate(TestData.ContextAt(points, 1)));
    }

    [Fact]
    public void DoesNotMatch_AcrossAGapInTheData()
    {
        // hours 0,1,2,4 (hour 3 missing) all rising in price - but the gap breaks the streak.
        var points = TestData.HourlyWithGap(gapBeforeIndex: 3, 100, 110, 120, 130);
        var rule = new StreakRule("up", 3);

        Assert.False(rule.Evaluate(TestData.ContextAt(points, 3)));
    }

    [Fact]
    public void UnknownDirection_Throws()
    {
        Assert.Throws<NotSupportedException>(() => new StreakRule("sideways", 3));
    }
}
