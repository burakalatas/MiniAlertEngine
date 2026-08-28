using AlertEngine.Core.Models;
using AlertEngine.Core.Rules;

namespace AlertEngine.Tests;

/// <summary>
/// Helpers for building small, readable price sequences without needing a
/// real JSON file for every test.
/// </summary>
internal static class TestData
{
    private static readonly DateTimeOffset Start =
        new(2026, 8, 10, 0, 0, 0, TimeSpan.FromHours(3));

    /// <summary>Hourly points starting at 2026-08-10T00:00+03:00, one per price given.</summary>
    public static List<PricePoint> Hourly(params double[] prices)
    {
        var points = new List<PricePoint>();
        for (int i = 0; i < prices.Length; i++)
        {
            points.Add(new PricePoint { Timestamp = Start.AddHours(i), Price = prices[i] });
        }
        return points;
    }

    /// <summary>Same as Hourly, but skips the hour at gapBeforeIndex (creates a 2h gap there).</summary>
    public static List<PricePoint> HourlyWithGap(int gapBeforeIndex, params double[] prices)
    {
        var points = new List<PricePoint>();
        var hour = 0;
        for (int i = 0; i < prices.Length; i++)
        {
            if (i == gapBeforeIndex) hour++; // skip one hour slot
            points.Add(new PricePoint { Timestamp = Start.AddHours(hour), Price = prices[i] });
            hour++;
        }
        return points;
    }

    public static EvaluationContext ContextAt(List<PricePoint> points, int index) =>
        new() { Points = points, Index = index };
}
