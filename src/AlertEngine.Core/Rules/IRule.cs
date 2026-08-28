namespace AlertEngine.Core.Rules;

/// <summary>
/// A single, composable piece of matching logic. Every rule type (threshold,
/// change, range, and, or, not, streak, cooldown) implements this the same
/// way, which is what lets them nest inside one another to arbitrary depth.
///
/// Note: some implementations (StreakRule needs none, CooldownRule needs a
/// "last fired" timestamp) hold mutable state. That state lives on the rule
/// instance itself and assumes Evaluate is called once per hour, in
/// increasing timestamp order - see AlertEngineRunner.
/// </summary>
public interface IRule
{
    bool Evaluate(EvaluationContext ctx);
}
