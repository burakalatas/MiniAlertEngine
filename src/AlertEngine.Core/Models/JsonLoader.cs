using System.Text.Json;

namespace AlertEngine.Core.Models;

/// <summary>
/// Small helper for reading the two input files. Kept separate from the
/// domain models so the models themselves stay plain POCOs.
/// </summary>
public static class JsonLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static PriceFile LoadPriceFile(string path)
    {
        var json = File.ReadAllText(path);
        var file = JsonSerializer.Deserialize<PriceFile>(json, Options);
        if (file is null)
            throw new InvalidDataException($"Could not parse price file: {path}");
        return file;
    }

    public static RuleFile LoadRuleFile(string path)
    {
        var json = File.ReadAllText(path);
        var file = JsonSerializer.Deserialize<RuleFile>(json, Options);
        if (file is null)
            throw new InvalidDataException($"Could not parse rule file: {path}");
        return file;
    }
}
