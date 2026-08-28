namespace AlertEngine.Core.Rules;

/// <summary>
/// Matches when every child rule matches at the current hour.
/// </summary>
public sealed class AndRule : IRule
{
    private readonly IReadOnlyList<IRule> _rules;

    public AndRule(IReadOnlyList<IRule> rules)
    {
        if (rules.Count == 0)
            throw new ArgumentException("An 'and' rule needs at least one child rule.", nameof(rules));
        _rules = rules;
    }

    public bool Evaluate(EvaluationContext ctx) => _rules.All(r => r.Evaluate(ctx));
}

/// <summary>
/// Matches when at least one child rule matches at the current hour.
/// </summary>
public sealed class OrRule : IRule
{
    private readonly IReadOnlyList<IRule> _rules;

    public OrRule(IReadOnlyList<IRule> rules)
    {
        if (rules.Count == 0)
            throw new ArgumentException("An 'or' rule needs at least one child rule.", nameof(rules));
        _rules = rules;
    }

    public bool Evaluate(EvaluationContext ctx) => _rules.Any(r => r.Evaluate(ctx));
}

/// <summary>
/// Matches when its single child rule does NOT match.
/// </summary>
public sealed class NotRule : IRule
{
    private readonly IRule _rule;

    public NotRule(IRule rule)
    {
        _rule = rule;
    }

    public bool Evaluate(EvaluationContext ctx) => !_rule.Evaluate(ctx);
}
