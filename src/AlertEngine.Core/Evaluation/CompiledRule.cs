using AlertEngine.Core.Rules;

namespace AlertEngine.Core.Evaluation;

/// <summary>
/// A top-level rule from rules.json after being compiled into an IRule tree.
/// Id and Message only exist at this top level - nested rules (inside and/or/
/// not/cooldown) never get their own alert line, only the outermost named
/// rule does.
/// </summary>
public sealed record CompiledRule(string Id, string Message, IRule Logic);
