namespace AlertEngine.Core.Evaluation;

/// <summary>
/// One matched rule at one hour - exactly one line of console output.
/// </summary>
public sealed record Alert(DateTimeOffset Timestamp, string RuleId, string Message, double Price)
{
    public override string ToString() =>
        $"[{Timestamp:yyyy-MM-ddTHH:mm:sszzz}] {RuleId}: {Message} (price: {Price:F2})";
}
