using System.Text.RegularExpressions;

namespace PhcKnowledgeScanner.Collectors.Phc;

/// <summary>Parse formc.prop — DOC-12 grelhas ocultas (udcVisible + Xexpressao).</summary>
public static partial class FormcPropParser
{
    [GeneratedRegex(
        @"udcVisible\s*=\s*(?<v>\.F\.|\.T\.|\.f\.|\.t\.|0|1)(?!\w)",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex UdcVisibleRegex();

    [GeneratedRegex(
        @"Xexpressao\s*(?<code>.*?)\s*Xexpressao",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex XexpressaoRegex();

    public static FormcPropParseResult Parse(string? prop)
    {
        if (string.IsNullOrEmpty(prop))
        {
            return FormcPropParseResult.Empty;
        }

        return new FormcPropParseResult(TryParseUdcVisible(prop), TryExtractXexpressao(prop));
    }

    public static bool? TryParseUdcVisible(string prop)
    {
        var match = UdcVisibleRegex().Match(prop);
        if (!match.Success)
        {
            return null;
        }

        var v = match.Groups["v"].Value;
        if (v.Equals(".F.", StringComparison.OrdinalIgnoreCase) || v.Equals("0", StringComparison.Ordinal))
        {
            return false;
        }

        if (v.Equals(".T.", StringComparison.OrdinalIgnoreCase) || v.Equals("1", StringComparison.Ordinal))
        {
            return true;
        }

        return null;
    }

    public static string? TryExtractXexpressao(string prop)
    {
        var match = XexpressaoRegex().Match(prop);
        if (!match.Success)
        {
            return null;
        }

        var code = match.Groups["code"].Value;
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        code = StripLargePropNoise(code.Trim());
        if (code.Length >= 2 && code[0] == '"' && code[^1] == '"')
        {
            code = code[1..^1];
        }

        return string.IsNullOrWhiteSpace(code) ? null : code;
    }

    private static string StripLargePropNoise(string code) =>
        code.Replace("<«LARGEPROP_INI»>", "", StringComparison.OrdinalIgnoreCase)
            .Replace("<«LARGEPROP_FIM»>", "", StringComparison.OrdinalIgnoreCase)
            .Replace("<LARGEPROP_INI>", "", StringComparison.OrdinalIgnoreCase)
            .Replace("<LARGEPROP_FIM>", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
}

public readonly record struct FormcPropParseResult(bool? UdcVisible, string? Xexpressao)
{
    public static FormcPropParseResult Empty { get; } = new(null, null);

    public bool IsHiddenWithCode => UdcVisible == false && !string.IsNullOrWhiteSpace(Xexpressao);
}
