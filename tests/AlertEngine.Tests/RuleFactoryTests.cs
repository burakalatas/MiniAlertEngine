using AlertEngine.Core.Models;
using AlertEngine.Core.Rules;
using Xunit;

namespace AlertEngine.Tests;

public class RuleFactoryTests
{
    [Fact]
    public void Builds_NestedAndOrNot_CorrectlyFromRawShape()
    {
        // and( or( lt(0), change(50) ), not( range(1200,3200) ) )
        var raw = new RawRule
        {
            Id = "abnormal-and-uncomfortable",
            Type = "and",
            Message = "test",
            Rules = new List<RawRule>
            {
                new()
                {
                    Type = "or",
                    Rules = new List<RawRule>
                    {
                        new() { Type = "threshold", Operator = "lt", Value = 0 },
                        new() { Type = "change", Percent = 50 },
                    }
                },
                new()
                {
                    Type = "not",
                    Rule = new RawRule { Type = "range", Min = 1200, Max = 3200 },
                }
            }
        };

        var rule = RuleFactory.Build(raw);

        var points = TestData.Hourly(-10); // lt(0) matches, and it's outside [1200,3200]
        Assert.True(rule.Evaluate(TestData.ContextAt(points, 0)));
    }

    [Fact]
    public void UnknownType_Throws()
    {
        var raw = new RawRule { Type = "made-up-type" };
        Assert.Throws<NotSupportedException>(() => RuleFactory.Build(raw));
    }

    [Fact]
    public void MissingRequiredField_ThrowsInvalidDataException()
    {
        var raw = new RawRule { Type = "threshold", Operator = "gt" /* missing Value */ };
        Assert.Throws<InvalidDataException>(() => RuleFactory.Build(raw));
    }
}
