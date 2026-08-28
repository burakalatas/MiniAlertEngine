using AlertEngine.Core.Rules;
using Xunit;

namespace AlertEngine.Tests;

public class CombinatorTests
{
    [Fact]
    public void And_Matches_OnlyWhenAllChildrenMatch()
    {
        var points = TestData.Hourly(2500, 2800); // +12% jump into high price territory
        var rule = new AndRule(new IRule[]
        {
            new ThresholdRule("gt", 2500),
            new ChangeRule(10),
        });

        Assert.False(rule.Evaluate(TestData.ContextAt(points, 0))); // no previous hour yet
        Assert.True(rule.Evaluate(TestData.ContextAt(points, 1)));  // >2500 AND >=10% jump
    }

    [Fact]
    public void And_DoesNotMatch_WhenOnlyOneChildMatches()
    {
        var points = TestData.Hourly(2500, 2600); // high enough, but only a +4% move
        var rule = new AndRule(new IRule[]
        {
            new ThresholdRule("gt", 2500),
            new ChangeRule(10),
        });

        Assert.False(rule.Evaluate(TestData.ContextAt(points, 1)));
    }

    [Fact]
    public void Or_Matches_WhenAtLeastOneChildMatches()
    {
        var points = TestData.Hourly(-10);
        var rule = new OrRule(new IRule[]
        {
            new ThresholdRule("lt", 0),
            new ChangeRule(50),
        });

        Assert.True(rule.Evaluate(TestData.ContextAt(points, 0)));
    }

    [Fact]
    public void Or_DoesNotMatch_WhenNoChildMatches()
    {
        var points = TestData.Hourly(1500);
        var rule = new OrRule(new IRule[]
        {
            new ThresholdRule("lt", 0),
            new ThresholdRule("gt", 3000),
        });

        Assert.False(rule.Evaluate(TestData.ContextAt(points, 0)));
    }

    [Fact]
    public void Not_InvertsInnerRule()
    {
        var points = TestData.Hourly(1500, 4000);
        var rule = new NotRule(new RangeRule(1200, 3200));

        Assert.False(rule.Evaluate(TestData.ContextAt(points, 0))); // inside zone -> not outside -> false
        Assert.True(rule.Evaluate(TestData.ContextAt(points, 1)));  // outside zone -> matches "not comfort zone"
    }

    [Fact]
    public void Combinators_CanNestToArbitraryDepth()
    {
        // and( or( lt(0), gt(3000) ), not( range(1200,3200) ) )
        var points = TestData.Hourly(3500); // >3000 and outside [1200,3200]
        var rule = new AndRule(new IRule[]
        {
            new OrRule(new IRule[]
            {
                new ThresholdRule("lt", 0),
                new ThresholdRule("gt", 3000),
            }),
            new NotRule(new RangeRule(1200, 3200)),
        });

        Assert.True(rule.Evaluate(TestData.ContextAt(points, 0)));
    }
}
