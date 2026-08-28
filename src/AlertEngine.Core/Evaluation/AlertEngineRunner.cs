using AlertEngine.Core.Models;
using AlertEngine.Core.Rules;

namespace AlertEngine.Core.Evaluation;

/// <summary>
/// Ties everything together: walks the price series hour by hour, in
/// chronological order, and checks every compiled top-level rule at every
/// hour, yielding one Alert per match.
///
/// Chronological order matters for correctness, not just presentation:
/// ChangeRule/StreakRule look at "the previous hour" and CooldownRule keeps
/// a running "last fired" timestamp, so the price list is sorted once up
/// front rather than trusting the file to already be in order.
/// </summary>
public sealed class AlertEngineRunner
{
    public IEnumerable<Alert> Run(PriceFile priceFile, RuleFile ruleFile)
    {
        var points = priceFile.Prices
            .OrderBy(p => p.Timestamp)
            .ToList();

        // Built once, up front: rule instances (specifically CooldownRule)
        // hold state across hours, so we must reuse the same instances for
        // every hour rather than rebuilding the tree each time.
        var compiledRules = ruleFile.Rules
            .Select(raw => new CompiledRule(
                Id: raw.Id ?? throw new InvalidDataException("Top-level rule is missing 'id'."),
                Message: raw.Message ?? throw new InvalidDataException($"Rule '{raw.Id}' is missing 'message'."),
                Logic: RuleFactory.Build(raw)))
            .ToList();

        for (int i = 0; i < points.Count; i++)
        {
            var ctx = new EvaluationContext { Points = points, Index = i };

            foreach (var rule in compiledRules)
            {
                if (rule.Logic.Evaluate(ctx))
                {
                    yield return new Alert(points[i].Timestamp, rule.Id, rule.Message, points[i].Price);
                }
            }
        }
    }
}
