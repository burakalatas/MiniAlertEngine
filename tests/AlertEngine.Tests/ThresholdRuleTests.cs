using AlertEngine.Core.Rules;
using Xunit;

namespace AlertEngine.Tests;

public class ThresholdRuleTests
{
    [Fact]
    public void Gt_Matches_WhenPriceAboveValue()
    {
        var points = TestData.Hourly(2347.60, 4200.00);
        var rule = new ThresholdRule("gt", 3000);

        Assert.False(rule.Evaluate(TestData.ContextAt(points, 0)));
        Assert.True(rule.Evaluate(TestData.ContextAt(points, 1)));
    }

    [Fact]
    public void Gt_DoesNotMatch_OnExactBoundary()
    {
        var points = TestData.Hourly(3000.00);
        var rule = new ThresholdRule("gt", 3000);

        Assert.False(rule.Evaluate(TestData.ContextAt(points, 0)));
    }

    [Fact]
    public void Lt_Matches_WhenPriceBelowValue()
    {
        var points = TestData.Hourly(50.0, 150.0);
        var rule = new ThresholdRule("lt", 100);

        Assert.True(rule.Evaluate(TestData.ContextAt(points, 0)));
        Assert.False(rule.Evaluate(TestData.ContextAt(points, 1)));
    }

    [Fact]
    public void UnknownOperator_Throws()
    {
        var points = TestData.Hourly(100.0);
        var rule = new ThresholdRule("gte", 100);

        Assert.Throws<NotSupportedException>(() => rule.Evaluate(TestData.ContextAt(points, 0)));
    }
}
