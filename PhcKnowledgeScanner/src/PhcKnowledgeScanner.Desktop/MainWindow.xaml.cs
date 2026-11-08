using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Search;
using Microsoft.Win32;
using PhcKnowledgeScanner.Abstractions.Connection;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;
using PhcKnowledgeScanner.Abstractions.Progress;
using PhcKnowledgeScanner.Abstractions.SchemaSnapshot;
using PhcKnowledgeScanner.Application.Services;
using PhcKnowledgeScanner.Export.Ikp;

namespace PhcKnowledgeScanner.Desktop;

public partial class MainWindow : Window
{
    private readonly IConnectionAppService _connectionService;
    private readonly IScanAppService _scanService;
    private readonly IInventoryAppService _inventoryService;
    private readonly IWorkspaceExportService _exportService;
    private readonly IDatabaseSchemaSnapshotService _schemaSnapshotService;
    private readonly WorkspaceSessionHost _workspaceSessionHost;

    private CancellationTokenSource? _scanCts;
    private readonly ObservableCollection<string> _progressLog = [];
    private readonly ObservableCollection<ExplorerGroup> _groups = [];
    private readonly ObservableCollection<InventoryTableListItem> _inventoryTables = [];
    private readonly ObservableCollection<RelationListItem> _outgoingRelations = [];
    private readonly ObservableCollection<RelationListItem> _incomingRelations = [];
    private readonly ObservableCollection<FieldMappingView> _fieldMappings = [];
    private SqlConnectionRequest? _lastRequest;
    private readonly DispatcherTimer _searchDebounce;
    private readonly DispatcherTimer _inventorySearchDebounce;
    private SearchPanel? _codeSearchPanel;
    private string? _lastListSearch;
    private bool _suppressGroupSelection;
    private bool _suppressInventorySelection;
    private bool _suppressRelationSelection;
    private bool _suppressFieldMappingSelection;
    private string? _selectedInventoryObjectId;
    private RelationDetailView? _selectedRelationDetail;

    public MainWindow(
        IConnectionAppService connectionService,
        IScanAppService scanService,
        IInventoryAppService inventoryService,
        IWorkspaceExportService exportService,
        IDatabaseSchemaSnapshotService schemaSnapshotService)
    {
        InitializeComponent();
        _connectionService = connectionService;
        _scanService = scanService;
        _inventoryService = inventoryService;
        _exportService = exportService;
        _schemaSnapshotService = schemaSnapshotService;
        _workspaceSessionHost = new WorkspaceSessionHost();

        LstProgressLog.ItemsSource = _progressLog;
        LstObjectGroups.ItemsSource = _groups;
        LstInventoryTables.ItemsSource = _inventoryTables;
        LstOutgoingRelations.ItemsSource = _outgoingRelations;
        LstIncomingRelations.ItemsSource = _incomingRelations;
        LstFieldMappings.ItemsSource = _fieldMappings;
        DarkSyntaxHighlighting.ConfigureEditor(CodeEditor);
        _codeSearchPanel = SearchPanel.Install(CodeEditor);

        _searchDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(280) };
        _searchDebounce.Tick += async (_, _) =>
        {
            _searchDebounce.Stop();
            if (IsLoaded && PageExplorer.Visibility == Visibility.Visible)
            {
                await LoadObjectsAsync();
            }
        };

        _inventorySearchDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(220) };
        _inventorySearchDebounce.Tick += (_, _) =>
        {
            _inventorySearchDebounce.Stop();
            if (IsLoaded && PageInventory.Visibility == Visibility.Visible)
            {
                RefreshInventoryTableList();
            }
        };

        Closed += MainWindow_OnClosed;
        UpdateSearchWatermark();
        UpdateInventorySearchWatermark();
        UpdateInventorySessionChrome();
        ShowPage("connection");
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag })
        {
            ShowPage(tag);
            if (tag is "dashboard" or "explorer" or "analysis")
            {
                _ = RefreshViewsAsync();
            }
        }
    }

    private void ShowPage(string tag)
    {
        PageConnection.Visibility = tag == "connection" ? Visibility.Visible : Visibility.Collapsed;
        PageProgress.Visibility = tag == "progress" ? Visibility.Visible : Visibility.Collapsed;
        PageDashboard.Visibility = tag == "dashboard" ? Visibility.Visible : Visibility.Collapsed;
        PageInventory.Visibility = tag == "inventory" ? Visibility.Visible : Visibility.Collapsed;
        PageExplorer.Visibility = tag == "explorer" ? Visibility.Visible : Visibility.Collapsed;
        PageAnalysis.Visibility = tag == "analysis" ? Visibility.Visible : Visibility.Collapsed;
        PageExport.Visibility = tag == "export" ? Visibility.Visible : Visibility.Collapsed;
        SetNavSelected(tag);
        if (tag == "inventory")
        {
            RefreshInventoryTableList();
        }
    }

    private void SetNavSelected(string tag)
    {
        var selected = (Style)FindResource("NavButtonSelected");
        var normal = (Style)FindResource("NavButton");
        foreach (var btn in new[]
                 {
                     NavConnection, NavProgress, NavDashboard, NavInventory, NavExplorer, NavAnalysis, NavExport
                 })
        {
            btn.Style = string.Equals(btn.Tag as string, tag, StringComparison.OrdinalIgnoreCase)
                ? selected
                : normal;
        }
    }

    private void CmbAuth_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Durante InitializeComponent o ComboBox dispara SelectionChanged antes dos outros controlos existirem.
        if (LblUser is null || TxtUser is null || LblPassword is null || TxtPassword is null)
        {
            return;
        }

        var sqlAuth = GetSelectedAuth() == SqlAuthenticationMode.SqlPassword;
        LblUser.Visibility = sqlAuth ? Visibility.Visible : Visibility.Collapsed;
        TxtUser.Visibility = sqlAuth ? Visibility.Visible : Visibility.Collapsed;
        LblPassword.Visibility = sqlAuth ? Visibility.Visible : Visibility.Collapsed;
        TxtPassword.Visibility = sqlAuth ? Visibility.Visible : Visibility.Collapsed;
    }

    private SqlAuthenticationMode GetSelectedAuth()
    {
        if (CmbAuth.SelectedItem is ComboBoxItem { Tag: string tag } &&
            Enum.TryParse<SqlAuthenticationMode>(tag, out var mode))
        {
            return mode;
        }

        return SqlAuthenticationMode.Windows;
    }

    private SqlConnectionRequest BuildRequest()
    {
        return new SqlConnectionRequest
        {
            Server = TxtServer.Text.Trim(),
            Database = TxtDatabase.Text.Trim(),
            AuthenticationMode = GetSelectedAuth(),
            UserName = string.IsNullOrWhiteSpace(TxtUser.Text) ? null : TxtUser.Text.Trim(),
            Password = TxtPassword.Password.Length == 0 ? null : TxtPassword.Password,
            TrustServerCertificate = true
        };
    }

    private async void BtnTestConnection_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            TxtConnectionStatus.Text = "A testar ligação...";
            var result = await _connectionService.TestConnectionAsync(BuildRequest());
            TxtConnectionStatus.Text = result.Succeeded
                ? $"{result.Message}\nServidor: {result.ServerVersion}\nBD: {result.DatabaseName}\nUtilizador: {result.CurrentUser}"
                : result.Message;
            TxtConnectionStatus.Foreground = result.Succeeded
                ? (System.Windows.Media.Brush)FindResource("OkBrush")
                : (System.Windows.Media.Brush)FindResource("DangerBrush");
        }
        catch (Exception ex)
        {
            TxtConnectionStatus.Text = ex.Message;
            TxtConnectionStatus.Foreground = (System.Windows.Media.Brush)FindResource("DangerBrush");
        }
    }

    private async void BtnStartScan_Click(object sender, RoutedEventArgs e)
    {
        _lastRequest = BuildRequest();
        if (string.IsNullOrWhiteSpace(_lastRequest.Database))
        {
            MessageBox.Show("Indique a base de dados.", "Ligação", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        _progressLog.Clear();
        ShowPage("progress");
        ProgressBarScan.Value = 0;

        var workspaceRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PhcKnowledgeScanner",
            "workspace-current");

        var sink = new UiProgressSink(this);
        try
        {
            TxtConnectionStatus.Text = "Recolha em curso...";
            var manifest = await Task.Run(
                () => _scanService.RunStructureScanAsync(_lastRequest, workspaceRoot, sink, _scanCts.Token),
                _scanCts.Token);

            AppendLog($"Concluído. Collectors: {manifest.Results.Count}. Versão scanner: {manifest.ScannerVersion}");
            await RefreshViewsAsync();
            ShowPage("dashboard");
            TxtConnectionStatus.Text = "Recolha concluída.";
            TxtConnectionStatus.Foreground = (System.Windows.Media.Brush)FindResource("OkBrush");
        }
        catch (OperationCanceledException)
        {
            AppendLog("Recolha cancelada.");
            TxtProgressStage.Text = "Cancelada";
        }
        catch (Exception ex)
        {
            AppendLog("Erro: " + ex.Message);
            MessageBox.Show(ex.Message, "Recolha", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // Limpar password da UI após uso opcional? Mantemos na sessão para re-scan; não persistimos.
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => _scanCts?.Cancel();

    private async void BtnOpenWorkspace_Click(object sender, RoutedEventArgs e)
    {
        if (_workspaceSessionHost.IsOpening)
        {
            MessageBox.Show(
                "Já existe uma abertura de workspace em curso.",
                "Inventário",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Abrir workspace SQLite",
            Filter = "Workspace SQLite (workspace.db)|workspace.db|Ficheiros SQLite (*.db)|*.db|Todos (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        TxtInventorySessionTitle.Text = "A abrir workspace…";
        TxtInventorySessionMeta.Text = dialog.FileName;
        TxtInventorySearch.IsEnabled = false;

        var previousScanId = _workspaceSessionHost.ActiveSession?.ScanId;
        WorkspaceSessionOpenResult result;
        try
        {
            result = await _workspaceSessionHost.OpenAsync(dialog.FileName);
        }
        catch (Exception ex)
        {
            result = WorkspaceSessionOpenResult.Failed(ex.Message);
        }

        if (!result.Success || result.Session is null)
        {
            UpdateInventorySessionChrome();
            MessageBox.Show(
                result.ErrorMessage ?? "Falha ao abrir o workspace.",
                "Inventário",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            // Sessão anterior preservada pelo host — repor lista se ainda válida.
            if (_workspaceSessionHost.HasSession
                && string.Equals(previousScanId, _workspaceSessionHost.ActiveSession?.ScanId, StringComparison.Ordinal))
            {
                RefreshInventoryTableList();
            }

            return;
        }

        ClearInventoryDetail();
        UpdateInventorySessionChrome();
        RefreshInventoryTableList();
        ShowPage("inventory");
    }

    private async void BtnCloseWorkspaceSession_Click(object sender, RoutedEventArgs e)
    {
        if (!_workspaceSessionHost.HasSession && !_workspaceSessionHost.IsOpening)
        {
            return;
        }

        try
        {
            await _workspaceSessionHost.CloseSessionAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Não foi possível fechar a sessão: " + ex.Message,
                "Inventário",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _inventoryTables.Clear();
        ClearInventoryDetail();
        UpdateInventorySessionChrome();
        TxtInventoryResultCount.Text = string.Empty;
    }

    private void BtnNavigateRelatedFrom_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRelationDetail is { CanNavigateFrom: true, FromObjectId: { } id })
        {
            NavigateToInventoryTable(id);
        }
    }

    private void BtnNavigateRelatedTo_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRelationDetail is { CanNavigateTo: true, ToObjectId: { } id })
        {
            NavigateToInventoryTable(id);
        }
    }

    private void TxtInventorySearch_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateInventorySearchWatermark();
        _inventorySearchDebounce.Stop();
        _inventorySearchDebounce.Start();
    }

    private void TxtInventorySearch_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            TxtInventorySearch.Text = string.Empty;
            RefreshInventoryTableList();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            _inventorySearchDebounce.Stop();
            RefreshInventoryTableList();
            e.Handled = true;
        }
    }

    private void UpdateInventorySearchWatermark()
    {
        if (TxtInventorySearchWatermark is null || TxtInventorySearch is null)
        {
            return;
        }

        TxtInventorySearchWatermark.Visibility = string.IsNullOrWhiteSpace(TxtInventorySearch.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void UpdateInventorySessionChrome()
    {
        var session = _workspaceSessionHost.ActiveSession;
        var has = session is not null;
        TxtInventorySearch.IsEnabled = has;
        if (!has)
        {
            TxtInventorySessionTitle.Text = "Nenhum workspace aberto";
            TxtInventorySessionMeta.Text = "Seleccione a pasta do scan ou o ficheiro workspace.db.";
            return;
        }

        var folderName = Path.GetFileName(session!.WorkspaceRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        TxtInventorySessionTitle.Text = string.IsNullOrWhiteSpace(folderName)
            ? session.InstallationLabel
            : $"{folderName} · {session.InstallationLabel}";
        TxtInventorySessionMeta.Text =
            $"ScanId: {session.ScanId} · {session.Tables.Count} tabelas · {session.DatabasePath}";
    }

    private void RefreshInventoryTableList(string? preferSelectObjectId = null)
    {
        var keepId = preferSelectObjectId ?? _selectedInventoryObjectId;
        _inventoryTables.Clear();
        ClearInventoryRelationPanels();

        if (!_workspaceSessionHost.HasSession)
        {
            TxtInventoryResultCount.Text = string.Empty;
            ClearInventoryTableHeader();
            return;
        }

        var search = string.IsNullOrWhiteSpace(TxtInventorySearch.Text)
            ? null
            : TxtInventorySearch.Text.Trim();
        IReadOnlyList<TechnicalObjectRecord> tables;
        try
        {
            // Ordem já vem priorizada por TableSearch (exact → prefix → contains → schema).
            tables = _workspaceSessionHost.FindTables(search);
        }
        catch (Exception ex)
        {
            TxtInventoryResultCount.Text = ex.Message;
            return;
        }

        foreach (var t in tables)
        {
            var display = string.IsNullOrWhiteSpace(t.SchemaName) ? t.Name : $"{t.SchemaName}.{t.Name}";
            _inventoryTables.Add(new InventoryTableListItem(t.ObjectId, display));
        }

        TxtInventoryResultCount.Text = $"{tables.Count} tabelas";

        if (!string.IsNullOrWhiteSpace(keepId)
            && _inventoryTables.Any(t => string.Equals(t.ObjectId, keepId, StringComparison.OrdinalIgnoreCase)))
        {
            SelectInventoryTableInList(keepId);
            ShowInventoryTable(keepId);
        }
        else
        {
            _selectedInventoryObjectId = null;
            ClearInventoryTableHeader();
            ClearInventoryRelationPanels();
        }
    }

    private void InventoryTable_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressInventorySelection)
        {
            return;
        }

        if (LstInventoryTables.SelectedItem is not InventoryTableListItem item)
        {
            _selectedInventoryObjectId = null;
            ClearInventoryRelationPanels();
            ClearInventoryTableHeader();
            return;
        }

        ShowInventoryTable(item.ObjectId);
    }

    private void InventoryRelation_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressRelationSelection)
        {
            return;
        }

        if (sender is not ListBox list)
        {
            return;
        }

        if (list.SelectedItem is not RelationListItem item)
        {
            if (ReferenceEquals(list, LstOutgoingRelations) || ReferenceEquals(list, LstIncomingRelations))
            {
                // Só limpar detalhe se nenhuma lista tiver selecção.
                if (LstOutgoingRelations.SelectedItem is null && LstIncomingRelations.SelectedItem is null)
                {
                    ClearRelationDetail();
                }
            }

            return;
        }

        // Evitar selecção simultânea nas duas listas.
        _suppressRelationSelection = true;
        try
        {
            if (ReferenceEquals(list, LstOutgoingRelations))
            {
                LstIncomingRelations.SelectedItem = null;
            }
            else
            {
                LstOutgoingRelations.SelectedItem = null;
            }
        }
        finally
        {
            _suppressRelationSelection = false;
        }

        ShowRelationDetail(item.KnowledgeRelationId);
    }

    private void ShowInventoryTable(string objectId)
    {
        var table = _workspaceSessionHost.GetTable(objectId);
        if (table is null)
        {
            ClearInventoryDetail();
            return;
        }

        _selectedInventoryObjectId = objectId;
        TxtInventoryDetailTitle.Text = string.IsNullOrWhiteSpace(table.SchemaName)
            ? table.Name
            : $"{table.SchemaName}.{table.Name}";

        var lines = new List<string>
        {
            $"ObjectId: {table.ObjectId}",
            $"Kind: {table.Kind}",
            $"Name: {table.Name}",
            $"Schema: {table.SchemaName ?? "(n/d)"}",
            $"Collector: {table.CollectorId}",
            $"Recolhido: {table.CollectedAtUtc:O}"
        };
        TxtInventoryDetailBody.Text = string.Join(Environment.NewLine, lines);

        LoadRelationsForSelectedTable(objectId);
    }

    private void LoadRelationsForSelectedTable(string objectId)
    {
        _outgoingRelations.Clear();
        _incomingRelations.Clear();
        ClearRelationDetail();

        var session = _workspaceSessionHost.ActiveSession;
        if (session is null)
        {
            TxtInventoryRelationSummary.Text = string.Empty;
            TxtOutgoingCount.Text = string.Empty;
            TxtIncomingCount.Text = string.Empty;
            return;
        }

        // Mesma projeção em memória — ProjectionBuildCount não muda.
        var board = InventoryRelationExplorer.BuildBoard(session.Knowledge, objectId);
        foreach (var item in board.Outgoing)
        {
            _outgoingRelations.Add(item);
        }

        foreach (var item in board.Incoming)
        {
            _incomingRelations.Add(item);
        }

        TxtOutgoingCount.Text = $"{board.Outgoing.Count} relações de saída";
        TxtIncomingCount.Text = $"{board.Incoming.Count} relações de entrada";
        TxtInventoryRelationSummary.Text =
            $"Saída: {board.Outgoing.Count} · Entrada: {board.Incoming.Count} · projeção #{session.ProjectionBuildCount}";
    }

    private void ShowRelationDetail(string knowledgeRelationId)
    {
        var session = _workspaceSessionHost.ActiveSession;
        if (session is null)
        {
            ClearRelationDetail();
            return;
        }

        var detail = InventoryRelationExplorer.BuildDetail(session.Knowledge, knowledgeRelationId);
        _selectedRelationDetail = detail;
        if (detail is null)
        {
            TxtRelationDetailBody.Text = "Relação não encontrada na projeção.";
            BtnNavigateRelatedFrom.IsEnabled = false;
            BtnNavigateRelatedTo.IsEnabled = false;
            ClearFieldMappingsPanel();
            return;
        }

        BtnNavigateRelatedFrom.IsEnabled = detail.CanNavigateFrom;
        BtnNavigateRelatedTo.IsEnabled = detail.CanNavigateTo;
        TxtRelationDetailBody.Text = FormatRelationDetail(detail);
        LoadFieldMappings(detail.FieldMappings);
    }

    private void LoadFieldMappings(IReadOnlyList<FieldMappingView> mappings)
    {
        _suppressFieldMappingSelection = true;
        try
        {
            _fieldMappings.Clear();
            foreach (var m in mappings)
            {
                _fieldMappings.Add(m);
            }

            if (LstFieldMappings is not null)
            {
                LstFieldMappings.SelectedItem = null;
            }
        }
        finally
        {
            _suppressFieldMappingSelection = false;
        }

        if (TxtFieldMappingCount is not null)
        {
            TxtFieldMappingCount.Text = mappings.Count == 0
                ? "0 mapeamentos"
                : $"{mappings.Count} mapeamentos";
        }

        if (TxtFieldMappingDetailBody is not null)
        {
            TxtFieldMappingDetailBody.Text = mappings.Count == 0
                ? "Esta relação não tem FieldMappings na projeção."
                : "Seleccione um mapeamento.";
        }
    }

    private void FieldMapping_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressFieldMappingSelection)
        {
            return;
        }

        if (LstFieldMappings.SelectedItem is not FieldMappingView mapping)
        {
            if (TxtFieldMappingDetailBody is not null)
            {
                TxtFieldMappingDetailBody.Text = _fieldMappings.Count == 0
                    ? "Esta relação não tem FieldMappings na projeção."
                    : "Seleccione um mapeamento.";
            }

            return;
        }

        if (TxtFieldMappingDetailBody is not null)
        {
            TxtFieldMappingDetailBody.Text = FormatFieldMappingDetail(mapping);
        }
    }

    private static string FormatFieldMappingDetail(FieldMappingView m)
    {
        var lines = new List<string>
        {
            $"FieldMappingId: {m.FieldMappingId}",
            $"Tipo: {m.Kind} — {m.KindLabel}",
            $"Operador: {m.Operator}",
            $"PredicateText: {m.PredicateText}",
            $"Contexto lógico: {m.LogicalContextLabel}",
            $"LogicalPath: {m.LogicalPath}",
            $"AstPath: {m.AstPath}",
            $"PredicateOrdinal: {m.PredicateOrdinal}",
            string.Empty,
            "— Lado esquerdo —",
        };
        lines.AddRange(FormatOperandLines(m.Left));
        lines.Add(string.Empty);
        lines.Add("— Lado direito —");
        lines.AddRange(FormatOperandLines(m.Right));
        lines.Add(string.Empty);
        lines.Add("— Proveniência —");
        lines.Add($"EvidenceId: {m.EvidenceId}");
        lines.Add($"Evidência: {m.EvidenceKindLabel}" +
                  (m.EvidenceKind is null ? string.Empty : $" ({m.EvidenceKind})"));
        lines.Add($"InterpretationEvidenceId: {m.InterpretationEvidenceId ?? "(n/d)"}");
        lines.Add($"ParserVersion: {m.ParserVersion ?? "(n/d)"}");
        if (!string.IsNullOrWhiteSpace(m.DiagnosticsJson))
        {
            lines.Add($"DiagnosticsJson: {m.DiagnosticsJson}");
        }
        else
        {
            lines.Add("Diagnósticos: (nenhum)");
        }

        if (m.HasIncompleteResolution)
        {
            lines.Add("Estado: resolução incompleta (campo não resolvido / ambíguo / não qualificado presente)");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static IEnumerable<string> FormatOperandLines(FieldOperandView op)
    {
        yield return $"  Tipo operando: {op.KindLabel} ({op.Kind})";
        yield return $"  Texto original: {(string.IsNullOrWhiteSpace(op.OriginalText) ? "(Não especificado)" : op.OriginalText)}";
        if (op.PrimaryField is not null)
        {
            yield return $"  Campo: {FormatMappedFieldLines(op.PrimaryField)}";
        }
        else
        {
            yield return "  Campo principal: (Não especificado)";
        }

        if (op.ContainedFields.Count > 0)
        {
            yield return "  Campos contidos na expressão:";
            foreach (var f in op.ContainedFields)
            {
                yield return "    · " + FormatMappedFieldLines(f);
            }
        }
    }

    private static string FormatMappedFieldLines(MappedFieldView f) =>
        $"{f.Display} · tabela={f.TableLabel} · objectId={f.TableObjectId ?? "(n/d)"} · original={f.OriginalText}";

    private static string FormatRelationDetail(RelationDetailView d)
    {
        var lines = new List<string>
        {
            $"KnowledgeRelationId: {d.KnowledgeRelationId}",
            $"Origem: {d.FromDisplay} [{d.FromResolution}] {(d.FromObjectId ?? "(não resolvido)")}",
            $"Destino: {d.ToDisplay} [{d.ToResolution}] {(d.ToObjectId ?? "(não resolvido)")}",
            $"Resolução extremos: {d.EndpointResolutionState}",
            $"Correspondência: {d.CorrespondenceLabel}",
            $"Fundamento: {d.BasisLabel}",
            $"Evidências de ligação: {d.EvidenceKindsLabel}",
            $"ExactGroup: {d.ExactCorrespondenceGroupId ?? "(n/d)"}",
            $"ProbableGroup: {d.ProbableCorrespondenceGroupId ?? "(n/d)"}",
            $"LinkEvidenceIds: {string.Join(", ", d.LinkEvidenceIds)}",
            $"InterpretationEvidenceIds: {(d.InterpretationEvidenceIds.Count == 0 ? "(nenhuma)" : string.Join(", ", d.InterpretationEvidenceIds))}",
            $"FieldMappings: {d.FieldMappings.Count}"
        };

        if (d.DiagnosticsLines.Count > 0)
        {
            lines.Add("Diagnósticos:");
            lines.AddRange(d.DiagnosticsLines.Select(x => "  · " + x));
        }
        else
        {
            lines.Add("Diagnósticos: (nenhum)");
        }

        lines.Add(string.Empty);
        lines.Add("— Evidências originais (FK / REL / DTL) —");
        foreach (var ev in d.LinkEvidences)
        {
            lines.AddRange(FormatEvidenceLines(ev));
            lines.Add(string.Empty);
        }

        if (d.InterpretationEvidences.Count > 0)
        {
            lines.Add("— Interpretações do parser (não são a expressão original) —");
            foreach (var ev in d.InterpretationEvidences)
            {
                lines.AddRange(FormatEvidenceLines(ev));
                lines.Add(string.Empty);
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static IEnumerable<string> FormatEvidenceLines(EvidenceView ev)
    {
        yield return $"[{ev.KindLabel}] {ev.EvidenceId}";
        yield return $"  SourceId: {ev.SourceId}";
        yield return $"  De: {ev.FromDisplay} [{ev.FromResolution}] {ev.FromObjectId ?? "(n/d)"}";
        yield return $"  Para: {ev.ToDisplay} [{ev.ToResolution}] {ev.ToObjectId ?? "(n/d)"}";

        if (ev.Kind is RelationEvidenceKind.PhcRel or RelationEvidenceKind.PhcDtl)
        {
            yield return $"  Expressão original: {ev.OriginalExpression ?? "(n/d)"}";
            if (ev.CorrespondenceKind is not null)
            {
                yield return $"  CorrespondenceKind: {ev.CorrespondenceKind}";
            }

            if (!string.IsNullOrWhiteSpace(ev.CorrespondenceGroupId))
            {
                yield return $"  CorrespondenceGroupId: {ev.CorrespondenceGroupId}";
            }
        }
        else if (ev.Kind == RelationEvidenceKind.SqlForeignKey)
        {
            if (!string.IsNullOrWhiteSpace(ev.FkColumnsSummary))
            {
                yield return $"  Colunas: {ev.FkColumnsSummary}";
            }

            if (!string.IsNullOrWhiteSpace(ev.FkActions.DeleteAction))
            {
                yield return $"  DeleteAction: {ev.FkActions.DeleteAction}";
            }

            if (!string.IsNullOrWhiteSpace(ev.FkActions.UpdateAction))
            {
                yield return $"  UpdateAction: {ev.FkActions.UpdateAction}";
            }
        }
        else if (ev.Kind == RelationEvidenceKind.ExpressionInterpretation)
        {
            yield return $"  ParserVersion: {ev.ParserVersion ?? "(n/d)"}";
            yield return $"  ParseStatus: {ev.ParseStatus?.ToString() ?? "(n/d)"}";
            yield return $"  Confidence: {ev.Confidence?.ToString() ?? "(n/d)"}";
            yield return $"  LinkedDeclaredRelationId: {ev.LinkedDeclaredRelationId ?? "(n/d)"}";
            if (!string.IsNullOrWhiteSpace(ev.DiagnosticsJson))
            {
                yield return $"  DiagnosticsJson: {ev.DiagnosticsJson}";
            }
        }

        if (ev.ObservedAtUtc is not null)
        {
            yield return $"  ObservedAtUtc: {ev.ObservedAtUtc:O}";
        }
    }

    private void NavigateToInventoryTable(string objectId)
    {
        if (!_workspaceSessionHost.HasSession)
        {
            return;
        }

        var table = _workspaceSessionHost.GetTable(objectId);
        if (table is null)
        {
            MessageBox.Show(
                "A tabela relacionada não está resolvida no inventário.",
                "Inventário",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        // Garantir visibilidade na lista (limpa filtro se necessário).
        if (_inventoryTables.All(t => !string.Equals(t.ObjectId, objectId, StringComparison.OrdinalIgnoreCase)))
        {
            TxtInventorySearch.Text = table.Name;
            RefreshInventoryTableList(objectId);
            return;
        }

        SelectInventoryTableInList(objectId);
        ShowInventoryTable(objectId);
    }

    private void SelectInventoryTableInList(string objectId)
    {
        var item = _inventoryTables.FirstOrDefault(t =>
            string.Equals(t.ObjectId, objectId, StringComparison.OrdinalIgnoreCase));
        if (item is null)
        {
            return;
        }

        _suppressInventorySelection = true;
        try
        {
            LstInventoryTables.SelectedItem = item;
            LstInventoryTables.ScrollIntoView(item);
        }
        finally
        {
            _suppressInventorySelection = false;
        }
    }

    private void ClearInventoryDetail()
    {
        _selectedInventoryObjectId = null;
        ClearInventoryTableHeader();
        ClearInventoryRelationPanels();

        if (LstInventoryTables is null)
        {
            return;
        }

        _suppressInventorySelection = true;
        try
        {
            LstInventoryTables.SelectedItem = null;
        }
        finally
        {
            _suppressInventorySelection = false;
        }
    }

    private void ClearInventoryTableHeader()
    {
        if (TxtInventoryDetailTitle is null)
        {
            return;
        }

        TxtInventoryDetailTitle.Text = "Seleccione uma tabela";
        TxtInventoryDetailBody.Text = string.Empty;
        if (TxtInventoryRelationSummary is not null)
        {
            TxtInventoryRelationSummary.Text = string.Empty;
        }
    }

    private void ClearInventoryRelationPanels()
    {
        _outgoingRelations.Clear();
        _incomingRelations.Clear();
        if (TxtOutgoingCount is not null)
        {
            TxtOutgoingCount.Text = string.Empty;
        }

        if (TxtIncomingCount is not null)
        {
            TxtIncomingCount.Text = string.Empty;
        }

        ClearRelationDetail();
    }

    private void ClearRelationDetail()
    {
        _selectedRelationDetail = null;
        if (TxtRelationDetailBody is not null)
        {
            TxtRelationDetailBody.Text = "Seleccione uma relação de saída ou de entrada.";
        }

        if (BtnNavigateRelatedFrom is not null)
        {
            BtnNavigateRelatedFrom.IsEnabled = false;
        }

        if (BtnNavigateRelatedTo is not null)
        {
            BtnNavigateRelatedTo.IsEnabled = false;
        }

        ClearFieldMappingsPanel();

        _suppressRelationSelection = true;
        try
        {
            if (LstOutgoingRelations is not null)
            {
                LstOutgoingRelations.SelectedItem = null;
            }

            if (LstIncomingRelations is not null)
            {
                LstIncomingRelations.SelectedItem = null;
            }
        }
        finally
        {
            _suppressRelationSelection = false;
        }
    }

    private void ClearFieldMappingsPanel()
    {
        _suppressFieldMappingSelection = true;
        try
        {
            _fieldMappings.Clear();
            if (LstFieldMappings is not null)
            {
                LstFieldMappings.SelectedItem = null;
            }
        }
        finally
        {
            _suppressFieldMappingSelection = false;
        }

        if (TxtFieldMappingCount is not null)
        {
            TxtFieldMappingCount.Text = string.Empty;
        }

        if (TxtFieldMappingDetailBody is not null)
        {
            TxtFieldMappingDetailBody.Text = "Seleccione um mapeamento.";
        }
    }

    private async void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        try
        {
            await _workspaceSessionHost.DisposeAsync();
        }
        catch
        {
            // fecho best-effort
        }
    }

    private async void BtnSearch_Click(object sender, RoutedEventArgs e) => await LoadObjectsAsync();

    private async void BtnClearSearch_Click(object sender, RoutedEventArgs e)
    {
        TxtSearch.Text = string.Empty;
        _lastListSearch = null;
        if (TxtFindStatus is not null)
        {
            TxtFindStatus.Text = string.Empty;
        }

        await LoadObjectsAsync();
    }

    private void BtnFindInCode_Click(object sender, RoutedEventArgs e) => OpenCodeSearch();

    private void OpenCodeSearch()
    {
        if (_codeSearchPanel is null)
        {
            return;
        }

        var seed = !string.IsNullOrWhiteSpace(TxtSearch.Text)
            ? TxtSearch.Text.Trim()
            : _lastListSearch;
        if (!string.IsNullOrWhiteSpace(seed))
        {
            _codeSearchPanel.SearchPattern = seed;
        }

        _codeSearchPanel.Open();
        _codeSearchPanel.Reactivate();
    }

    private void TxtSearch_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateSearchWatermark();
        _searchDebounce.Stop();
        _searchDebounce.Start();
    }

    private async void TxtSearch_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _searchDebounce.Stop();
            e.Handled = true;
            await LoadObjectsAsync();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            TxtSearch.Text = string.Empty;
            _lastListSearch = null;
            if (TxtFindStatus is not null)
            {
                TxtFindStatus.Text = string.Empty;
            }

            await LoadObjectsAsync();
        }
    }

    private void UpdateSearchWatermark()
    {
        if (TxtSearchWatermark is null || TxtSearch is null)
        {
            return;
        }

        TxtSearchWatermark.Visibility = string.IsNullOrWhiteSpace(TxtSearch.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private async void ExplorerFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
        {
            await LoadObjectsAsync();
        }
    }

    private async void GroupedObject_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressGroupSelection)
        {
            return;
        }

        if (sender is not ListBox list || list.SelectedItem is not ObjectListItem item)
        {
            return;
        }

        // Uma selecção de cada vez entre categorias.
        _suppressGroupSelection = true;
        try
        {
            ClearSiblingListSelections(list);
            await ShowObjectAsync(item.ObjectId);
        }
        finally
        {
            _suppressGroupSelection = false;
        }
    }

    private void ClearSiblingListSelections(ListBox keep)
    {
        foreach (var other in FindVisualChildren<ListBox>(LstObjectGroups))
        {
            if (!ReferenceEquals(other, keep) && other.SelectedItem is not null)
            {
                other.SelectedItem = null;
            }
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent is null)
        {
            yield break;
        }

        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var nested in FindVisualChildren<T>(child))
            {
                yield return nested;
            }
        }
    }

    private async Task ShowObjectAsync(string objectId)
    {
        var obj = await _inventoryService.GetObjectAsync(objectId);
        if (obj is null)
        {
            return;
        }

        var estado = TryReadEstado(obj.SummaryJson);
        var titleCore = obj.SchemaName is null ? obj.Name : obj.SchemaName + "." + obj.Name;
        TxtObjectTitle.Text = estado is null
            ? $"{FriendlyKind(obj.Kind)} · {titleCore}"
            : $"{FriendlyKind(obj.Kind)} · {titleCore} · {estado}";
        var metaParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(obj.ParentObjectId))
        {
            metaParts.Add(obj.ParentObjectId);
        }

        metaParts.Add(obj.ObjectId);
        TxtObjectMeta.Text = string.Join("  ·  ", metaParts);

        string? text;
        if (!string.IsNullOrWhiteSpace(obj.DefinitionArtifactId))
        {
            text = await _inventoryService.GetArtifactTextAsync(obj.DefinitionArtifactId);
            CodeEditor.Text = text ?? string.Empty;
        }
        else
        {
            text = obj.SummaryJson ?? "(sem definição SQL / apenas metadados)";
            CodeEditor.Text = text;
        }

        DarkSyntaxHighlighting.SetLanguage(CodeEditor, obj.Kind, text);
        JumpToSearchInCode(text);
    }

    private void ClearObjectDetail()
    {
        TxtObjectTitle.Text = "Seleccione uma categoria e depois um objecto";
        TxtObjectMeta.Text = string.Empty;
        if (TxtFindStatus is not null)
        {
            TxtFindStatus.Text = string.Empty;
        }

        CodeEditor.Text = string.Empty;
    }

    private void JumpToSearchInCode(string? text)
    {
        if (TxtFindStatus is not null)
        {
            TxtFindStatus.Text = string.Empty;
        }

        var term = !string.IsNullOrWhiteSpace(TxtSearch.Text) ? TxtSearch.Text.Trim() : _lastListSearch;
        if (string.IsNullOrWhiteSpace(term) || string.IsNullOrEmpty(text))
        {
            return;
        }

        var idx = text.IndexOf(term, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            if (TxtFindStatus is not null)
            {
                TxtFindStatus.Text = $"“{term}” não encontrado no código";
            }

            return;
        }

        CodeEditor.Select(idx, term.Length);
        CodeEditor.ScrollToLine(CodeEditor.Document.GetLineByOffset(idx).LineNumber);
        if (TxtFindStatus is not null)
        {
            var count = 0;
            var pos = 0;
            while ((pos = text.IndexOf(term, pos, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                count++;
                pos += term.Length;
            }

            TxtFindStatus.Text = count == 1
                ? $"1 ocorrência de “{term}”"
                : $"{count} ocorrências de “{term}” · Ctrl+F para navegar";
        }
    }

    private void BtnCopyAll_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(CodeEditor.Text))
        {
            Clipboard.SetText(CodeEditor.Text);
        }
    }

    private void BtnCopySelection_Click(object sender, RoutedEventArgs e)
    {
        var selected = CodeEditor.SelectedText;
        if (!string.IsNullOrEmpty(selected))
        {
            Clipboard.SetText(selected);
        }
    }

    private async void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        if (_scanService.CurrentWorkspace is null)
        {
            MessageBox.Show("Execute primeiro uma recolha.", "Exportação", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "ZIP (*.zip)|*.zip",
            FileName = $"phc-personalizacoes-{DateTime.Now:yyyyMMdd-HHmmss}.zip"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            TxtExportStatus.Text = "A exportar...";
            await _exportService.ExportCurrentAsync(dialog.FileName);
            TxtExportStatus.Text = "Exportado: " + dialog.FileName;
        }
        catch (Exception ex)
        {
            TxtExportStatus.Text = ex.Message;
        }
    }

    private async void BtnExportIkp_Click(object sender, RoutedEventArgs e)
    {
        var workspace = _scanService.CurrentWorkspace;
        if (workspace is null)
        {
            MessageBox.Show("Execute primeiro uma recolha.", "Exportação IKP", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_lastRequest is null || string.IsNullOrWhiteSpace(_lastRequest.Database))
        {
            MessageBox.Show("Não foi possível determinar a base de dados de origem desta captura.", "Exportação IKP", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "IKP 0.1 (*.zip)|*.zip",
            FileName = $"phc-capture-ikp-{DateTime.Now:yyyyMMdd-HHmmss}.zip"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            TxtExportStatus.Text = "A validar captura e a gerar pacote IKP 0.1...";
            var metadata = await workspace.GetCaptureMetadataAsync();
            if (!metadata.HasCompleteCoverageMetadata || string.IsNullOrWhiteSpace(metadata.CaptureId))
                throw new InvalidDataException("A captura não contém CaptureId, capabilities e coverage completos. Não é possível gerar IKP.");

            var result = await new IkpPackageExporter().ExportAsync(
                workspace,
                new IkpCaptureDescriptor(metadata.CaptureId, null, null, null, null),
                new IkpOriginDescriptor(DatabaseName: _lastRequest.Database),
                dialog.FileName);

            TxtExportStatus.Text =
                $"IKP 0.1 exportado e validado.\nPackageId: {result.PackageId}\nCaptureId: {result.CaptureId}\n" +
                $"SHA-256: {result.ZipSha256}\nFicheiros: {result.PackageFileCount} · Artefactos: {result.PayloadArtifactCount}\n{result.ZipPath}";
        }
        catch (Exception ex)
        {
            TxtExportStatus.Text = "Falha na exportação IKP: " + ex.Message;
            MessageBox.Show(ex.Message, "Exportação IKP", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnSchemaSnapshot_Click(object sender, RoutedEventArgs e)
    {
        var request = BuildRequest();
        if (string.IsNullOrWhiteSpace(request.Database))
        {
            MessageBox.Show("Indique a base de dados na página Ligação.", "Schema Snapshot", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "ZIP schema snapshot (*.zip)|*.zip",
            FileName = $"database-schema-{DateTime.Now:yyyyMMdd-HHmmss}.zip"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var includeSql = ChkSchemaIncludeSql.IsChecked == true;
        var options = new SchemaSnapshotOptions
        {
            IncludeSqlDefinitions = includeSql,
            FailOnPrivacyFindings = !includeSql
        };

        try
        {
            TxtSchemaSnapshotStatus.Text = "A gerar schema snapshot...";
            var result = await _schemaSnapshotService.ExportZipAsync(request, dialog.FileName, options);
            if (!result.Succeeded)
            {
                TxtSchemaSnapshotStatus.Text = "Bloqueado por privacidade: "
                    + string.Join("; ", result.Privacy.Findings.Select(f => f.Message));
                MessageBox.Show(
                    "A exportação foi bloqueada pela validação de privacidade. Remova definições SQL ou revise o conteúdo.",
                    "Schema Snapshot",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            TxtSchemaSnapshotStatus.Text =
                $"Exportado: {result.ZipPath} · hash={result.SnapshotHash} · privacyOK={result.Privacy.IsSafe}";
        }
        catch (Exception ex)
        {
            TxtSchemaSnapshotStatus.Text = ex.Message;
            MessageBox.Show(ex.Message, "Schema Snapshot", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RefreshViewsAsync()
    {
        var summary = await _inventoryService.GetSummaryAsync();
        if (summary is null)
        {
            DashboardPresenter.RenderEmpty(
                TxtDashInstallation,
                TxtDashStatus,
                TxtDashBadge,
                BadgeDashStatus,
                PanelKpiCards,
                PanelChartActivity,
                PanelChartPersonalizations,
                PanelChartStructure,
                TxtIssuesSummary,
                LstIssues);
            GridRelations.ItemsSource = null;
            TxtAnalysisEmpty.Visibility = Visibility.Visible;
            return;
        }

        var issues = await _inventoryService.GetIssuesAsync();
        var issueLines = issues.Select(i => $"[{i.Severity}] {i.Code}: {i.Message}" +
                                            (i.ObjectName is null ? string.Empty : $" ({i.ObjectName})")).ToList();
        var allObjects = await _inventoryService.SearchObjectsAsync(null, null);

        DashboardPresenter.Render(
            summary,
            issueLines,
            allObjects,
            TxtDashInstallation,
            TxtDashStatus,
            TxtDashBadge,
            BadgeDashStatus,
            PanelKpiCards,
            PanelChartActivity,
            PanelChartPersonalizations,
            PanelChartStructure,
            TxtIssuesSummary,
            LstIssues,
            FriendlyKind);

        var relations = (await _inventoryService.GetRelationsAsync())
            .Where(r => !string.Equals(r.RelationType, "inference.schema.system_like", StringComparison.OrdinalIgnoreCase))
            .ToList();
        GridRelations.ItemsSource = relations;
        TxtAnalysisEmpty.Visibility = relations.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        await LoadObjectsAsync();
    }

    private async Task LoadObjectsAsync()
    {
        var search = string.IsNullOrWhiteSpace(TxtSearch.Text) ? null : TxtSearch.Text.Trim();
        _lastListSearch = search;

        var kind = (CmbObjectKind.SelectedItem as ComboBoxItem)?.Tag as string;
        if (string.IsNullOrWhiteSpace(kind))
        {
            kind = null;
        }

        // Com texto de pesquisa e "todos os tipos": atravessa tipos (desenvolvimento completo).
        // Com tipo escolhido: pesquisa + tipo em conjunto.
        var queryKind = kind;
        if (!string.IsNullOrWhiteSpace(search) && kind is null)
        {
            queryKind = null;
        }

        var estadoFilter = (CmbEstado.SelectedItem as ComboBoxItem)?.Tag as string;
        if (string.IsNullOrWhiteSpace(estadoFilter))
        {
            estadoFilter = null;
        }

        var sort = (CmbSort.SelectedItem as ComboBoxItem)?.Tag as string ?? "kind";

        var objects = await _inventoryService.SearchObjectsAsync(queryKind, search);
        var filtered = objects
            .Where(IncludeInExplorer)
            .Select(o =>
            {
                var estado = TryReadEstado(o.SummaryJson);
                return (Obj: o, Estado: estado);
            })
            .Where(x => estadoFilter is null
                        || string.Equals(x.Estado, estadoFilter, StringComparison.OrdinalIgnoreCase))
            .ToList();

        filtered = sort switch
        {
            "name" => filtered
                .OrderBy(x => x.Obj.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Obj.Kind, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            "inactive" => filtered
                .OrderByDescending(x => string.Equals(x.Estado, "Inactivo", StringComparison.OrdinalIgnoreCase))
                .ThenBy(x => FriendlyKind(x.Obj.Kind), StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Obj.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            _ => filtered
                .OrderBy(x => FriendlyKind(x.Obj.Kind), StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Obj.SchemaName ?? "", StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Obj.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };

        // Accordion: categorias fechadas por defeito.
        // Só abre com filtro de tipo, texto de pesquisa, ou uma única categoria.
        var expandAllMatches = !string.IsNullOrWhiteSpace(search);
        var expandSpecificKind = kind is not null;
        var kindCount = filtered.Select(x => x.Obj.Kind).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var expandSingleKind = kindCount == 1;

        var grouped = filtered
            .GroupBy(x => x.Obj.Kind)
            .OrderBy(g => FriendlyKind(g.Key), StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var items = g.Select(x =>
                {
                    var scope = x.Obj.SchemaName;
                    var title = string.IsNullOrWhiteSpace(scope) ? x.Obj.Name : $"{scope} · {x.Obj.Name}";
                    var display = x.Estado is null ? title : $"{title} · {x.Estado}";
                    return new ObjectListItem(x.Obj.ObjectId, display);
                }).ToList();

                var expand = expandSpecificKind || expandAllMatches || expandSingleKind;
                return new ExplorerGroup(FriendlyKind(g.Key), g.Key, items, expand);
            })
            .ToList();

        _groups.Clear();
        foreach (var group in grouped)
        {
            _groups.Add(group);
        }

        ClearObjectDetail();

        if (TxtResultCount is not null)
        {
            var total = filtered.Count;
            var parts = new List<string>
            {
                $"{_groups.Count} categorias",
                $"{total} objectos"
            };
            if (search is not null)
            {
                parts.Add($"texto “{search}”");
            }

            if (kind is not null)
            {
                parts.Add(FriendlyKind(kind));
            }

            if (estadoFilter is not null)
            {
                parts.Add(estadoFilter);
            }

            TxtResultCount.Text = string.Join(" · ", parts);
        }
    }

    /// <summary>
    /// Explorador: só personalizações + estrutura custom (<c>u_*</c>).
    /// Sem schemas, índices, colunas estruturais em massa, nem dbo.campos.
    /// </summary>
    private static bool IncludeInExplorer(TechnicalObjectRecord o)
    {
        if (o.Kind is ObjectKind.Index or ObjectKind.Schema or ObjectKind.Column
            or ObjectKind.PhcCampoDefinition or ObjectKind.Database)
        {
            return false;
        }

        if (o.Kind is ObjectKind.Table or ObjectKind.PrimaryKey
            or ObjectKind.ForeignKey or ObjectKind.Trigger)
        {
            var parts = o.ObjectId.Split(':', 2);
            var path = parts.Length == 2 ? parts[1] : o.ObjectId;
            var tablePart = o.Kind == ObjectKind.Table
                ? o.Name
                : path.Split('.') is { Length: >= 2 } segs ? segs[1] : o.Name;

            return tablePart.StartsWith("u_", StringComparison.OrdinalIgnoreCase);
        }

        // Campos de utilizador = colunas u_* (já são não-padrão PHC).
        if (o.Kind == ObjectKind.PhcUserField)
        {
            return o.Name.StartsWith("u_", StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    private static string? TryReadEstado(string? summaryJson)
    {
        if (string.IsNullOrWhiteSpace(summaryJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(summaryJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("estado", out var estadoEl)
                && estadoEl.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(estadoEl.GetString()))
            {
                return estadoEl.GetString();
            }

            foreach (var prop in new[] { "inactivo", "inativo" })
            {
                if (!root.TryGetProperty(prop, out var flag))
                {
                    continue;
                }

                var inactive = flag.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Number => flag.TryGetInt64(out var n) ? n != 0 : flag.GetDouble() != 0,
                    JsonValueKind.String => IsInactiveString(flag.GetString()),
                    _ => false
                };
                return inactive ? "Inactivo" : "Activo";
            }
        }
        catch
        {
            // metadados opcionais
        }

        return null;
    }

    private static bool IsInactiveString(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        s = s.Trim();
        return s.Equals("true", StringComparison.OrdinalIgnoreCase)
               || s.Equals("1", StringComparison.Ordinal)
               || s.Equals(".t.", StringComparison.OrdinalIgnoreCase);
    }

    private static string FriendlyKind(string kind) => kind switch
    {
        "phc.event" => "Evento",
        "phc.web_event" => "Evento Web",
        "phc.user_rule" => "Regra",
        "phc.web_user_rule" => "Regra Web",
        "phc.default_value" => "Valor por defeito",
        "phc.web_default_value" => "Valor por defeito Web",
        "phc.user_field" => "Campo u_*",
        "phc.hotkey" => "Tecla",
        "phc.web_script" => "Script Web",
        "phc.user_javascript" => "JavaScript utilizador",
        "phc.web_cssjs" => "CSS/JS Web",
        "phc.hidden_grid" => "Grelha oculta",
        "phc.custom_screen" => "Ecrã personalizado",
        "phc.user_tsql_function" => "Função T-SQL",
        "table" => "Tabelas u_*",
        "view" => "Views",
        "trigger" => "Triggers",
        "stored_procedure" => "Procedures",
        "foreign_key" => "FK",
        "primary_key" => "PK",
        _ => kind
    };

    internal void OnProgress(ScanProgressEvent progress)
    {
        Dispatcher.Invoke(() =>
        {
            TxtProgressStage.Text = progress.Stage;
            TxtProgressDetail.Text =
                $"{progress.CollectorDisplayName ?? "(orquestração)"} — {progress.Message}";
            if (progress.PercentComplete is double p)
            {
                ProgressBarScan.Value = p;
            }

            TxtStatProcessed.Text = $"Processados: {progress.ObjectsProcessed}";
            TxtStatSuccess.Text = $"Sucessos: {progress.SuccessCount}";
            TxtStatWarnings.Text = $"Avisos: {progress.WarningCount}";
            TxtStatErrors.Text = $"Falhas: {progress.ErrorCount}";
            TxtProgressElapsed.Text = $"Duração: {progress.Elapsed:hh\\:mm\\:ss}";
            AppendLog($"[{progress.Elapsed:mm\\:ss}] {progress.Message}");
        }, DispatcherPriority.Background);
    }

    private void AppendLog(string line)
    {
        _progressLog.Insert(0, line);
        while (_progressLog.Count > 500)
        {
            _progressLog.RemoveAt(_progressLog.Count - 1);
        }
    }

    private sealed class UiProgressSink(MainWindow window) : IScanProgressSink
    {
        public void Report(ScanProgressEvent progress) => window.OnProgress(progress);
    }

    private sealed record ObjectListItem(string ObjectId, string Display);

    private sealed record InventoryTableListItem(string ObjectId, string Display);

    private sealed class ExplorerGroup : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isExpanded;

        public ExplorerGroup(string title, string kind, IReadOnlyList<ObjectListItem> items, bool isExpanded)
        {
            Title = title;
            Kind = kind;
            Items = items;
            _isExpanded = isExpanded;
        }

        public string Title { get; }
        public string Kind { get; }
        public IReadOnlyList<ObjectListItem> Items { get; }
        public int Count => Items.Count;

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value)
                {
                    return;
                }

                _isExpanded = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsExpanded)));
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }
}
