using AlertEngine.Core.Rules;
using Xunit;

namespace AlertEngine.Tests;

public class ChangeRuleTests
{
    [Fact]
    public void Matches_OnJumpOfAtLeastPercent()
    {
        // 2000 -> 2500 is a +25% jump.
        var points = TestData.Hourly(2000, 2500);
        var rule = new ChangeRule(20);

        Assert.True(rule.Evaluate(TestData.ContextAt(points, 1)));
    }

    [Fact]
    public void Matches_OnDropOfAtLeastPercent()
    {
        // 2500 -> 2000 is a -20% move, exactly at the threshold.
        var points = TestData.Hourly(2500, 2000);
        var rule = new ChangeRule(20);

        Assert.True(rule.Evaluate(TestData.ContextAt(points, 1)));
    }

    [Fact]
    public void DoesNotMatch_BelowThreshold()
    {
        var points = TestData.Hourly(2000, 2100); // +5%
        var rule = new ChangeRule(20);

        Assert.False(rule.Evaluate(TestData.ContextAt(points, 1)));
    }

    [Fact]
    public void FirstHour_NeverMatches_NoPreviousPrice()
    {
        var points = TestData.Hourly(9999);
        var rule = new ChangeRule(1);

        Assert.False(rule.Evaluate(TestData.ContextAt(points, 0)));
    }

    [Fact]
    public void MissingHourGap_DoesNotMatch()
    {
        // Index 1 and index 2 are 2 real hours apart because index 2 is missing an hour.
        var points = TestData.HourlyWithGap(gapBeforeIndex: 2, 2000, 2000, 5000);
        var rule = new ChangeRule(1);

        Assert.False(rule.Evaluate(TestData.ContextAt(points, 2)));
    }

    [Fact]
    public void PreviousPriceZero_AnyNonZeroCurrentMatches()
    {
        var points = TestData.Hourly(0, 5);
        var rule = new ChangeRule(1000); // percent doesn't matter for this edge case

        Assert.True(rule.Evaluate(TestData.ContextAt(points, 1)));
    }

    [Fact]
    public void PreviousAndCurrentBothZero_DoesNotMatch()
    {
        var points = TestData.Hourly(0, 0);
        var rule = new ChangeRule(1);

        Assert.False(rule.Evaluate(TestData.ContextAt(points, 1)));
    }
}
