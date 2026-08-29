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
        // RangeRule matches when the price is OUTSIDE [min, max] (see RangeRule.cs).
        // Wrapping it in NotRule therefore matches when the price is INSIDE the band -
        // this is the same "not(range) = inside" behaviour documented in the README
        // for the sample rules.json's "outside-comfort-zone" rule.
        var points = TestData.Hourly(1500, 4000);
        var rule = new NotRule(new RangeRule(1200, 3200));

        Assert.True(rule.Evaluate(TestData.ContextAt(points, 0)));  // 1500 is inside [1200,3200] -> not(outside) -> true
        Assert.False(rule.Evaluate(TestData.ContextAt(points, 1))); // 4000 is outside [1200,3200] -> not(outside) -> false
    }

    [Fact]
    public void Combinators_CanNestToArbitraryDepth()
    {
        // and( or( lt(0), gt(3000) ), not( range(1200,4000) ) )
        // NotRule(RangeRule) matches when the price is INSIDE the given band
        // (RangeRule itself matches OUTSIDE it - see RangeRuleTests / README).
        // 3500 satisfies gt(3000) via the "or", and is inside [1200,4000] so
        // not(range(1200,4000)) is also true - this exercises three levels of
        // nesting (and > or > threshold, and > not > range) in one rule.
        var points = TestData.Hourly(3500);
        var rule = new AndRule(new IRule[]
        {
            new OrRule(new IRule[]
            {
                new ThresholdRule("lt", 0),
                new ThresholdRule("gt", 3000),
            }),
            new NotRule(new RangeRule(1200, 4000)),
        });

        Assert.True(rule.Evaluate(TestData.ContextAt(points, 0)));
    }
}
