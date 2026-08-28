namespace AlertEngine.Core.Rules;

/// <summary>
/// Matches when the price has moved in the same direction for "hours"
/// consecutive hours, ending at (and including) the current hour.
///
/// Example: hours=3, direction=up matches at hour H if
///   price[H-3] &lt; price[H-2] &lt; price[H-1] &lt; price[H]
/// i.e. three consecutive up-moves, which needs 4 unbroken data points.
///
/// Documented decisions:
///  - Needs Hours+1 *unbroken*, exactly-hourly data points. If the file has
///    a gap in that window (like the missing 2026-08-12 03:00 row) the
///    streak cannot be evaluated and simply does not match - we don't try
///    to "bridge" the gap or guess what the missing hour would have been.
///  - A flat (equal) price breaks the streak in both directions: it is
///    neither an "up" move nor a "down" move.
///  - This rule does not need any memory beyond the price list itself
///    (it recomputes the window from Points/Index each time), so unlike
///    CooldownRule it has no mutable instance state.
/// </summary>
public sealed class StreakRule : IRule
{
    private readonly string _direction;
    private readonly int _hours;

    public StreakRule(string direction, int hours)
    {
        if (direction is not ("up" or "down"))
            throw new NotSupportedException($"Unknown streak direction '{direction}'. Expected 'up' or 'down'.");
        if (hours <= 0)
            throw new ArgumentOutOfRangeException(nameof(hours), "Streak length must be positive.");

        _direction = direction;
        _hours = hours;
    }

    public bool Evaluate(EvaluationContext ctx)
    {
        var window = ctx.LastConsecutiveHours(_hours);
        if (window is null) return false;

        for (int i = 1; i < window.Count; i++)
        {
            var prev = window[i - 1].Price;
            var curr = window[i].Price;

            var stepMatches = _direction == "up" ? curr > prev : curr < prev;
            if (!stepMatches) return false;
        }

        return true;
    }
}
