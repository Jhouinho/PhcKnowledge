using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Workspace;

namespace PhcKnowledgeScanner.Desktop;

internal static class DashboardPresenter
{
    private static readonly Brush PanelInner = Brush("#0F1218");
    private static readonly Brush Accent = Brush("#E8A838");
    private static readonly Brush AccentSoft = Brush("#B8842A");
    private static readonly Brush Ok = Brush("#5ECF8E");
    private static readonly Brush Warn = Brush("#E8B86D");
    private static readonly Brush Danger = Brush("#E86A6A");
    private static readonly Brush Muted = Brush("#8B93A7");
    private static readonly Brush Text = Brush("#F1F3F7");
    private static readonly Brush BarTrack = Brush("#242B36");

    private static readonly HashSet<string> PersonalizationKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        ObjectKind.PhcEvent,
        ObjectKind.PhcWebEvent,
        ObjectKind.PhcUserRule,
        ObjectKind.PhcWebUserRule,
        ObjectKind.PhcDefaultValue,
        ObjectKind.PhcWebDefaultValue,
        ObjectKind.PhcUserField,
        ObjectKind.PhcHotkey,
        ObjectKind.PhcWebScript,
        ObjectKind.PhcUserJavascript,
        ObjectKind.PhcWebCssJs,
        ObjectKind.PhcHiddenGrid,
        ObjectKind.PhcCustomScreen,
        ObjectKind.PhcUserTsqlFunction
    };

    private static readonly HashSet<string> StructureKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        ObjectKind.Table,
        ObjectKind.PrimaryKey,
        ObjectKind.ForeignKey,
        ObjectKind.Trigger,
        ObjectKind.View,
        ObjectKind.StoredProcedure
    };

    public static void RenderEmpty(
        TextBlock installation,
        TextBlock status,
        TextBlock badge,
        Border badgeBorder,
        Panel kpi,
        Panel chartActivity,
        Panel chartPersonalizations,
        Panel chartStructure,
        TextBlock issuesSummary,
        ItemsControl issues)
    {
        installation.Text = "Sem recolha ativa";
        status.Text = "Ligue-se a uma instalação e execute a recolha para ver o resumo.";
        badge.Text = "Aguardando";
        badge.Foreground = Muted;
        badgeBorder.Background = Brush("#1E293B");
        kpi.Children.Clear();
        chartActivity.Children.Clear();
        chartPersonalizations.Children.Clear();
        chartStructure.Children.Clear();
        chartActivity.Children.Add(MutedNote("Sem dados."));
        chartPersonalizations.Children.Add(MutedNote("Sem dados."));
        chartStructure.Children.Add(MutedNote("Sem dados."));
        issuesSummary.Text = "";
        issues.ItemsSource = null;
    }

    public static void Render(
        WorkspaceSummary summary,
        IReadOnlyList<string> issueLines,
        IReadOnlyList<TechnicalObjectRecord> objects,
        TextBlock installation,
        TextBlock status,
        TextBlock badge,
        Border badgeBorder,
        Panel kpi,
        Panel chartActivity,
        Panel chartPersonalizations,
        Panel chartStructure,
        TextBlock issuesSummary,
        ItemsControl issues,
        Func<string, string> friendlyKind)
    {
        installation.Text = summary.InstallationLabel;
        status.Text =
            $"Scan {summary.ScanId[..Math.Min(8, summary.ScanId.Length)]}… · " +
            $"Estado: {summary.Status} · Criado: {summary.CreatedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm}";

        var (badgeText, badgeFg, badgeBg) = StatusStyle(summary);
        badge.Text = badgeText;
        badge.Foreground = badgeFg;
        badgeBorder.Background = badgeBg;

        var counts = summary.ObjectCounts;
        var total = counts.Values.Sum();
        var personalizationTotal = counts.Where(kv => PersonalizationKinds.Contains(kv.Key)).Sum(kv => kv.Value);
        var structureTotal = counts.Where(kv => StructureKinds.Contains(kv.Key)).Sum(kv => kv.Value);
        var tables = CountOf(counts, ObjectKind.Table);
        var userFields = CountOf(counts, ObjectKind.PhcUserField);
        var events = CountOf(counts, ObjectKind.PhcEvent) + CountOf(counts, ObjectKind.PhcWebEvent);
        var rules = CountOf(counts, ObjectKind.PhcUserRule) + CountOf(counts, ObjectKind.PhcWebUserRule);

        kpi.Children.Clear();
        kpi.Children.Add(KpiCard("Objetos", total.ToString("N0"), "Inventário total", Accent));
        kpi.Children.Add(KpiCard("Personalizações", personalizationTotal.ToString("N0"), "Código / config PHC", AccentSoft));
        kpi.Children.Add(KpiCard("Tabelas u_*", tables.ToString("N0"), "Só custom (não PHC standard)", Ok));
        kpi.Children.Add(KpiCard("Campos u_*", userFields.ToString("N0"), "Colunas de utilizador", Brush("#A78BFA")));
        kpi.Children.Add(KpiCard("Eventos + regras", (events + rules).ToString("N0"), $"{events} eventos · {rules} regras", Warn));
        kpi.Children.Add(KpiCard(
            "Avisos / erros",
            $"{summary.WarningCount} / {summary.ErrorCount}",
            summary.ErrorCount > 0 ? "Há falhas na recolha" : "Recolha sem erros",
            summary.ErrorCount > 0 ? Danger : summary.WarningCount > 0 ? Warn : Ok));

        RenderActivityChart(chartActivity, objects, friendlyKind);

        RenderBarChart(
            chartPersonalizations,
            PersonalizationKinds
                .Select(k => (Label: friendlyKind(k), Count: CountOf(counts, k)))
                .Where(x => x.Count > 0)
                .OrderByDescending(x => x.Count)
                .ToList(),
            Accent);

        RenderBarChart(
            chartStructure,
            StructureKinds
                .Select(k => (Label: friendlyKind(k), Count: CountOf(counts, k)))
                .Where(x => x.Count > 0)
                .OrderByDescending(x => x.Count)
                .ToList(),
            Ok);

        issuesSummary.Text = issueLines.Count == 0
            ? "Nenhum aviso"
            : $"{issueLines.Count} item(ns) · {summary.WarningCount} warning · {summary.ErrorCount} error";
        issues.ItemsSource = issueLines.Count == 0
            ? new[] { "Sem avisos ou falhas nesta recolha." }
            : issueLines;
    }

    private static void RenderActivityChart(
        Panel host,
        IReadOnlyList<TechnicalObjectRecord> objects,
        Func<string, string> friendlyKind)
    {
        host.Children.Clear();
        var rows = objects
            .Where(o => PersonalizationKinds.Contains(o.Kind))
            .Select(o => (o.Kind, Estado: TryEstado(o.SummaryJson)))
            .Where(x => x.Estado is not null)
            .GroupBy(x => x.Kind)
            .Select(g =>
            {
                var activo = g.Count(x => x.Estado == "Activo");
                var inactivo = g.Count(x => x.Estado == "Inactivo");
                return (Kind: g.Key, Activo: activo, Inactivo: inactivo, Total: activo + inactivo);
            })
            .Where(x => x.Total > 0)
            .OrderByDescending(x => x.Inactivo)
            .ThenByDescending(x => x.Total)
            .ToList();

        if (rows.Count == 0)
        {
            host.Children.Add(MutedNote("Sem metadados de estado nesta recolha."));
            return;
        }

        var totalActivo = rows.Sum(r => r.Activo);
        var totalInactivo = rows.Sum(r => r.Inactivo);
        host.Children.Add(new TextBlock
        {
            Text = $"Total com estado: {totalActivo + totalInactivo:N0}  ·  Activo {totalActivo:N0}  ·  Inactivo {totalInactivo:N0}",
            Foreground = Muted,
            Margin = new Thickness(0, 0, 0, 12),
            FontSize = 12
        });

        foreach (var row in rows)
        {
            var right = new TextBlock
            {
                Text = $"Activo {row.Activo} · Inactivo {row.Inactivo}",
                Foreground = Muted,
                VerticalAlignment = VerticalAlignment.Center
            };
            var header = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
            DockPanel.SetDock(right, Dock.Right);
            header.Children.Add(right);
            header.Children.Add(new TextBlock
            {
                Text = friendlyKind(row.Kind),
                Foreground = Text,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            host.Children.Add(header);

            var track = new Grid { Height = 12, Margin = new Thickness(0, 0, 0, 12) };
            var a = Math.Max(row.Activo, 0.001);
            var i = Math.Max(row.Inactivo, 0.001);
            track.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(a, GridUnitType.Star) });
            track.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(i, GridUnitType.Star) });
            var activeBar = new Border { Background = Ok, CornerRadius = new CornerRadius(4, 0, 0, 4) };
            var inactiveBar = new Border { Background = Danger, CornerRadius = new CornerRadius(0, 4, 4, 0) };
            Grid.SetColumn(activeBar, 0);
            Grid.SetColumn(inactiveBar, 1);
            track.Children.Add(activeBar);
            track.Children.Add(inactiveBar);
            var wrap = new Border { Background = BarTrack, CornerRadius = new CornerRadius(4), Child = track };
            host.Children.Add(wrap);
        }
    }

    private static string? TryEstado(string? summaryJson)
    {
        if (string.IsNullOrWhiteSpace(summaryJson))
        {
            return null;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(summaryJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("estado", out var estado)
                && estado.ValueKind == System.Text.Json.JsonValueKind.String
                && !string.IsNullOrWhiteSpace(estado.GetString()))
            {
                return estado.GetString();
            }

            foreach (var prop in new[] { "inactivo", "inativo" })
            {
                if (!root.TryGetProperty(prop, out var flag))
                {
                    continue;
                }

                var inactive = flag.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.True => true,
                    System.Text.Json.JsonValueKind.False => false,
                    System.Text.Json.JsonValueKind.Number => flag.TryGetInt64(out var n) ? n != 0 : flag.GetDouble() != 0,
                    System.Text.Json.JsonValueKind.String =>
                        flag.GetString() is { } s
                        && (s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1"),
                    _ => false
                };
                return inactive ? "Inactivo" : "Activo";
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static void RenderBarChart(Panel host, IReadOnlyList<(string Label, int Count)> series, Brush barColor)
    {
        host.Children.Clear();
        if (series.Count == 0)
        {
            host.Children.Add(MutedNote("Sem itens nesta categoria."));
            return;
        }

        var max = Math.Max(1, series.Max(s => s.Count));
        foreach (var (label, count) in series)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });

            var name = new TextBlock
            {
                Text = label,
                Foreground = Text,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(name, 0);

            var fillHost = new Grid();
            fillHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(count, 0.001), GridUnitType.Star) });
            fillHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(max - count, 0.001), GridUnitType.Star) });
            var fill = new Border { Background = barColor, CornerRadius = new CornerRadius(4) };
            Grid.SetColumn(fill, 0);
            fillHost.Children.Add(fill);

            var track = new Border
            {
                Background = BarTrack,
                CornerRadius = new CornerRadius(4),
                Height = 14,
                Margin = new Thickness(8, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = fillHost
            };
            Grid.SetColumn(track, 1);

            var value = new TextBlock
            {
                Text = count.ToString("N0"),
                Foreground = Muted,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetColumn(value, 2);

            row.Children.Add(name);
            row.Children.Add(track);
            row.Children.Add(value);
            host.Children.Add(row);
        }
    }

    private static UIElement KpiCard(string title, string value, string subtitle, Brush accent)
    {
        var accentBar = new Border
        {
            Width = 4,
            Background = accent,
            CornerRadius = new CornerRadius(2),
            Margin = new Thickness(0, 0, 12, 0)
        };

        var body = new StackPanel();
        body.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = Muted,
            FontSize = 12
        });
        body.Children.Add(new TextBlock
        {
            Text = value,
            Foreground = Text,
            FontSize = 26,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 2, 0, 2)
        });
        body.Children.Add(new TextBlock
        {
            Text = subtitle,
            Foreground = Muted,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap
        });

        var content = new DockPanel();
        DockPanel.SetDock(accentBar, Dock.Left);
        content.Children.Add(accentBar);
        content.Children.Add(body);

        return new Border
        {
            Background = PanelInner,
            BorderBrush = Brush("#2A3140"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 10, 10),
            Child = content
        };
    }

    private static TextBlock MutedNote(string text) => new()
    {
        Text = text,
        Foreground = Muted,
        Margin = new Thickness(0, 8, 0, 0)
    };

    private static (string Text, Brush Fg, Brush Bg) StatusStyle(WorkspaceSummary summary)
    {
        if (summary.ErrorCount > 0)
        {
            return ("Com erros", Danger, Brush("#2A1515"));
        }

        if (summary.WarningCount > 0)
        {
            return ("Com avisos", Warn, Brush("#2A2114"));
        }

        return summary.Status.ToLowerInvariant() switch
        {
            "analyzed" or "succeeded" or "completed" => ("OK", Ok, Brush("#1A2A20")),
            _ => (summary.Status, Accent, Brush("#2A2418"))
        };
    }

    private static int CountOf(IReadOnlyDictionary<string, int> counts, string kind) =>
        counts.TryGetValue(kind, out var n) ? n : 0;

    private static SolidColorBrush Brush(string hex)
    {
        var b = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
        if (b.CanFreeze)
        {
            b.Freeze();
        }

        return b;
    }
}
