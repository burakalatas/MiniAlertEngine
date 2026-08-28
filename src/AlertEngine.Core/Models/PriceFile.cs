namespace AlertEngine.Core.Models;

/// <summary>
/// Root object of the price JSON file.
/// </summary>
public sealed class PriceFile
{
    public string Currency { get; init; } = "";
    public string Timezone { get; init; } = "";
    public List<PricePoint> Prices { get; init; } = new();
}
