using AlertEngine.Core.Evaluation;
using AlertEngine.Core.Models;
using Xunit;

namespace AlertEngine.Tests;

/// <summary>
/// Runs the whole engine end-to-end against the actual prices.json / rules.json
/// handed out with the assignment, and checks a handful of the interesting,
/// hand-verifiable moments called out in the assignment text and visible in
/// the data (the 18:00 spike on 2026-08-15, the negative price on 2026-08-13,
/// the missing hour on 2026-08-12).
/// </summary>
public class EngineIntegrationTests
{
    private static string SamplePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "SampleData", fileName);

    private static List<Alert> RunSample()
    {
        var priceFile = JsonLoader.LoadPriceFile(SamplePath("prices.json"));
        var ruleFile = JsonLoader.LoadRuleFile(SamplePath("rules.json"));
        return new AlertEngineRunner().Run(priceFile, ruleFile).ToList();
    }

    [Fact]
    public void SpikeAt20260815T1800_TriggersThresholdAboveThreeThousand()
    {
        var alerts = RunSample();

        Assert.Contains(alerts, a =>
            a.RuleId == "price-above-3000" &&
            a.Timestamp == DateTimeOffset.Parse("2026-08-15T18:00:00+03:00"));
    }

    [Fact]
    public void SpikeAt20260815T1800_AlsoTriggersHourlyJumpAndAbnormalMarket()
    {
        // 2347.60 -> 4200.00 is roughly a +79% jump: well past the 20%
        // hourly-jump rule and the 50% abnormal-market rule.
        var alerts = RunSample();
        var at1800 = alerts.Where(a => a.Timestamp == DateTimeOffset.Parse("2026-08-15T18:00:00+03:00"))
                            .Select(a => a.RuleId)
                            .ToList();

        Assert.Contains("hourly-jump-20", at1800);
        Assert.Contains("abnormal-market", at1800);
    }

    [Fact]
    public void NegativePriceAt20260813T1400_TriggersBelowHundredAndAbnormalMarket()
    {
        var alerts = RunSample();
        var atTime = alerts.Where(a => a.Timestamp == DateTimeOffset.Parse("2026-08-13T14:00:00+03:00"))
                            .Select(a => a.RuleId)
                            .ToList();

        Assert.Contains("price-below-100", atTime);
        Assert.Contains("abnormal-market", atTime); // lt(0) branch of the "or"
        Assert.Contains("outside-normal-band", atTime); // below min 0

        // NOT "outside-comfort-zone": given the spec's own definition of "range"
        // (matches when the price LEAVES [min,max]), not(range(1200,3200)) matches
        // when the price is INSIDE [1200,3200] - the opposite of what its id/message
        // suggest. -50 is outside that band, so this rule does not fire here.
        // See README "Notes on the sample rules.json" for the full discussion.
        Assert.DoesNotContain("outside-comfort-zone", atTime);
    }

    [Fact]
    public void NoAlertFires_ForThePreviousHourBeforeTheSpike()
    {
        // 2026-08-15T17:00 = 2347.60, per the assignment's own worked example.
        var alerts = RunSample();

        Assert.DoesNotContain(alerts, a =>
            a.Timestamp == DateTimeOffset.Parse("2026-08-15T17:00:00+03:00") &&
            a.RuleId == "price-above-3000");
    }

    [Fact]
    public void MissingHourOn20260812_DoesNotCrashAndSkipsChangeRuleAcrossTheGap()
    {
        // 2026-08-12 is missing the 03:00 row (02:00 -> 04:00 directly).
        // The change rule must not compare 04:00 against 02:00 as if they
        // were adjacent hours.
        var alerts = RunSample();

        Assert.DoesNotContain(alerts, a =>
            a.Timestamp == DateTimeOffset.Parse("2026-08-12T04:00:00+03:00") &&
            a.RuleId == "hourly-jump-20");
    }

    [Fact]
    public void CooldownRule_NeverFiresMoreOftenThanItsWindow()
    {
        var alerts = RunSample()
            .Where(a => a.RuleId == "spike-alarm-with-cooldown")
            .OrderBy(a => a.Timestamp)
            .ToList();

        for (int i = 1; i < alerts.Count; i++)
        {
            var gap = alerts[i].Timestamp - alerts[i - 1].Timestamp;
            Assert.True(gap >= TimeSpan.FromHours(6),
                $"Cooldown fired twice within {gap} at {alerts[i].Timestamp}");
        }
    }

    [Fact]
    public void EngineProducesSomeAlerts_OverTheWholeWeek()
    {
        // Sanity check: this dataset was designed to exercise every rule at
        // least once, so the engine should not come back empty.
        var alerts = RunSample();
        Assert.NotEmpty(alerts);
    }
}
