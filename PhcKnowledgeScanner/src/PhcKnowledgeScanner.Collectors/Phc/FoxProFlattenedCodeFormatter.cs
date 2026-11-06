using System.Text;
using System.Text.RegularExpressions;

namespace PhcKnowledgeScanner.Collectors.Phc;

/// <summary>Pretty-print best-effort para Xexpressao gravado sem CR/LF.</summary>
public static class FoxProFlattenedCodeFormatter
{
    private static readonly Regex StatementStartRegex = new(
        """
        (?<![\w.])(?i:(?:
            function|procedure|endfunc|endproc|
            if|else|elif|endif|
            do\s+while|enddo|
            for|endfor|next|
            scan|endscan|
            do\s+case|case|otherwise|endcase|
            text|endtext|
            return|exit|loop|
            select|go\s+top|go\s+bottom|
            replace|append|
            msg|pergunta|docomando|
            u_sqlexec|u_sendmailhtml|idutopdf|
            release|
            endwith
        ))(?![\w])
        """,
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

    private static readonly HashSet<string> KeepAfterIfElse = new(StringComparer.OrdinalIgnoreCase)
    {
        "msg", "pergunta", "docomando", "u_sqlexec", "u_sendmailhtml", "idutopdf"
    };

    public static string Format(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var code = StripWrappers(raw);
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        if (code.IndexOfAny(['\r', '\n']) >= 0)
        {
            return NormalizeNewlines(code).Trim() + "\n";
        }

        var mask = BuildStringMask(code);
        var breaks = new SortedSet<int>();
        foreach (Match m in StatementStartRegex.Matches(code))
        {
            if (m.Index == 0 || mask[m.Index])
            {
                continue;
            }

            var keyword = m.Value.Trim();
            if (KeepAfterIfElse.Contains(keyword) && PrecededByIfOrElseOnSameLine(code, m.Index, mask))
            {
                continue;
            }

            breaks.Add(m.Index);
        }

        var sb = new StringBuilder(code.Length + breaks.Count * 2);
        for (var i = 0; i < code.Length; i++)
        {
            if (breaks.Contains(i) && sb.Length > 0 && sb[^1] != '\n')
            {
                while (sb.Length > 0 && sb[^1] is ' ' or '\t')
                {
                    sb.Length--;
                }

                sb.Append('\n');
            }

            if (mask[i])
            {
                sb.Append(code[i]);
                continue;
            }

            if (code[i] is ' ' or '\t')
            {
                if (sb.Length == 0 || sb[^1] is ' ' or '\n')
                {
                    continue;
                }

                sb.Append(' ');
                continue;
            }

            sb.Append(code[i]);
        }

        var result = sb.ToString();
        result = Regex.Replace(result, @"[ \t]+\r?\n", "\n");
        result = Regex.Replace(result, @"\n{3,}", "\n\n");
        return result.Trim() + "\n";
    }

    private static bool PrecededByIfOrElseOnSameLine(string code, int index, bool[] mask)
    {
        var i = index - 1;
        while (i >= 0 && !mask[i] && code[i] is ' ' or '\t')
        {
            i--;
        }

        if (i < 0 || mask[i])
        {
            return false;
        }

        var end = i;
        while (i >= 0 && !mask[i] && (char.IsLetterOrDigit(code[i]) || code[i] == '_'))
        {
            i--;
        }

        var token = code[(i + 1)..(end + 1)];
        return token.Equals("if", StringComparison.OrdinalIgnoreCase)
               || token.Equals("else", StringComparison.OrdinalIgnoreCase)
               || token.Equals("elif", StringComparison.OrdinalIgnoreCase)
               || token.Equals("elseif", StringComparison.OrdinalIgnoreCase);
    }

    private static string StripWrappers(string raw)
    {
        var code = raw.Trim();
        code = code.Replace("<«LARGEPROP_INI»>", "", StringComparison.OrdinalIgnoreCase)
            .Replace("<«LARGEPROP_FIM»>", "", StringComparison.OrdinalIgnoreCase)
            .Replace("<LARGEPROP_INI>", "", StringComparison.OrdinalIgnoreCase)
            .Replace("<LARGEPROP_FIM>", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
        if (code.Length >= 2 && code[0] == '"' && code[^1] == '"')
        {
            code = code[1..^1];
        }

        return code.Trim();
    }

    private static string NormalizeNewlines(string code) =>
        code.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static bool[] BuildStringMask(string code)
    {
        var mask = new bool[code.Length];
        var i = 0;
        while (i < code.Length)
        {
            if (code[i] is not ('"' or '\''))
            {
                i++;
                continue;
            }

            var q = code[i];
            mask[i++] = true;
            while (i < code.Length)
            {
                mask[i] = true;
                var ch = code[i++];
                if (ch != q)
                {
                    continue;
                }

                if (i < code.Length && code[i] == q)
                {
                    mask[i++] = true;
                    continue;
                }

                break;
            }
        }

        return mask;
    }
}
