namespace AlertEngine.Core.Models;

/// <summary>
/// Root object of the rules JSON file.
/// </summary>
public sealed class RuleFile
{
    public List<RawRule> Rules { get; init; } = new();
}
