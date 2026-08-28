namespace AlertEngine.Core.Rules;

/// <summary>
/// Matches when the current price falls outside the [min, max] band
/// (strictly below min, or strictly above max). Being exactly on the
/// boundary counts as "inside" the band.
/// </summary>
public sealed class RangeRule : IRule
{
    private readonly double _min;
    private readonly double _max;

    public RangeRule(double min, double max)
    {
        _min = min;
        _max = max;
    }

    public bool Evaluate(EvaluationContext ctx)
    {
        var price = ctx.Current.Price;
        return price < _min || price > _max;
    }
}
