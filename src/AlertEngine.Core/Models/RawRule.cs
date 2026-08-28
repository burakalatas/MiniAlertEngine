namespace AlertEngine.Core.Models;

/// <summary>
/// Loose, "one shape fits all" representation of a rule as it appears in rules.json.
/// Every rule type only uses a subset of these fields; unused fields stay null.
/// This is deliberately a plain data holder (deserialization target) - the actual
/// rule *behaviour* lives in the AlertEngine.Core.Rules classes, built from this
/// via RuleFactory. Id/Message are only meaningful (and only required) on the
/// top-level entries of rules.json; nested rules never carry them.
/// </summary>
public sealed class RawRule
{
    public string? Id { get; set; }
    public string Type { get; set; } = "";
    public string? Message { get; set; }

    // threshold
    public string? Operator { get; set; }
    public double? Value { get; set; }

    // change
    public double? Percent { get; set; }

    // range
    public double? Min { get; set; }
    public double? Max { get; set; }

    // streak
    public string? Direction { get; set; }
    public int? Hours { get; set; }

    // and / or
    public List<RawRule>? Rules { get; set; }

    // not / cooldown
    public RawRule? Rule { get; set; }
}
