using AlertEngine.Core.Models;

namespace AlertEngine.Core.Rules;

/// <summary>
/// Turns the JSON-shaped RawRule tree into an actual, evaluable IRule tree.
/// Works recursively so and/or/not/cooldown can nest to any depth - each one
/// just calls back into Build for its children.
/// </summary>
public static class RuleFactory
{
    public static IRule Build(RawRule raw)
    {
        return raw.Type switch
        {
            "threshold" => new ThresholdRule(
                Require(raw.Operator, raw, "operator"),
                Require(raw.Value, raw, "value")),

            "change" => new ChangeRule(
                Require(raw.Percent, raw, "percent")),

            "range" => new RangeRule(
                Require(raw.Min, raw, "min"),
                Require(raw.Max, raw, "max")),

            "and" => new AndRule(
                Require(raw.Rules, raw, "rules").Select(Build).ToList()),

            "or" => new OrRule(
                Require(raw.Rules, raw, "rules").Select(Build).ToList()),

            "not" => new NotRule(
                Build(Require(raw.Rule, raw, "rule"))),

            "streak" => new StreakRule(
                Require(raw.Direction, raw, "direction"),
                Require(raw.Hours, raw, "hours")),

            "cooldown" => new CooldownRule(
                Require(raw.Hours, raw, "hours"),
                Build(Require(raw.Rule, raw, "rule"))),

            _ => throw new NotSupportedException($"Unknown rule type '{raw.Type}'.")
        };
    }

    private static T Require<T>(T? value, RawRule raw, string fieldName) where T : class
    {
        if (value is null)
            throw new InvalidDataException($"Rule of type '{raw.Type}' (id: {raw.Id ?? "<nested>"}) is missing required field '{fieldName}'.");
        return value;
    }

    private static T Require<T>(T? value, RawRule raw, string fieldName) where T : struct
    {
        if (value is null)
            throw new InvalidDataException($"Rule of type '{raw.Type}' (id: {raw.Id ?? "<nested>"}) is missing required field '{fieldName}'.");
        return value.Value;
    }
}
