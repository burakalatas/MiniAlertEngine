# Mini Alert Engine

A small console program that walks a week of hourly electricity prices and
reports every hour where a configured rule matches.

```
[2026-08-15T18:00:00+03:00] price-above-3000: Price exceeded 3000 TRY/MWh. (price: 4200.00)
```

## Project layout

```
MiniAlertEngine.sln
src/
  AlertEngine.Core/            <- the engine, as a class library (no console/IO logic here)
    Models/                    <- PriceFile, PricePoint, RawRule, RuleFile, JSON loading
    Rules/                     <- IRule + one class per rule type + RuleFactory
    Evaluation/                <- Alert, CompiledRule, AlertEngineRunner (the main loop)
  AlertEngine.Console/         <- thin console entry point (Program.cs)
tests/
  AlertEngine.Tests/           <- xUnit tests, including an end-to-end test against
                                  the actual sample prices.json / rules.json
data/
  prices.json, rules.json      <- copies of the files handed out with the assignment,
                                  for convenience when running the console app
```

### Why this shape

- **Core has zero dependency on Console or file paths.** `AlertEngineRunner.Run(PriceFile, RuleFile)`
  just takes already-parsed objects and yields `Alert`s. That's what makes it trivial to unit-test
  and to reuse (e.g. behind a web API later) without touching engine code.
- **Every rule type is one small class implementing `IRule { bool Evaluate(EvaluationContext) }`.**
  `and`/`or`/`not`/`cooldown` hold child `IRule`s and just call back into them, which is what lets
  rules nest to arbitrary depth for free (`RuleFactory.Build` recurses on the same JSON shape).
- **`RawRule` is a deliberately "kitchen sink" DTO** with every field any rule type might use,
  all nullable. It's not a domain type - it's purely what JSON deserialization produces.
  `RuleFactory.Build` is the one place that turns "loose JSON shape" into "actual behaviour",
  and it's where a missing required field for a given type throws a clear error instead of
  silently doing the wrong thing.

## Build, run, test

Requires .NET 8 SDK.

```bash
dotnet build

# run against the sample data
dotnet run --project src/AlertEngine.Console -- data/prices.json data/rules.json

# run the tests
dotnet test
```

> **Note on verification:** this was originally written in a sandbox without NuGet/internet
> access, so it wasn't compiled there. After a real `dotnet test` run, the engine code
> compiled and ran correctly, but it surfaced 3 test bugs of my own: `Not_InvertsInnerRule`
> and two nesting tests had asserted the opposite of what `not(range(...))` actually does
> (see "A quirk I noticed in the sample rules.json" below - I'd apparently made the same
> mental slip while writing those specific tests that I flagged as a risk in the sample data).
> The engine logic itself needed no changes; only those three tests' expectations/rule
> shapes were fixed. All 42 tests pass now.

## Decisions on the deliberately-undefined cases

The assignment calls out that some behaviour is intentionally left open. Here's what I chose
and why - happy to defend or change any of these in the interview.

**`change` rule on the very first hour of the file.** There's no previous price, so the rule
simply does not match. Treating "no data" as "no alert" felt safer than treating it as a match
or throwing.

**`change` (and `streak`) across a missing hour.** The sample data has a real gap:
`2026-08-12` jumps straight from `02:00` to `04:00`, skipping `03:00`. If I compared `04:00`
against `02:00` as if they were one hour apart, a rule like `hourly-jump-20` would report a
"percent per hour" figure that's actually a two-hour move - technically correct data, wrong
story. So both `ChangeRule` and `StreakRule` require the previous point(s) to be *exactly* one
hour earlier; if there's a gap, they simply don't match for that hour, the same as if there
were no history at all. This is implemented once, in `EvaluationContext.PreviousHourOrNull()`
and `EvaluationContext.LastConsecutiveHours()`, so both rules share the same gap-handling logic.

**`change` when the previous price is exactly 0.** Percentage change from zero is undefined.
I chose: any non-zero current price counts as a match (a clear, large move away from zero),
and a `0 -> 0` step does not match. This doesn't come up in the sample data but felt like the
kind of edge case worth deciding explicitly rather than letting a `DivideByZeroException`
decide it for me.

**`range` boundaries.** A price exactly equal to `min` or `max` counts as *inside* the band
(only strictly-less-than / strictly-greater-than counts as "left the band"). Arbitrary but
consistent, and matches how the `threshold` rule's `gt`/`lt` are also strict.

**`streak` and flat prices.** Two equal consecutive prices count as neither an "up" move nor
a "down" move, so a flat step breaks a streak in either direction.

**Missing hour in the injected data (`2026-08-12T03:00`).** Left as-is; the engine iterates
whatever hours are actually present rather than trying to synthesize the missing one. Rules
that need history (`change`, `streak`) simply treat the surrounding hours as having no valid
"previous" relationship, per above.

**The negative price (`2026-08-13T14:00`, `-50.00`) and the spike (`2026-08-15T18:00`,
`4200.00`).** These read like intentionally planted anomalies to exercise the rules
(`price-below-100`, `outside-normal-band`, `abnormal-market`, `price-above-3000`,
`hourly-jump-20` all fire on one or the other). No special-casing needed - they just flow
through the normal rule logic, which is exactly the point of having generic rules rather than
hardcoded checks. There's an integration test pinned to both moments.

### A quirk I noticed in the sample `rules.json`

`outside-comfort-zone` is defined as:

```json
{
  "id": "outside-comfort-zone",
  "type": "not",
  "message": "Price is outside the comfortable trading zone.",
  "rule": { "type": "range", "min": 1200, "max": 3200 }
}
```

Per the assignment's own definition, `range` matches **when the price leaves** `[min, max]`
(see `outside-normal-band` for an unambiguous example of that). So `not(range(1200,3200))`
matches when the price does *not* leave that band - i.e. when it's **inside** `[1200, 3200]`.
That's the opposite of what the id and message say.

I implemented `range` and `not` exactly per spec, consistently, regardless of where they're
nested (I didn't special-case this one rule to make its name line up, since that would make
`range`'s meaning depend on context, which seemed worse). So as written, `outside-comfort-zone`
currently fires on *comfortable* prices, not uncomfortable ones. I'm flagging it here rather
than silently "fixing" the sample file's intent, since I wasn't sure which was the actual bug:
the rule shape (should be `and`/`or` of two thresholds, or `range` without the `not`) or the
message text. There's a test (`NegativePriceAt20260813T1400_...`) that documents and locks in
the current, literal behaviour.

## Section 3 (optional): both memory-based rule types implemented

Both `streak` and `cooldown` are implemented (the assignment only required one).

- `StreakRule` is stateless - it recomputes the last N hours from `Points`/`Index` on every
  call, so it doesn't care how many times or in what order it's asked (as long as the data
  itself is time-ordered).
- `CooldownRule` is the one genuinely stateful rule: it remembers the timestamp it last fired
  at, on the rule instance itself. This only produces correct results if the engine evaluates
  each hour exactly once, in ascending time order - which `AlertEngineRunner` guarantees by
  sorting the price list once up front before looping.

## Section 4 (optional): written questions

### 1. What would you change if this took 10,000 price updates/second, across many products?

The current design evaluates every rule, for one product, over a batch of history it already
holds entirely in memory - fine for "one file, one run", not fine for a continuous,
multi-product stream. Three things I'd change, roughly in priority order:

- **Shard by product, and make each shard's state independent and tiny.** Right now the only
  state that crosses hours is `CooldownRule`'s "last fired" timestamp (and `StreakRule`'s
  implicit lookback window). At 10k updates/sec I would not keep full price history around -
  I'd turn every rule into an explicit, small piece of state per (product, rule) pair: the last
  price and timestamp for `change`, the current run-length and direction for `streak`, the last
  fired time for `cooldown`. Then processing an update is O(number of rules) with no history
  scan, and products are trivially parallelizable (they share nothing).
- **Move off "in-memory list + LINQ" and onto a streaming model** (e.g. one lightweight
  worker/queue per product, or a partitioned stream like Kafka keyed by product id) so work
  distributes across cores/machines instead of one process holding everything.
- **Separate "detect" from "notify".** At this volume you don't want every match doing
  synchronous console I/O; alerts should go on a queue/log and be processed asynchronously,
  with backpressure so a slow downstream (e.g. cooldown lookups, if they ever needed shared
  storage) doesn't stall ingestion.

What I would *not* rush to change: the actual rule-matching logic (threshold/range/and/or/not
comparisons) is already O(1) per rule per update - that was never the bottleneck, the shared
mutable state and I/O model would be.

### 2. Adding a brand-new rule type via configuration only, no code changes - how, and what's the downside?

The one part of this design that *isn't* purely data-driven is `RuleFactory.Build`'s
`switch` on `Type` - that's a hardcoded, closed set of rule kinds. To add new types purely
through configuration you'd need to replace that switch with something that interprets a
small, generic expression language *as data*: e.g. let a rule's config be an arbitrary
boolean expression tree over a fixed set of primitives (`price`, `previous_price`, arithmetic,
comparisons, `and`/`or`/`not`), so a "new rule type" is really just a new named, saved
expression rather than a new code path. Concretely: ship a tiny interpreter for a JSON-encoded
expression AST once, and after that, "add a rule type" means "add a JSON document", no
redeploy.

The downsides are real, though, and they're why I didn't build the whole engine this way for
a 4-hour take-home:

- **You lose type safety and compile-time checking.** A typo in a field name becomes a
  runtime surprise in production instead of a build error.
- **Stateful rules (`streak`, `cooldown`) don't fit a pure "evaluate this expression against
  current data" model** - they need somewhere to keep memory between calls, and somebody has
  to define, generically, what "state" a config-defined rule type is allowed to have and how
  it's keyed/persisted. That's a much bigger design surface than the stateless rules.
- **Debuggability and testability get harder.** Right now every rule type is a named class you
  can unit test in isolation with a clear name in a stack trace. A generic expression engine
  pushes correctness onto whoever wrote the JSON, with worse error messages when it's wrong.
- **Security/complexity creep.** A sufficiently expressive config language starts to look like
  a tiny programming language that non-developers are now allowed to write, which brings its
  own review and validation burden.

I'd probably land on a middle ground in practice: keep a fixed, well-tested set of *primitive*
rule types (which is what this assignment already is) and only make the *combination* layer
(`and`/`or`/`not`, and maybe simple arithmetic on `price/previous_price`) generic and
config-driven, since that's where genuinely new "shapes" of rules tend to come from without
needing new code.

## What I'd do next with more time

- Add a `--format json` / `--format ndjson` output mode to `AlertEngine.Console`, so the
  output can feed a downstream system instead of only being human-readable.
- Property-based tests (e.g. FsCheck) for the combinators (`and`/`or`/`not`) against randomly
  generated rule trees and price sequences, to get more confidence in arbitrary nesting depth
  than the handful of examples in `CombinatorTests.cs` give.
- A `--rules-check` mode that loads and compiles rules.json without any price data, purely to
  validate the file (unknown types, missing fields, unresolved nesting) before a real run.
- Decide and fix (or explicitly confirm) the `outside-comfort-zone` question above with
  whoever owns `rules.json`.
