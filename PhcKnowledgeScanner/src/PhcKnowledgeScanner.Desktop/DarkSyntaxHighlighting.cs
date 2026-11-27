using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;

namespace PhcKnowledgeScanner.Desktop;

/// <summary>
/// Ajusta o syntax highlighting do AvalonEdit para fundo escuro (legível).
/// As definições embutidas (TSQL/C#) usam cores pensadas para tema claro.
/// </summary>
internal static class DarkSyntaxHighlighting
{
    private static bool _applied;

    public static void ConfigureEditor(TextEditor editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        editor.Background = BrushFrom("#101316");
        editor.Foreground = BrushFrom("#E5EAED");
        editor.LineNumbersForeground = BrushFrom("#818E96");
        editor.TextArea.SelectionBrush = new SolidColorBrush(Color.FromArgb(120, 145, 166, 177));
        editor.TextArea.SelectionForeground = BrushFrom("#101316");
        editor.TextArea.SelectionBorder = null;
        editor.Options.HighlightCurrentLine = true;
        editor.TextArea.TextView.CurrentLineBackground =
            new SolidColorBrush(Color.FromArgb(48, 32, 40, 45));
        editor.TextArea.TextView.CurrentLineBorder = null;
        editor.TextArea.TextView.LinkTextForegroundBrush = BrushFrom("#B8C8D0");
        // Scrollbars do AvalonEdit herdam o estilo da app quando possível.
        editor.ApplyTemplate();

        EnsureDarkNamedColors();
        editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#")
            ?? HighlightingManager.Instance.GetDefinition("TSQL")
            ?? HighlightingManager.Instance.GetDefinition("XML");
    }

    public static void SetLanguage(TextEditor editor, string? objectKind, string? content)
    {
        EnsureDarkNamedColors();

        var isSql = objectKind is "view" or "stored_procedure" or "trigger"
            || LooksLikeSql(content);

        editor.SyntaxHighlighting = isSql
            ? HighlightingManager.Instance.GetDefinition("TSQL")
              ?? HighlightingManager.Instance.GetDefinition("SQL")
              ?? HighlightingManager.Instance.GetDefinition("C#")
            : HighlightingManager.Instance.GetDefinition("C#")
              ?? HighlightingManager.Instance.GetDefinition("XML");
    }

    private static void EnsureDarkNamedColors()
    {
        if (_applied)
        {
            return;
        }

        foreach (var name in new[] { "C#", "TSQL", "SQL", "XML", "JavaScript", "HTML" })
        {
            var def = HighlightingManager.Instance.GetDefinition(name);
            if (def is not null)
            {
                RemapDefinition(def);
            }
        }

        _applied = true;
    }

    private static void RemapDefinition(IHighlightingDefinition definition)
    {
        foreach (var color in definition.NamedHighlightingColors)
        {
            var key = color.Name ?? string.Empty;
            color.Foreground = new SimpleHighlightingBrush(ResolveColor(key));
            color.Background = null;
            // Evitar bold excessivo que reduz legibilidade em ecrãs pequenos.
            if (key.Contains("Comment", StringComparison.OrdinalIgnoreCase))
            {
                color.FontStyle = FontStyles.Italic;
                color.FontWeight = null;
            }
            else
            {
                color.FontStyle = null;
            }
        }
    }

    private static Color ResolveColor(string name)
    {
        // Comentários: cinza-azulado claro (antes ficavam azul escuro ilegível).
        if (Contains(name, "Comment", "Documentation", "XmlDoc"))
        {
            return Rgb(0x9A, 0xB0, 0xC8);
        }

        if (Contains(name, "String", "Char", "Character", "Heredoc"))
        {
            return Rgb(0xF0, 0xC6, 0xA8);
        }

        if (Contains(name, "Number", "Digit", "Constant"))
        {
            return Rgb(0xB8, 0xE0, 0xB0);
        }

        if (Contains(name, "Keyword", "Reserved", "Visibility", "Modifier", "ControlFlow", "Statement"))
        {
            return Rgb(0x8C, 0xC4, 0xFF);
        }

        if (Contains(name, "Type", "Class", "Struct", "Interface", "Enum", "ValueType"))
        {
            return Rgb(0x6E, 0xDB, 0xC8);
        }

        if (Contains(name, "Method", "Function", "Invocation", "Reference"))
        {
            return Rgb(0xDC, 0xDC, 0xAA);
        }

        if (Contains(name, "Preprocessor", "Directive", "Attribute", "Annotation"))
        {
            return Rgb(0xD4, 0xA5, 0xE8);
        }

        if (Contains(name, "Operator", "Punctuation", "Delimiter", "Symbol"))
        {
            return Rgb(0xCB, 0xD5, 0xE1);
        }

        if (Contains(name, "Variable", "Parameter", "Identifier", "Name", "Field", "Property"))
        {
            return Rgb(0xE8, 0xEE, 0xF7);
        }

        if (Contains(name, "Error", "Warning", "Bad"))
        {
            return Rgb(0xF8, 0xA8, 0xA8);
        }

        // Fallback: texto principal claro.
        return Rgb(0xE8, 0xEE, 0xF7);
    }

    private static bool Contains(string name, params string[] tokens) =>
        tokens.Any(t => name.Contains(t, StringComparison.OrdinalIgnoreCase));

    private static bool LooksLikeSql(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        var head = content.Length > 400 ? content[..400] : content;
        return head.Contains("SELECT ", StringComparison.OrdinalIgnoreCase)
            || head.Contains("CREATE ", StringComparison.OrdinalIgnoreCase)
            || head.Contains("ALTER ", StringComparison.OrdinalIgnoreCase)
            || head.Contains("INSERT ", StringComparison.OrdinalIgnoreCase)
            || head.Contains("UPDATE ", StringComparison.OrdinalIgnoreCase)
            || head.Contains("FROM dbo.", StringComparison.OrdinalIgnoreCase);
    }

    private static Color Rgb(byte r, byte g, byte b) => Color.FromRgb(r, g, b);

    private static SolidColorBrush BrushFrom(string hex)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        brush.Freeze();
        return brush;
    }
}
