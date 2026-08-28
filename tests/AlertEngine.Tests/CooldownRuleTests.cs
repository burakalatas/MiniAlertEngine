using AlertEngine.Core.Rules;
using Xunit;

namespace AlertEngine.Tests;

public class CooldownRuleTests
{
    [Fact]
    public void FiresOnFirstMatch_ThenSuppressesUntilWindowElapses()
    {
        // Prices stay above 2800 for 8 straight hours; cooldown is 6h.
        var points = TestData.Hourly(2900, 2900, 2900, 2900, 2900, 2900, 2900, 2900);
        var rule = new CooldownRule(6, new ThresholdRule("gt", 2800));

        var fired = new List<int>();
        for (int i = 0; i < points.Count; i++)
        {
            if (rule.Evaluate(TestData.ContextAt(points, i)))
                fired.Add(i);
        }

        // Fires at hour 0, then must wait 6 full hours -> next allowed fire is hour 6.
        Assert.Equal(new[] { 0, 6 }, fired);
    }

    [Fact]
    public void DoesNotFire_WhenInnerRuleNeverMatches()
    {
        var points = TestData.Hourly(100, 100, 100);
        var rule = new CooldownRule(6, new ThresholdRule("gt", 2800));

        for (int i = 0; i < points.Count; i++)
        {
            Assert.False(rule.Evaluate(TestData.ContextAt(points, i)));
        }
    }

    [Fact]
    public void ResumesFiring_AfterInnerRuleStopsAndStartsMatchingAgain()
    {
        // Matches at hour 0 (fires), drops away, comes back at hour 2 - but
        // cooldown window (6h) hasn't elapsed, so hour 2 is suppressed.
        var points = TestData.Hourly(2900, 100, 2900);
        var rule = new CooldownRule(6, new ThresholdRule("gt", 2800));

        Assert.True(rule.Evaluate(TestData.ContextAt(points, 0)));
        Assert.False(rule.Evaluate(TestData.ContextAt(points, 1)));
        Assert.False(rule.Evaluate(TestData.ContextAt(points, 2)));
    }
}
