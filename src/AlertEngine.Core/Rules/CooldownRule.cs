namespace AlertEngine.Core.Rules;

/// <summary>
/// Wraps another rule and limits how often it may actually fire: even if the
/// inner rule matches every hour, this only reports true at most once per
/// "hours" window.
///
/// This rule is inherently stateful (it remembers when it last fired), so
/// unlike the stateless rules it must be evaluated exactly once per hour, in
/// chronological order - re-evaluating the same hour twice, or evaluating
/// out of order, would corrupt the cooldown window. AlertEngineRunner
/// guarantees this by sorting the price file once up front and then walking
/// it strictly forward.
/// </summary>
public sealed class CooldownRule : IRule
{
    private readonly int _hours;
    private readonly IRule _inner;
    private DateTimeOffset? _lastFiredAt;

    public CooldownRule(int hours, IRule inner)
    {
        if (hours <= 0)
            throw new ArgumentOutOfRangeException(nameof(hours), "Cooldown length must be positive.");
        _hours = hours;
        _inner = inner;
    }

    public bool Evaluate(EvaluationContext ctx)
    {
        if (!_inner.Evaluate(ctx)) return false;

        var now = ctx.Current.Timestamp;
        if (_lastFiredAt is not null && now - _lastFiredAt.Value < TimeSpan.FromHours(_hours))
            return false;

        _lastFiredAt = now;
        return true;
    }
}
