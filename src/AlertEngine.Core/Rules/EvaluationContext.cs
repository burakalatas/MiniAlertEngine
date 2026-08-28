using AlertEngine.Core.Models;

namespace AlertEngine.Core.Rules;

/// <summary>
/// Everything a rule needs to decide whether it matches "right now".
/// Rules only ever look at Points[0..Index] (the past and present),
/// never into the future - that keeps the engine a genuine single pass.
/// </summary>
public sealed class EvaluationContext
{
    public required IReadOnlyList<PricePoint> Points { get; init; }
    public required int Index { get; init; }

    public PricePoint Current => Points[Index];

    /// <summary>
    /// Returns the point exactly one hour before Points[Index], but only if
    /// it truly is the immediately preceding hour (no gap in the data) and
    /// only if it exists at all. See README section "Missing hours" for why
    /// this matters: the sample data has at least one hole (2026-08-12 03:00).
    /// </summary>
    public PricePoint? PreviousHourOrNull()
    {
        if (Index <= 0) return null;
        var prev = Points[Index - 1];
        var expectedGap = TimeSpan.FromHours(1);
        return Current.Timestamp - prev.Timestamp == expectedGap ? prev : null;
    }

    /// <summary>
    /// Walks backwards "count" consecutive hours from Current, requiring every
    /// step to be an exact one-hour gap. Returns the points in chronological
    /// order (oldest first, Current last), or null if there isn't enough
    /// unbroken history. Used by StreakRule.
    /// </summary>
    public IReadOnlyList<PricePoint>? LastConsecutiveHours(int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (Index - count < 0) return null;

        var window = new PricePoint[count + 1];
        for (int i = 0; i <= count; i++)
        {
            window[count - i] = Points[Index - i];
        }

        for (int i = 1; i < window.Length; i++)
        {
            if (window[i].Timestamp - window[i - 1].Timestamp != TimeSpan.FromHours(1))
                return null;
        }

        return window;
    }
}
