namespace AlertEngine.Core.Rules;

/// <summary>
/// Matches when the price moved by at least Percent% versus the previous hour,
/// in either direction (up or down).
///
/// Documented decisions (see README "Belirsiz durumlar"):
///  - First hour in the file (no previous point at all) -> does not match.
///  - Missing hour in the data (previous list entry exists but is not exactly
///    one hour earlier, e.g. the 2026-08-12 03:00 gap in the sample data) ->
///    treated the same as "no previous point": does not match. Comparing
///    across a 2-hour gap as if it were 1 hour would silently misreport the
///    speed of the move, which felt worse than just staying quiet for that hour.
///  - Previous price of exactly 0 -> any non-zero current price counts as a
///    match (percentage change from 0 is undefined/infinite; we treat "moved
///    away from zero at all" as clearly a match rather than throwing).
/// </summary>
public sealed class ChangeRule : IRule
{
    private readonly double _percent;

    public ChangeRule(double percent)
    {
        _percent = percent;
    }

    public bool Evaluate(EvaluationContext ctx)
    {
        var previous = ctx.PreviousHourOrNull();
        if (previous is null) return false;

        var previousPrice = previous.Price;
        var currentPrice = ctx.Current.Price;

        if (previousPrice == 0)
            return currentPrice != 0;

        var percentChange = Math.Abs(currentPrice - previousPrice) / Math.Abs(previousPrice) * 100.0;
        return percentChange >= _percent;
    }
}
