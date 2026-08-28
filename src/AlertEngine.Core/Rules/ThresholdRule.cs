namespace AlertEngine.Core.Rules;

/// <summary>
/// Matches when the current price is above ("gt") or below ("lt") a fixed value.
/// </summary>
public sealed class ThresholdRule : IRule
{
    private readonly string _operator;
    private readonly double _value;

    public ThresholdRule(string @operator, double value)
    {
        _operator = @operator;
        _value = value;
    }

    public bool Evaluate(EvaluationContext ctx)
    {
        var price = ctx.Current.Price;
        return _operator switch
        {
            "gt" => price > _value,
            "lt" => price < _value,
            _ => throw new NotSupportedException(
                $"Unknown threshold operator '{_operator}'. Expected 'gt' or 'lt'.")
        };
    }
}
