namespace AlertEngine.Core.Models;

/// <summary>
/// A single hourly price observation.
/// </summary>
public sealed class PricePoint
{
    public DateTimeOffset Timestamp { get; init; }
    public double Price { get; init; }

    public override string ToString() => $"{Timestamp:yyyy-MM-ddTHH:mm:sszzz} -> {Price}";
}
