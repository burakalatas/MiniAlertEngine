using AlertEngine.Core.Evaluation;
using AlertEngine.Core.Models;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: alert-engine <prices.json> <rules.json>");
    return 1;
}

var pricesPath = args[0];
var rulesPath = args[1];

try
{
    var priceFile = JsonLoader.LoadPriceFile(pricesPath);
    var ruleFile = JsonLoader.LoadRuleFile(rulesPath);

    var runner = new AlertEngineRunner();
    foreach (var alert in runner.Run(priceFile, ruleFile))
    {
        Console.WriteLine(alert.ToString());
    }

    return 0;
}
catch (Exception ex) when (ex is IOException or InvalidDataException or NotSupportedException)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}
