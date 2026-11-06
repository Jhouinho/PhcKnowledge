using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Coverage;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Progress;
using PhcKnowledgeScanner.Collectors.Coverage;

namespace PhcKnowledgeScanner.Collectors.Phc;

/// <summary>
/// Recolhe personalizações PHC (regras, eventos, valores por defeito, etc.).
/// Contrato de campos/SQL alinhado com <c>PhcCustomizationCodeExport/CaptureExporter.cs</c>.
/// </summary>
public sealed class PhcCustomizationsCollector : ICollector
{
    public const string CollectorId = "phc.customizations";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public string Id => CollectorId;
    public string DisplayName => "Personalizações PHC";
    public IReadOnlyList<DeclaredCapability> DeclaredCapabilities => ScannerCapabilityCatalog.ForPhcCustomizations;

    public async Task<CollectorCapability> ProbeAsync(ScanContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context.SqlConnectionString))
        {
            return new CollectorCapability(false, "Connection string SQL em falta no contexto.");
        }

        try
        {
            await using var connection = new SqlConnection(context.SqlConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // Pelo menos uma tabela-core de personalização Desktop.
            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = N'dbo'
                  AND TABLE_NAME IN (N'brule', N'eventos', N'udefs');
                """;
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
            if (count == 0)
            {
                return new CollectorCapability(false, "Tabelas PHC de personalização (brule/eventos/udefs) não encontradas.");
            }

            return new CollectorCapability(true);
        }
        catch (Exception ex)
        {
            return new CollectorCapability(false, "Não foi possível abrir ligação SQL: " + ex.Message);
        }
    }

    public async Task<CollectorResult> CollectAsync(ScanContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context.Workspace);

        var sw = Stopwatch.StartNew();
        var issues = new List<CollectionIssue>();
        var collectedAt = DateTimeOffset.UtcNow;
        var objectCount = 0;

        await using var connection = new SqlConnection(context.SqlConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var tracker = new OperationCoverageTracker(issues);
        var caps = DeclaredCapabilities.ToDictionary(c => c.OperationId, StringComparer.OrdinalIgnoreCase);

        async Task Report(string message)
        {
            context.Progress?.Report(new ScanProgressEvent(
                "collector.running",
                Id,
                DisplayName,
                message,
                objectCount,
                objectCount,
                issues.Count(i => i.Severity == CollectionIssueSeverity.Warning),
                issues.Count(i => i.Severity == CollectionIssueSeverity.Error),
                null,
                sw.Elapsed));
            await Task.CompletedTask;
        }

        Task<bool> Exists(string table) => TableExistsAsync(connection, table, cancellationToken);

        await tracker.RunAsync(
                caps[KnownOperations.PhcBrule],
                () => CollectBruleAsync(connection, context, collectedAt, issues, () => objectCount++, Report, cancellationToken),
                () => objectCount)
            .ConfigureAwait(false);
        await tracker.RunOptionalAsync(
                caps[KnownOperations.PhcEbrule],
                () => Exists("ebrule"),
                () => CollectEbruleAsync(connection, context, collectedAt, issues, () => objectCount++, Report, cancellationToken),
                () => objectCount,
                "dbo.ebrule")
            .ConfigureAwait(false);
        await tracker.RunAsync(
                caps[KnownOperations.PhcTeclas],
                () => CollectTeclasAsync(connection, context, collectedAt, issues, () => objectCount++, Report, cancellationToken),
                () => objectCount)
            .ConfigureAwait(false);
        await tracker.RunAsync(
                caps[KnownOperations.PhcUdefs],
                () => CollectUdefsAsync(connection, context, collectedAt, issues, () => objectCount++, Report, cancellationToken),
                () => objectCount)
            .ConfigureAwait(false);
        await tracker.RunOptionalAsync(
                caps[KnownOperations.PhcEudefs],
                () => Exists("eudefs"),
                () => CollectEudefsAsync(connection, context, collectedAt, issues, () => objectCount++, Report, cancellationToken),
                () => objectCount,
                "dbo.eudefs")
            .ConfigureAwait(false);
        await tracker.RunAsync(
                caps[KnownOperations.PhcEventos],
                () => CollectEventosAsync(connection, context, collectedAt, issues, () => objectCount++, Report, cancellationToken),
                () => objectCount)
            .ConfigureAwait(false);
        await tracker.RunOptionalAsync(
                caps[KnownOperations.PhcEeventos],
                () => Exists("eeventos"),
                () => CollectEeventosAsync(connection, context, collectedAt, issues, () => objectCount++, Report, cancellationToken),
                () => objectCount,
                "dbo.eeventos")
            .ConfigureAwait(false);
        await tracker.RunAsync(
                caps[KnownOperations.PhcUserFields],
                () => CollectUserFieldsAsync(connection, context, collectedAt, issues, () => objectCount++, Report, cancellationToken),
                () => objectCount)
            .ConfigureAwait(false);
        await tracker.RunOptionalAsync(
                caps[KnownOperations.PhcEscr],
                () => Exists("escr"),
                () => CollectEscrAsync(connection, context, collectedAt, issues, () => objectCount++, Report, cancellationToken),
                () => objectCount,
                "dbo.escr")
            .ConfigureAwait(false);
        await tracker.RunOptionalAsync(
                caps[KnownOperations.PhcJsu],
                () => Exists("jsu"),
                () => CollectJsuAsync(connection, context, collectedAt, issues, () => objectCount++, Report, cancellationToken),
                () => objectCount,
                "dbo.jsu")
            .ConfigureAwait(false);
        await tracker.RunOptionalAsync(
                caps[KnownOperations.PhcCssjs],
                () => Exists("cssjs"),
                () => CollectCssjsAsync(connection, context, collectedAt, issues, () => objectCount++, Report, cancellationToken),
                () => objectCount,
                "dbo.cssjs")
            .ConfigureAwait(false);
        await tracker.RunOptionalAsync(
                caps[KnownOperations.PhcFormc],
                () => Exists("formc"),
                () => CollectFormcHiddenAsync(connection, context, collectedAt, issues, () => objectCount++, Report, cancellationToken),
                () => objectCount,
                "dbo.formc")
            .ConfigureAwait(false);
        await tracker.RunOptionalAsync(
                caps[KnownOperations.PhcFormb],
                () => Exists("formb"),
                () => CollectFormbAsync(connection, context, collectedAt, issues, () => objectCount++, Report, cancellationToken),
                () => objectCount,
                "dbo.formb")
            .ConfigureAwait(false);
        await tracker.RunOptionalAsync(
                caps[KnownOperations.PhcFtu],
                () => Exists("ftu"),
                () => CollectFtuAsync(connection, context, collectedAt, issues, () => objectCount++, Report, cancellationToken),
                () => objectCount,
                "dbo.ftu")
            .ConfigureAwait(false);

        foreach (var issue in issues)
        {
            await context.Workspace.AddIssueAsync(Id, issue, cancellationToken).ConfigureAwait(false);
        }

        var status = issues.Any(i => i.Severity == CollectionIssueSeverity.Error)
            ? (objectCount > 0 ? CollectorStatus.Partial : CollectorStatus.Failed)
            : issues.Any(i => i.Severity == CollectionIssueSeverity.Warning)
                ? CollectorStatus.SucceededWithWarnings
                : CollectorStatus.Succeeded;

        return new CollectorResult(Id, status, Array.Empty<CollectedArtifact>(), issues, sw.Elapsed, tracker.Coverages);
    }

    private static async Task CollectBruleAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        List<CollectionIssue> issues,
        Action bump,
        Func<string, Task> report,
        CancellationToken ct)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 300;
            cmd.CommandText =
                """
                SELECT RTRIM(brulestamp) AS stamp, RTRIM(ISNULL(tabela,'')) AS tabela,
                       CAST(ISNULL(expressao,'') AS nvarchar(max)) AS expressao,
                       CAST(ISNULL(mensagem,'') AS nvarchar(max)) AS mensagem,
                       CAST(ISNULL(descricao,'') AS nvarchar(max)) AS descricao,
                       ISNULL(inactivo,0) AS inactivo
                FROM dbo.brule
                ORDER BY tabela, ordem, brulestamp
                """;

            var batch = new List<TechnicalObjectRecord>(200);
            var count = 0;
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                var stamp = S(reader, 0);
                var tabela = S(reader, 1);
                var expressao = S(reader, 2);
                var mensagem = S(reader, 3);
                var descricao = S(reader, 4);
                var inactivo = reader.GetValue(5)?.ToString() ?? "0";
                if (string.IsNullOrWhiteSpace(expressao))
                {
                    continue;
                }

                var body = new StringBuilder();
                body.AppendLine("* BRULE");
                body.AppendLine($"* stamp: {stamp}");
                body.AppendLine($"* tabela: {tabela}");
                body.AppendLine($"* mensagem: {mensagem}");
                body.AppendLine($"* descricao: {descricao}");
                body.AppendLine($"* estado: {PhcActivityState.ToEstado(inactivo)}");
                body.AppendLine($"* inactivo: {(PhcActivityState.IsInactive(inactivo) ? "True" : "False")}");
                body.AppendLine();
                body.AppendLine(expressao.TrimEnd());
                body.AppendLine();

                var artifactId = await SaveDefinitionAsync(
                    context, $"phc/brule/{Sanitize(stamp)}.prg", body.ToString(), ct).ConfigureAwait(false);

                batch.Add(new TechnicalObjectRecord(
                    $"phc.brule:{stamp}",
                    ObjectKind.PhcUserRule,
                    PhcActivityState.WithInactiveSuffix(string.IsNullOrWhiteSpace(mensagem) ? stamp : mensagem, inactivo),
                    string.IsNullOrWhiteSpace(tabela) ? null : tabela,
                    null,
                    CollectorId,
                    artifactId,
                    JsonSerializer.Serialize(new { source = "brule", stamp, tabela, inactivo, estado = PhcActivityState.ToEstado(inactivo) }, JsonOptions),
                    collectedAt));
                bump();
                count++;
                if (batch.Count >= 100)
                {
                    await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
            }

            await report($"brule: {count}.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            issues.Add(Issue("phc.brule", "Não foi possível recolher brule.", ex));
        }
    }

    private static async Task CollectEbruleAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        List<CollectionIssue> issues,
        Action bump,
        Func<string, Task> report,
        CancellationToken ct)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 300;
            cmd.CommandText =
                """
                SELECT RTRIM(ebrulestamp) AS stamp,
                       RTRIM(ISNULL(tabela,'')) AS tabela,
                       RTRIM(ISNULL(ecran,'')) AS ecran,
                       CAST(ISNULL(expressao,'') AS nvarchar(max)) AS expressao,
                       CAST(ISNULL(mensagem,'') AS nvarchar(max)) AS mensagem,
                       CAST(ISNULL(descricao,'') AS nvarchar(max)) AS descricao,
                       ISNULL(grau,0) AS grau,
                       ISNULL(inactivo,0) AS inactivo
                FROM dbo.ebrule
                ORDER BY tabela, ecran, ordem, ebrulestamp
                """;

            var batch = new List<TechnicalObjectRecord>(200);
            var count = 0;
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                var stamp = S(reader, 0);
                var tabela = S(reader, 1);
                var ecran = S(reader, 2);
                var expressao = S(reader, 3);
                var mensagem = S(reader, 4);
                var descricao = S(reader, 5);
                var grau = reader.GetValue(6)?.ToString() ?? "0";
                var inactivo = reader.GetValue(7)?.ToString() ?? "0";
                if (string.IsNullOrWhiteSpace(expressao))
                {
                    continue;
                }

                var body = new StringBuilder();
                body.AppendLine("* EBRULE (Web)");
                body.AppendLine($"* stamp: {stamp}");
                body.AppendLine($"* tabela: {tabela}");
                body.AppendLine($"* ecran: {ecran}");
                body.AppendLine($"* mensagem: {mensagem}");
                body.AppendLine($"* descricao: {descricao}");
                body.AppendLine($"* grau: {grau}");
                body.AppendLine($"* estado: {PhcActivityState.ToEstado(inactivo)}");
                body.AppendLine($"* inactivo: {(PhcActivityState.IsInactive(inactivo) ? "True" : "False")}");
                body.AppendLine();
                body.AppendLine(expressao.TrimEnd());
                body.AppendLine();

                var artifactId = await SaveDefinitionAsync(
                    context, $"phc/ebrule/{Sanitize(stamp)}.prg", body.ToString(), ct).ConfigureAwait(false);

                batch.Add(new TechnicalObjectRecord(
                    $"phc.ebrule:{stamp}",
                    ObjectKind.PhcWebUserRule,
                    PhcActivityState.WithInactiveSuffix(string.IsNullOrWhiteSpace(mensagem) ? stamp : mensagem, inactivo),
                    string.IsNullOrWhiteSpace(tabela) ? ecran : tabela,
                    null,
                    CollectorId,
                    artifactId,
                    JsonSerializer.Serialize(new { source = "ebrule", stamp, tabela, ecran, inactivo, estado = PhcActivityState.ToEstado(inactivo) }, JsonOptions),
                    collectedAt));
                bump();
                count++;
                if (batch.Count >= 100)
                {
                    await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
            }

            await report($"ebrule: {count}.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            issues.Add(Issue("phc.ebrule", "Não foi possível recolher ebrule.", ex));
        }
    }

    private static async Task CollectTeclasAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        List<CollectionIssue> issues,
        Action bump,
        Func<string, Task> report,
        CancellationToken ct)
    {
        try
        {
            if (!await TableExistsAsync(connection, "ti", ct).ConfigureAwait(false)
                || !await TableExistsAsync(connection, "tc", ct).ConfigureAwait(false))
            {
                await report("teclas: tabelas ti/tc ausentes (ignorado).").ConfigureAwait(false);
                return;
            }

            await using var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 300;
            cmd.CommandText =
                """
                SELECT RTRIM(ti.tistamp) AS tistamp,
                       RTRIM(ISNULL(tc.ecran,'')) AS ecran,
                       RTRIM(ISNULL(ti.teclas,'')) AS teclas,
                       CAST(ISNULL(ti.retorno,'') AS nvarchar(max)) AS retorno,
                       CAST(ISNULL(ti.descricao,'') AS nvarchar(max)) AS descricao,
                       ISNULL(ti.inactivo,0) AS inactivo
                FROM dbo.ti
                INNER JOIN dbo.tc ON ti.tcstamp = tc.tcstamp
                ORDER BY tc.ecran, ti.teclas, ti.tistamp
                """;

            var batch = new List<TechnicalObjectRecord>(200);
            var count = 0;
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                var stamp = S(reader, 0);
                var ecran = S(reader, 1);
                var teclas = S(reader, 2);
                var retorno = S(reader, 3);
                var descricao = S(reader, 4);
                var inactivo = reader.GetValue(5)?.ToString() ?? "0";
                if (string.IsNullOrWhiteSpace(retorno))
                {
                    continue;
                }

                var body = new StringBuilder();
                body.AppendLine("* TECLA (ti.retorno)");
                body.AppendLine($"* tistamp: {stamp}");
                body.AppendLine($"* ecran: {ecran}");
                body.AppendLine($"* teclas: {teclas}");
                body.AppendLine($"* descricao: {descricao}");
                body.AppendLine($"* estado: {PhcActivityState.ToEstado(inactivo)}");
                body.AppendLine($"* inactivo: {(PhcActivityState.IsInactive(inactivo) ? "True" : "False")}");
                body.AppendLine();
                body.AppendLine(retorno.TrimEnd());
                body.AppendLine();

                var artifactId = await SaveDefinitionAsync(
                    context, $"phc/teclas/{Sanitize(stamp)}.prg", body.ToString(), ct).ConfigureAwait(false);

                var name = string.IsNullOrWhiteSpace(descricao)
                    ? (string.IsNullOrWhiteSpace(teclas) ? stamp : teclas)
                    : descricao;

                batch.Add(new TechnicalObjectRecord(
                    $"phc.tecla:{stamp}",
                    ObjectKind.PhcHotkey,
                    PhcActivityState.WithInactiveSuffix(name, inactivo),
                    string.IsNullOrWhiteSpace(ecran) ? null : ecran,
                    null,
                    CollectorId,
                    artifactId,
                    JsonSerializer.Serialize(new { source = "ti", stamp, ecran, teclas, inactivo, estado = PhcActivityState.ToEstado(inactivo) }, JsonOptions),
                    collectedAt));
                bump();
                count++;
                if (batch.Count >= 100)
                {
                    await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
            }

            await report($"teclas: {count}.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            issues.Add(Issue("phc.teclas", "Não foi possível recolher teclas (ti/tc).", ex));
        }
    }

    private static async Task CollectUdefsAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        List<CollectionIssue> issues,
        Action bump,
        Func<string, Task> report,
        CancellationToken ct)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 300;
            cmd.CommandText =
                """
                SELECT RTRIM(udefsstamp) AS stamp,
                       RTRIM(ISNULL(tabela,'')) AS tabela,
                       RTRIM(ISNULL(campo,'')) AS campo,
                       CAST(ISNULL(descricao,'') AS nvarchar(max)) AS descricao,
                       ISNULL(tipo,0) AS tipo,
                       CAST(ISNULL(xbase,'') AS nvarchar(max)) AS xbase,
                       CAST(ISNULL(transact,'') AS nvarchar(max)) AS transact,
                       CAST(ISNULL(defsql,'') AS nvarchar(max)) AS defsql,
                       CAST(ISNULL(valorc,'') AS nvarchar(max)) AS valorc,
                       ISNULL(quando,0) AS quando,
                       ISNULL(inactivo,0) AS inactivo
                FROM dbo.udefs
                ORDER BY tabela, campo, udefsstamp
                """;

            var batch = new List<TechnicalObjectRecord>(200);
            var count = 0;
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                var stamp = S(reader, 0);
                var tabela = S(reader, 1);
                var campo = S(reader, 2);
                var descricao = S(reader, 3);
                var tipo = reader.GetValue(4)?.ToString() ?? "0";
                var xbase = S(reader, 5);
                var transact = S(reader, 6);
                var defsql = S(reader, 7);
                var valorc = S(reader, 8);
                var quando = reader.GetValue(9)?.ToString() ?? "0";
                var inactivo = reader.GetValue(10)?.ToString() ?? "0";

                var parts = new (string Col, string Body)[]
                {
                    ("xbase", xbase),
                    ("transact", transact),
                    ("defsql", defsql),
                    ("valorc", valorc),
                };
                if (parts.All(p => string.IsNullOrWhiteSpace(p.Body)))
                {
                    continue;
                }

                var body = new StringBuilder();
                body.AppendLine("* UDEFS");
                body.AppendLine($"* stamp: {stamp}");
                body.AppendLine($"* tabela: {tabela}");
                body.AppendLine($"* campo: {campo}");
                body.AppendLine($"* descricao: {descricao}");
                body.AppendLine($"* tipo: {tipo}");
                body.AppendLine($"* quando: {quando}");
                body.AppendLine($"* estado: {PhcActivityState.ToEstado(inactivo)}");
                body.AppendLine($"* inactivo: {(PhcActivityState.IsInactive(inactivo) ? "True" : "False")}");
                body.AppendLine();
                foreach (var (col, part) in parts)
                {
                    if (string.IsNullOrWhiteSpace(part))
                    {
                        continue;
                    }

                    body.AppendLine($"*=== {col.ToUpperInvariant()} ({col}) ===");
                    body.AppendLine(part.TrimEnd());
                    body.AppendLine();
                }

                var artifactId = await SaveDefinitionAsync(
                    context, $"phc/udefs/{Sanitize(stamp)}.prg", body.ToString(), ct).ConfigureAwait(false);

                var name = string.IsNullOrWhiteSpace(campo) ? stamp : $"{tabela}.{campo}".Trim('.');
                batch.Add(new TechnicalObjectRecord(
                    $"phc.udefs:{stamp}",
                    ObjectKind.PhcDefaultValue,
                    PhcActivityState.WithInactiveSuffix(name, inactivo),
                    string.IsNullOrWhiteSpace(tabela) ? null : tabela,
                    null,
                    CollectorId,
                    artifactId,
                    JsonSerializer.Serialize(new { source = "udefs", stamp, tabela, campo, inactivo, estado = PhcActivityState.ToEstado(inactivo) }, JsonOptions),
                    collectedAt));
                bump();
                count++;
                if (batch.Count >= 100)
                {
                    await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
            }

            await report($"udefs: {count}.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            issues.Add(Issue("phc.udefs", "Não foi possível recolher udefs.", ex));
        }
    }

    private static async Task CollectEudefsAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        List<CollectionIssue> issues,
        Action bump,
        Func<string, Task> report,
        CancellationToken ct)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 300;
            cmd.CommandText =
                """
                SELECT RTRIM(eudefsstamp) AS stamp,
                       RTRIM(ISNULL(tabela,'')) AS tabela,
                       RTRIM(ISNULL(campo,'')) AS campo,
                       RTRIM(ISNULL(ecran,'')) AS ecran,
                       CAST(ISNULL(descricao,'') AS nvarchar(max)) AS descricao,
                       ISNULL(tipo,0) AS tipo,
                       CAST(ISNULL(expvb,'') AS nvarchar(max)) AS expvb,
                       CAST(ISNULL(transact,'') AS nvarchar(max)) AS transact,
                       CAST(ISNULL(valorc,'') AS nvarchar(max)) AS valorc,
                       ISNULL(quando,0) AS quando,
                       ISNULL(inactivo,0) AS inactivo
                FROM dbo.eudefs
                ORDER BY tabela, campo, eudefsstamp
                """;

            var batch = new List<TechnicalObjectRecord>(200);
            var count = 0;
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                var stamp = S(reader, 0);
                var tabela = S(reader, 1);
                var campo = S(reader, 2);
                var ecran = S(reader, 3);
                var descricao = S(reader, 4);
                var tipo = reader.GetValue(5)?.ToString() ?? "0";
                var expvb = S(reader, 6);
                var transact = S(reader, 7);
                var valorc = S(reader, 8);
                var quando = reader.GetValue(9)?.ToString() ?? "0";
                var inactivo = reader.GetValue(10)?.ToString() ?? "0";

                var parts = new (string Col, string Body)[]
                {
                    ("expvb", expvb),
                    ("transact", transact),
                    ("valorc", valorc),
                };
                if (parts.All(p => string.IsNullOrWhiteSpace(p.Body)))
                {
                    continue;
                }

                var body = new StringBuilder();
                body.AppendLine("* EUDEFS (Web)");
                body.AppendLine($"* stamp: {stamp}");
                body.AppendLine($"* tabela: {tabela}");
                body.AppendLine($"* campo: {campo}");
                body.AppendLine($"* ecran: {ecran}");
                body.AppendLine($"* descricao: {descricao}");
                body.AppendLine($"* tipo: {tipo}");
                body.AppendLine($"* quando: {quando}");
                body.AppendLine($"* estado: {PhcActivityState.ToEstado(inactivo)}");
                body.AppendLine($"* inactivo: {(PhcActivityState.IsInactive(inactivo) ? "True" : "False")}");
                body.AppendLine();
                foreach (var (col, part) in parts)
                {
                    if (string.IsNullOrWhiteSpace(part))
                    {
                        continue;
                    }

                    body.AppendLine($"*=== {col.ToUpperInvariant()} ({col}) ===");
                    body.AppendLine(part.TrimEnd());
                    body.AppendLine();
                }

                var artifactId = await SaveDefinitionAsync(
                    context, $"phc/eudefs/{Sanitize(stamp)}.prg", body.ToString(), ct).ConfigureAwait(false);

                var name = string.IsNullOrWhiteSpace(campo) ? stamp : $"{tabela}.{campo}".Trim('.');
                batch.Add(new TechnicalObjectRecord(
                    $"phc.eudefs:{stamp}",
                    ObjectKind.PhcWebDefaultValue,
                    PhcActivityState.WithInactiveSuffix(name, inactivo),
                    string.IsNullOrWhiteSpace(tabela) ? ecran : tabela,
                    null,
                    CollectorId,
                    artifactId,
                    JsonSerializer.Serialize(new { source = "eudefs", stamp, tabela, campo, ecran, inactivo, estado = PhcActivityState.ToEstado(inactivo) }, JsonOptions),
                    collectedAt));
                bump();
                count++;
                if (batch.Count >= 100)
                {
                    await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
            }

            await report($"eudefs: {count}.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            issues.Add(Issue("phc.eudefs", "Não foi possível recolher eudefs.", ex));
        }
    }

    private static async Task CollectEventosAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        List<CollectionIssue> issues,
        Action bump,
        Func<string, Task> report,
        CancellationToken ct)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 300;
            cmd.CommandText =
                """
                SELECT RTRIM(eventosstamp) AS stamp,
                       CAST(ISNULL(resumo,'') AS nvarchar(max)) AS resumo,
                       RTRIM(ISNULL(ecran,'')) AS ecran,
                       RTRIM(ISNULL(qevento,'')) AS qevento,
                       ISNULL(reacao,0) AS reacao,
                       CAST(ISNULL(expressao,'') AS nvarchar(max)) AS expressao,
                       CAST(ISNULL(econdicao,'') AS nvarchar(max)) AS econdicao,
                       CAST(ISNULL(condicao,'') AS nvarchar(max)) AS condicao,
                       ISNULL(pdescricao,0) AS pdescricao,
                       ISNULL(inactivo,0) AS inactivo
                FROM dbo.eventos
                ORDER BY ecran, qevento, resumo
                """;

            var batch = new List<TechnicalObjectRecord>(200);
            var count = 0;
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                var stamp = S(reader, 0);
                var resumo = S(reader, 1);
                var ecran = S(reader, 2);
                var qevento = S(reader, 3);
                var reacao = reader.GetValue(4)?.ToString() ?? "0";
                var expressao = S(reader, 5);
                var econdicao = S(reader, 6);
                var condicao = S(reader, 7);
                var pdescricao = reader.GetValue(8)?.ToString() ?? "0";
                var inactivo = reader.GetValue(9)?.ToString() ?? "0";

                if (string.IsNullOrWhiteSpace(expressao)
                    && string.IsNullOrWhiteSpace(econdicao)
                    && string.IsNullOrWhiteSpace(condicao))
                {
                    continue;
                }

                var body = BuildEventBody("* EVENTO", stamp, resumo, ecran, qevento, reacao, pdescricao, inactivo,
                    expressao, econdicao, condicao);

                var artifactId = await SaveDefinitionAsync(
                    context, $"phc/eventos/{Sanitize(stamp)}.prg", body, ct).ConfigureAwait(false);

                var name = string.IsNullOrWhiteSpace(resumo)
                    ? (string.IsNullOrWhiteSpace(qevento) ? stamp : qevento)
                    : resumo;

                batch.Add(new TechnicalObjectRecord(
                    $"phc.event:{stamp}",
                    ObjectKind.PhcEvent,
                    PhcActivityState.WithInactiveSuffix(name, inactivo),
                    string.IsNullOrWhiteSpace(ecran) ? null : ecran,
                    null,
                    CollectorId,
                    artifactId,
                    JsonSerializer.Serialize(new { source = "eventos", stamp, ecran, qevento, inactivo, estado = PhcActivityState.ToEstado(inactivo) }, JsonOptions),
                    collectedAt));
                bump();
                count++;
                if (batch.Count >= 100)
                {
                    await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
            }

            await report($"eventos: {count}.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            issues.Add(Issue("phc.eventos", "Não foi possível recolher eventos.", ex));
        }
    }

    private static async Task CollectEeventosAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        List<CollectionIssue> issues,
        Action bump,
        Func<string, Task> report,
        CancellationToken ct)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 300;
            cmd.CommandText =
                """
                SELECT RTRIM(eeventosstamp) AS stamp,
                       CAST(ISNULL(resumo,'') AS nvarchar(max)) AS resumo,
                       RTRIM(ISNULL(ecran,'')) AS ecran,
                       RTRIM(ISNULL(qevento,'')) AS qevento,
                       ISNULL(reacao,0) AS reacao,
                       CAST(ISNULL(expressao,'') AS nvarchar(max)) AS expressao,
                       CAST(ISNULL(econdicao,'') AS nvarchar(max)) AS econdicao,
                       CAST(ISNULL(condicao,'') AS nvarchar(max)) AS condicao,
                       ISNULL(pdescricao,0) AS pdescricao,
                       ISNULL(inactivo,0) AS inactivo
                FROM dbo.eeventos
                ORDER BY ecran, qevento, resumo
                """;

            var batch = new List<TechnicalObjectRecord>(200);
            var count = 0;
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                var stamp = S(reader, 0);
                var resumo = S(reader, 1);
                var ecran = S(reader, 2);
                var qevento = S(reader, 3);
                var reacao = reader.GetValue(4)?.ToString() ?? "0";
                var expressao = S(reader, 5);
                var econdicao = S(reader, 6);
                var condicao = S(reader, 7);
                var pdescricao = reader.GetValue(8)?.ToString() ?? "0";
                var inactivo = reader.GetValue(9)?.ToString() ?? "0";

                if (string.IsNullOrWhiteSpace(expressao)
                    && string.IsNullOrWhiteSpace(econdicao)
                    && string.IsNullOrWhiteSpace(condicao))
                {
                    continue;
                }

                var body = BuildEventBody("* EEVENTO (Web)", stamp, resumo, ecran, qevento, reacao, pdescricao, inactivo,
                    expressao, econdicao, condicao);

                var artifactId = await SaveDefinitionAsync(
                    context, $"phc/eeventos/{Sanitize(stamp)}.prg", body, ct).ConfigureAwait(false);

                var name = string.IsNullOrWhiteSpace(resumo)
                    ? (string.IsNullOrWhiteSpace(qevento) ? stamp : qevento)
                    : resumo;

                batch.Add(new TechnicalObjectRecord(
                    $"phc.web_event:{stamp}",
                    ObjectKind.PhcWebEvent,
                    PhcActivityState.WithInactiveSuffix(name, inactivo),
                    string.IsNullOrWhiteSpace(ecran) ? null : ecran,
                    null,
                    CollectorId,
                    artifactId,
                    JsonSerializer.Serialize(new { source = "eeventos", stamp, ecran, qevento, inactivo, estado = PhcActivityState.ToEstado(inactivo) }, JsonOptions),
                    collectedAt));
                bump();
                count++;
                if (batch.Count >= 100)
                {
                    await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
            }

            await report($"eeventos: {count}.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            issues.Add(Issue("phc.eeventos", "Não foi possível recolher eeventos.", ex));
        }
    }

    /// <summary>
    /// Campos de utilizador PHC = colunas físicas cujo nome começa por <c>u_</c>.
    /// </summary>
    private static async Task CollectUserFieldsAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        List<CollectionIssue> issues,
        Action bump,
        Func<string, Task> report,
        CancellationToken ct)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 300;
            cmd.CommandText =
                """
                SELECT s.name AS schema_name,
                       t.name AS table_name,
                       c.name AS column_name,
                       ty.name AS type_name,
                       c.max_length,
                       c.precision,
                       c.scale,
                       c.is_nullable,
                       c.column_id
                FROM sys.columns c
                INNER JOIN sys.tables t ON t.object_id = c.object_id
                INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
                INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
                WHERE c.name LIKE N'u[_]%'
                ORDER BY s.name, t.name, c.column_id;
                """;

            var batch = new List<TechnicalObjectRecord>(500);
            var count = 0;
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                var schema = reader.GetString(0);
                var table = reader.GetString(1);
                var colName = reader.GetString(2);
                var typeName = reader.GetString(3);
                var maxLength = reader.GetInt16(4);
                var precision = reader.GetByte(5);
                var scale = reader.GetByte(6);
                var isNullable = reader.GetBoolean(7);
                var columnId = reader.GetInt32(8);

                var summary = JsonSerializer.Serialize(new
                {
                    source = "sys.columns",
                    prefix = "u_",
                    schema,
                    table,
                    column = colName,
                    type = typeName,
                    maxLength,
                    precision,
                    scale,
                    isNullable,
                    columnId
                }, JsonOptions);

                var body = new StringBuilder();
                body.AppendLine("* CAMPO UTILIZADOR (u_*)");
                body.AppendLine($"* schema: {schema}");
                body.AppendLine($"* tabela: {table}");
                body.AppendLine($"* coluna: {colName}");
                body.AppendLine($"* tipo: {typeName}");
                body.AppendLine($"* max_length: {maxLength}");
                body.AppendLine($"* precision: {precision}");
                body.AppendLine($"* scale: {scale}");
                body.AppendLine($"* nullable: {isNullable}");
                body.AppendLine();

                var artifactId = await SaveDefinitionAsync(
                    context,
                    $"phc/user_fields/{Sanitize(schema)}_{Sanitize(table)}_{Sanitize(colName)}.txt",
                    body.ToString(),
                    ct).ConfigureAwait(false);

                batch.Add(new TechnicalObjectRecord(
                    $"phc.user_field:{schema}.{table}.{colName}",
                    ObjectKind.PhcUserField,
                    colName,
                    $"{schema}.{table}",
                    $"table:{schema}.{table}",
                    CollectorId,
                    artifactId,
                    summary,
                    collectedAt));
                bump();
                count++;
                if (batch.Count >= 500)
                {
                    await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
                    batch.Clear();
                    await report($"Campos u_*: {count}...").ConfigureAwait(false);
                }
            }

            if (batch.Count > 0)
            {
                await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
            }

            await report($"Campos utilizador (u_*): {count}.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            issues.Add(Issue("phc.user_fields", "Não foi possível recolher colunas u_*.", ex));
        }
    }

    private static async Task CollectEscrAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        List<CollectionIssue> issues,
        Action bump,
        Func<string, Task> report,
        CancellationToken ct)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 300;
            cmd.CommandText =
                """
                SELECT RTRIM(escrstamp) AS stamp,
                       RTRIM(ISNULL(codigo,'')) AS codigo,
                       CAST(ISNULL(resumo,'') AS nvarchar(max)) AS resumo,
                       CAST(ISNULL(expressao,'') AS nvarchar(max)) AS expressao,
                       CAST(ISNULL(cmen,'') AS nvarchar(max)) AS cmen,
                       CAST(ISNULL(cmenhtml,'') AS nvarchar(max)) AS cmenhtml,
                       CAST(ISNULL(rurl,'') AS nvarchar(max)) AS rurl,
                       ISNULL(reacao,0) AS reacao,
                       ISNULL(inactivo,0) AS inactivo
                FROM dbo.escr
                ORDER BY codigo, escrstamp
                """;

            var batch = new List<TechnicalObjectRecord>(100);
            var count = 0;
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                var stamp = S(reader, 0);
                var codigo = S(reader, 1);
                var resumo = S(reader, 2);
                var expressao = S(reader, 3);
                var cmen = S(reader, 4);
                var cmenhtml = S(reader, 5);
                var rurl = S(reader, 6);
                var reacao = reader.GetValue(7)?.ToString() ?? "0";
                var inactivo = reader.GetValue(8)?.ToString() ?? "0";

                if (string.IsNullOrWhiteSpace(expressao)
                    && string.IsNullOrWhiteSpace(cmen)
                    && string.IsNullOrWhiteSpace(cmenhtml))
                {
                    continue;
                }

                var body = new StringBuilder();
                body.AppendLine("' ESCR (Web — Scripts Framework CS Web)");
                body.AppendLine($"' stamp: {stamp}");
                body.AppendLine($"' codigo: {codigo}");
                body.AppendLine($"' resumo: {resumo}");
                body.AppendLine($"' reacao: {reacao}");
                body.AppendLine($"' rurl: {rurl}");
                body.AppendLine($"' estado: {PhcActivityState.ToEstado(inactivo)}");
                body.AppendLine($"' inactivo: {(PhcActivityState.IsInactive(inactivo) ? "True" : "False")}");
                body.AppendLine();
                if (!string.IsNullOrWhiteSpace(expressao))
                {
                    body.AppendLine("'=== EXPRESSAO (expressao) ===");
                    body.AppendLine(expressao.TrimEnd());
                    body.AppendLine();
                }
                if (!string.IsNullOrWhiteSpace(cmen))
                {
                    body.AppendLine("'=== CMEN (cmen) ===");
                    body.AppendLine(cmen.TrimEnd());
                    body.AppendLine();
                }
                if (!string.IsNullOrWhiteSpace(cmenhtml))
                {
                    body.AppendLine("'=== CMENHTML (cmenhtml) ===");
                    body.AppendLine(cmenhtml.TrimEnd());
                    body.AppendLine();
                }

                var fileKey = string.IsNullOrWhiteSpace(codigo) ? stamp : codigo;
                var artifactId = await SaveDefinitionAsync(
                    context, $"phc/escr/{Sanitize(fileKey)}.vb", body.ToString(), ct).ConfigureAwait(false);

                var name = string.IsNullOrWhiteSpace(codigo)
                    ? (string.IsNullOrWhiteSpace(resumo) ? stamp : resumo)
                    : codigo;

                batch.Add(new TechnicalObjectRecord(
                    $"phc.web_script:{stamp}",
                    ObjectKind.PhcWebScript,
                    PhcActivityState.WithInactiveSuffix(name, inactivo),
                    string.IsNullOrWhiteSpace(resumo) ? null : resumo,
                    null,
                    CollectorId,
                    artifactId,
                    JsonSerializer.Serialize(new { source = "escr", stamp, codigo, inactivo, estado = PhcActivityState.ToEstado(inactivo) }, JsonOptions),
                    collectedAt));
                bump();
                count++;
                if (batch.Count >= 100)
                {
                    await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
            }

            await report($"escr (scripts Web): {count}.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            issues.Add(Issue("phc.escr", "Não foi possível recolher escr (scripts Web).", ex));
        }
    }

    private static async Task CollectJsuAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        List<CollectionIssue> issues,
        Action bump,
        Func<string, Task> report,
        CancellationToken ct)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 300;
            cmd.CommandText =
                """
                SELECT RTRIM(jsustamp) AS stamp,
                       CAST(ISNULL(titulo,'') AS nvarchar(max)) AS titulo,
                       RTRIM(ISNULL(ecran,'')) AS ecran,
                       CAST(ISNULL(javascript,'') AS nvarchar(max)) AS javascript,
                       ISNULL(ordem,0) AS ordem,
                       ISNULL(inativo,0) AS inativo
                FROM dbo.jsu
                ORDER BY ecran, ordem, titulo, jsustamp
                """;

            var batch = new List<TechnicalObjectRecord>(100);
            var count = 0;
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                var stamp = S(reader, 0);
                var titulo = S(reader, 1);
                var ecran = S(reader, 2);
                var javascript = S(reader, 3);
                var ordem = reader.GetValue(4)?.ToString() ?? "0";
                var inativo = reader.GetValue(5)?.ToString() ?? "0";

                if (string.IsNullOrWhiteSpace(javascript))
                {
                    continue;
                }

                var body = new StringBuilder();
                body.AppendLine("// JSU (JavaScript de Utilizador)");
                body.AppendLine($"// stamp: {stamp}");
                body.AppendLine($"// titulo: {titulo}");
                body.AppendLine($"// ecran: {ecran}");
                body.AppendLine($"// ordem: {ordem}");
                body.AppendLine($"// estado: {PhcActivityState.ToEstado(inativo)}");
                body.AppendLine($"// inativo: {(PhcActivityState.IsInactive(inativo) ? "True" : "False")}");
                body.AppendLine();
                body.AppendLine(javascript.TrimEnd());
                body.AppendLine();

                var artifactId = await SaveDefinitionAsync(
                    context, $"phc/jsu/{Sanitize(stamp)}.js", body.ToString(), ct).ConfigureAwait(false);

                var name = string.IsNullOrWhiteSpace(titulo) ? stamp : titulo;

                batch.Add(new TechnicalObjectRecord(
                    $"phc.user_javascript:{stamp}",
                    ObjectKind.PhcUserJavascript,
                    PhcActivityState.WithInactiveSuffix(name, inativo),
                    string.IsNullOrWhiteSpace(ecran) ? null : ecran,
                    null,
                    CollectorId,
                    artifactId,
                    JsonSerializer.Serialize(new { source = "jsu", stamp, ecran, inativo, estado = PhcActivityState.ToEstado(inativo) }, JsonOptions),
                    collectedAt));
                bump();
                count++;
                if (batch.Count >= 100)
                {
                    await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
            }

            await report($"jsu (JavaScript utilizador): {count}.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            issues.Add(Issue("phc.jsu", "Não foi possível recolher jsu (JavaScript de utilizador).", ex));
        }
    }

    private static async Task CollectCssjsAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        List<CollectionIssue> issues,
        Action bump,
        Func<string, Task> report,
        CancellationToken ct)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 300;
            cmd.CommandText =
                """
                SELECT RTRIM(cssjsstamp) AS stamp,
                       RTRIM(ISNULL(codigo,'')) AS codigo,
                       CAST(ISNULL(titulo,'') AS nvarchar(max)) AS titulo,
                       RTRIM(ISNULL(tipo,'')) AS tipo,
                       CAST(ISNULL(source,'') AS nvarchar(max)) AS source,
                       CAST(ISNULL(sourcedefault,'') AS nvarchar(max)) AS sourcedefault,
                       ISNULL(inativo,0) AS inativo
                FROM dbo.cssjs
                ORDER BY tipo, codigo, cssjsstamp
                """;

            var batch = new List<TechnicalObjectRecord>(100);
            var count = 0;
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                var stamp = S(reader, 0);
                var codigo = S(reader, 1);
                var titulo = S(reader, 2);
                var tipo = S(reader, 3);
                var source = S(reader, 4);
                var sourcedefault = S(reader, 5);
                var inativo = reader.GetValue(6)?.ToString() ?? "0";

                if (string.IsNullOrWhiteSpace(source) && string.IsNullOrWhiteSpace(sourcedefault))
                {
                    continue;
                }

                var body = new StringBuilder();
                body.AppendLine("/* CSSJS (recursos Web CSS/JS) */");
                body.AppendLine($"/* stamp: {stamp} */");
                body.AppendLine($"/* codigo: {codigo} */");
                body.AppendLine($"/* titulo: {titulo} */");
                body.AppendLine($"/* tipo: {tipo} */");
                body.AppendLine($"/* estado: {PhcActivityState.ToEstado(inativo)} */");
                body.AppendLine($"/* inativo: {(PhcActivityState.IsInactive(inativo) ? "True" : "False")} */");
                body.AppendLine();
                if (!string.IsNullOrWhiteSpace(source))
                {
                    body.AppendLine("/*=== SOURCE (source) ===*/");
                    body.AppendLine(source.TrimEnd());
                    body.AppendLine();
                }
                if (!string.IsNullOrWhiteSpace(sourcedefault))
                {
                    body.AppendLine("/*=== SOURCEDEFAULT (sourcedefault) ===*/");
                    body.AppendLine(sourcedefault.TrimEnd());
                    body.AppendLine();
                }

                var fileKey = string.IsNullOrWhiteSpace(codigo) ? stamp : codigo;
                var artifactId = await SaveDefinitionAsync(
                    context, $"phc/cssjs/{Sanitize(fileKey)}.txt", body.ToString(), ct).ConfigureAwait(false);

                var name = string.IsNullOrWhiteSpace(titulo)
                    ? (string.IsNullOrWhiteSpace(codigo) ? stamp : codigo)
                    : titulo;

                batch.Add(new TechnicalObjectRecord(
                    $"phc.web_cssjs:{stamp}",
                    ObjectKind.PhcWebCssJs,
                    PhcActivityState.WithInactiveSuffix(name, inativo),
                    string.IsNullOrWhiteSpace(tipo) ? null : tipo,
                    null,
                    CollectorId,
                    artifactId,
                    JsonSerializer.Serialize(new { source = "cssjs", stamp, codigo, tipo, inativo, estado = PhcActivityState.ToEstado(inativo) }, JsonOptions),
                    collectedAt));
                bump();
                count++;
                if (batch.Count >= 100)
                {
                    await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
            }

            await report($"cssjs: {count}.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            issues.Add(Issue("phc.cssjs", "Não foi possível recolher cssjs.", ex));
        }
    }

    private static async Task CollectFormcHiddenAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        List<CollectionIssue> issues,
        Action bump,
        Func<string, Task> report,
        CancellationToken ct)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 300;
            cmd.CommandText =
                """
                SELECT RTRIM(formcstamp) AS stamp,
                       RTRIM(ISNULL(ecran,'')) AS ecran,
                       RTRIM(ISNULL(nome,'')) AS nome,
                       CAST(ISNULL(tag,'') AS nvarchar(max)) AS tag,
                       CAST(ISNULL(tvalobj,'') AS nvarchar(max)) AS tvalobj,
                       CAST(ISNULL(prop,'') AS nvarchar(max)) AS prop
                FROM dbo.formc
                WHERE CAST(prop AS nvarchar(max)) LIKE N'%udcVisible=.F.%'
                  AND CAST(prop AS nvarchar(max)) LIKE N'%Xexpressao%'
                ORDER BY ecran, nome
                """;

            var batch = new List<TechnicalObjectRecord>(100);
            var count = 0;
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                var stamp = S(reader, 0);
                var ecran = S(reader, 1);
                var nome = S(reader, 2);
                var tag = S(reader, 3);
                var tvalobj = S(reader, 4);
                var prop = S(reader, 5);

                var parsed = FormcPropParser.Parse(prop);
                if (!parsed.IsHiddenWithCode)
                {
                    continue;
                }

                var code = FoxProFlattenedCodeFormatter.Format(parsed.Xexpressao);
                var body = new StringBuilder();
                body.AppendLine("* FORMC Xexpressao (grelha/objecto oculto)");
                body.AppendLine($"* stamp: {stamp}");
                body.AppendLine($"* ecran: {ecran}");
                body.AppendLine($"* nome: {nome}");
                body.AppendLine($"* tag: {tag}");
                body.AppendLine($"* tvalobj: {tvalobj}");
                body.AppendLine($"* udcVisible: {parsed.UdcVisible}");
                body.AppendLine();
                body.AppendLine(code.TrimEnd());
                body.AppendLine();

                var artifactId = await SaveDefinitionAsync(
                    context, $"phc/formc/{Sanitize(stamp)}.prg", body.ToString(), ct).ConfigureAwait(false);

                var name = string.IsNullOrWhiteSpace(nome) ? stamp : nome;

                batch.Add(new TechnicalObjectRecord(
                    $"phc.hidden_grid:{stamp}",
                    ObjectKind.PhcHiddenGrid,
                    name,
                    string.IsNullOrWhiteSpace(ecran) ? null : ecran,
                    null,
                    CollectorId,
                    artifactId,
                    JsonSerializer.Serialize(new { source = "formc", stamp, ecran, nome, tag, tvalobj }, JsonOptions),
                    collectedAt));
                bump();
                count++;
                if (batch.Count >= 100)
                {
                    await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
            }

            await report($"formc (grelhas ocultas): {count}.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            issues.Add(Issue("phc.formc", "Não foi possível recolher grelhas ocultas (formc).", ex));
        }
    }

    private static async Task CollectFormbAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        List<CollectionIssue> issues,
        Action bump,
        Func<string, Task> report,
        CancellationToken ct)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 300;
            cmd.CommandText =
                """
                SELECT RTRIM(formbstamp) AS stamp,
                       RTRIM(ISNULL(ecran,'')) AS ecran,
                       RTRIM(ISNULL(ecrannm,'')) AS ecrannm,
                       RTRIM(ISNULL(nome,'')) AS nome,
                       RTRIM(ISNULL(tabela,'')) AS tabela,
                       CAST(ISNULL(descricao,'') AS nvarchar(max)) AS descricao,
                       ISNULL(defeito,0) AS defeito,
                       ISNULL(ordem,0) AS ordem,
                       ISNULL(inactivo,0) AS inactivo
                FROM dbo.formb
                ORDER BY ecran, ordem, nome, formbstamp
                """;

            var batch = new List<TechnicalObjectRecord>(100);
            var count = 0;
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                var stamp = S(reader, 0);
                var ecran = S(reader, 1);
                var ecrannm = S(reader, 2);
                var nome = S(reader, 3);
                var tabela = S(reader, 4);
                var descricao = S(reader, 5);
                var defeito = reader.GetValue(6)?.ToString() ?? "0";
                var ordem = reader.GetValue(7)?.ToString() ?? "0";
                var inactivo = reader.GetValue(8)?.ToString() ?? "0";

                var body = new StringBuilder();
                body.AppendLine("* FORMB (Ecrã personalizado — Framework)");
                body.AppendLine($"* stamp: {stamp}");
                body.AppendLine($"* ecran: {ecran}");
                body.AppendLine($"* ecrannm: {ecrannm}");
                body.AppendLine($"* nome: {nome}");
                body.AppendLine($"* tabela: {tabela}");
                body.AppendLine($"* descricao: {descricao}");
                body.AppendLine($"* defeito: {defeito}");
                body.AppendLine($"* ordem: {ordem}");
                body.AppendLine($"* estado: {PhcActivityState.ToEstado(inactivo)}");
                body.AppendLine($"* inactivo: {(PhcActivityState.IsInactive(inactivo) ? "True" : "False")}");
                body.AppendLine();

                var artifactId = await SaveDefinitionAsync(
                    context, $"phc/formb/{Sanitize(stamp)}.txt", body.ToString(), ct).ConfigureAwait(false);

                var name = string.IsNullOrWhiteSpace(nome) ? stamp : nome;
                batch.Add(new TechnicalObjectRecord(
                    $"phc.custom_screen:{stamp}",
                    ObjectKind.PhcCustomScreen,
                    PhcActivityState.WithInactiveSuffix(name, inactivo),
                    string.IsNullOrWhiteSpace(ecran) ? null : ecran,
                    null,
                    CollectorId,
                    artifactId,
                    JsonSerializer.Serialize(new
                    {
                        source = "formb",
                        stamp,
                        ecran,
                        nome,
                        tabela,
                        defeito,
                        inactivo,
                        estado = PhcActivityState.ToEstado(inactivo)
                    }, JsonOptions),
                    collectedAt));
                bump();
                count++;
                if (batch.Count >= 100)
                {
                    await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
            }

            await report($"formb (ecrãs personalizados): {count}.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            issues.Add(Issue("phc.formb", "Não foi possível recolher ecrãs personalizados (formb).", ex));
        }
    }

    private static async Task CollectFtuAsync(
        SqlConnection connection,
        ScanContext context,
        DateTimeOffset collectedAt,
        List<CollectionIssue> issues,
        Action bump,
        Func<string, Task> report,
        CancellationToken ct)
    {
        var count = 0;
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 300;
            cmd.CommandText =
                """
                SELECT RTRIM(ftustamp) AS stamp,
                       RTRIM(ISNULL(nome,'')) AS nome,
                       RTRIM(ISNULL(classif,'')) AS classif,
                       CAST(ISNULL(xbase,'') AS nvarchar(max)) AS xbase,
                       ISNULL(inactivo,0) AS inactivo
                FROM dbo.ftu
                ORDER BY nome, ftustamp
                """;

            var batch = new List<TechnicalObjectRecord>(50);
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                var stamp = S(reader, 0);
                var nome = S(reader, 1);
                var classif = S(reader, 2);
                var xbase = S(reader, 3);
                var inactivo = reader.GetValue(4)?.ToString() ?? "0";

                if (string.IsNullOrWhiteSpace(xbase) && string.IsNullOrWhiteSpace(nome))
                {
                    continue;
                }

                var body = new StringBuilder();
                body.AppendLine("-- FTU (Função T-SQL de utilizador — definição PHC)");
                body.AppendLine($"-- stamp: {stamp}");
                body.AppendLine($"-- nome: {nome}");
                body.AppendLine($"-- classif: {classif}");
                body.AppendLine($"-- estado: {PhcActivityState.ToEstado(inactivo)}");
                body.AppendLine($"-- inactivo: {(PhcActivityState.IsInactive(inactivo) ? "True" : "False")}");
                body.AppendLine();
                body.AppendLine(xbase.TrimEnd());
                body.AppendLine();

                var artifactId = await SaveDefinitionAsync(
                    context, $"phc/ftu/{Sanitize(stamp)}.sql", body.ToString(), ct).ConfigureAwait(false);

                var name = string.IsNullOrWhiteSpace(nome) ? stamp : nome;
                if (!string.IsNullOrWhiteSpace(nome))
                {
                    seenNames.Add(nome);
                }

                batch.Add(new TechnicalObjectRecord(
                    $"phc.user_tsql_function:{stamp}",
                    ObjectKind.PhcUserTsqlFunction,
                    PhcActivityState.WithInactiveSuffix(name, inactivo),
                    string.IsNullOrWhiteSpace(classif) ? null : classif,
                    null,
                    CollectorId,
                    artifactId,
                    JsonSerializer.Serialize(new
                    {
                        source = "ftu",
                        stamp,
                        nome,
                        classif,
                        inactivo,
                        estado = PhcActivityState.ToEstado(inactivo)
                    }, JsonOptions),
                    collectedAt));
                bump();
                count++;
                if (batch.Count >= 50)
                {
                    await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
            }

            await report($"ftu (funções T-SQL PHC): {count}.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            issues.Add(Issue("phc.ftu", "Não foi possível recolher ftu (funções T-SQL de utilizador).", ex));
        }

        // Funções já instaladas no SQL Server (Fx_* / User_*) que possam não estar na ftu.
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 300;
            cmd.CommandText =
                """
                SELECT o.name,
                       OBJECT_SCHEMA_NAME(o.object_id) AS sch,
                       OBJECT_DEFINITION(o.object_id) AS def
                FROM sys.objects o
                WHERE o.is_ms_shipped = 0
                  AND o.type IN ('FN', 'IF', 'TF')
                  AND (o.name LIKE N'Fx[_]%' OR o.name LIKE N'User[_]%')
                ORDER BY o.name
                """;

            var batch = new List<TechnicalObjectRecord>(50);
            var installed = 0;
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                var nome = S(reader, 0);
                var sch = S(reader, 1);
                var def = S(reader, 2);
                if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(def))
                {
                    continue;
                }

                if (seenNames.Contains(nome))
                {
                    continue;
                }

                var body = new StringBuilder();
                body.AppendLine("-- Função T-SQL instalada no SQL Server (não encontrada em dbo.ftu)");
                body.AppendLine($"-- schema: {sch}");
                body.AppendLine($"-- nome: {nome}");
                body.AppendLine($"-- estado: Activo");
                body.AppendLine($"-- inactivo: False");
                body.AppendLine();
                body.AppendLine(def.TrimEnd());
                body.AppendLine();

                var artifactId = await SaveDefinitionAsync(
                    context, $"phc/ftu_installed/{Sanitize(sch)}_{Sanitize(nome)}.sql", body.ToString(), ct)
                    .ConfigureAwait(false);

                batch.Add(new TechnicalObjectRecord(
                    $"phc.user_tsql_function:installed:{sch}.{nome}",
                    ObjectKind.PhcUserTsqlFunction,
                    nome,
                    string.IsNullOrWhiteSpace(sch) ? null : sch,
                    null,
                    CollectorId,
                    artifactId,
                    JsonSerializer.Serialize(new
                    {
                        source = "sys.objects",
                        schema = sch,
                        nome,
                        inactivo = false,
                        estado = "Activo"
                    }, JsonOptions),
                    collectedAt));
                bump();
                installed++;
                if (batch.Count >= 50)
                {
                    await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await context.Workspace!.UpsertObjectsAsync(batch, ct).ConfigureAwait(false);
            }

            await report($"funções T-SQL instaladas (Fx_/User_ sem ftu): {installed}.").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            issues.Add(Issue("phc.ftu_installed", "Não foi possível listar funções T-SQL instaladas (Fx_/User_).", ex));
        }
    }

    private static string BuildEventBody(
        string header,
        string stamp,
        string resumo,
        string ecran,
        string qevento,
        string reacao,
        string pdescricao,
        string inactivo,
        string expressao,
        string econdicao,
        string condicao)
    {
        var body = new StringBuilder();
        body.AppendLine(header);
        body.AppendLine($"* stamp: {stamp}");
        body.AppendLine($"* resumo: {resumo}");
        body.AppendLine($"* ecran: {ecran}");
        body.AppendLine($"* qevento: {qevento}");
        body.AppendLine($"* reacao: {reacao}");
        body.AppendLine($"* pdescricao: {pdescricao}");
        body.AppendLine($"* estado: {PhcActivityState.ToEstado(inactivo)}");
        body.AppendLine($"* inactivo: {(PhcActivityState.IsInactive(inactivo) ? "True" : "False")}");
        body.AppendLine();

        var condicaoBody = !string.IsNullOrWhiteSpace(econdicao) ? econdicao : condicao;
        var condicaoSource = !string.IsNullOrWhiteSpace(econdicao) ? "econdicao" : "condicao";
        if (!string.IsNullOrWhiteSpace(condicaoBody))
        {
            body.AppendLine($"*=== CONDICAO ({condicaoSource}) ===");
            body.AppendLine(condicaoBody.TrimEnd());
            body.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(expressao))
        {
            body.AppendLine("*=== EXPRESSAO (expressao) ===");
            body.AppendLine(expressao.TrimEnd());
            body.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(econdicao)
            && !string.IsNullOrWhiteSpace(condicao)
            && !string.Equals(econdicao.Trim(), condicao.Trim(), StringComparison.Ordinal))
        {
            body.AppendLine("*=== CONDICAO (condicao) ===");
            body.AppendLine(condicao.TrimEnd());
            body.AppendLine();
        }

        return body.ToString();
    }

    private static async Task<bool> TableExistsAsync(SqlConnection connection, string tableName, CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT 1
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = N'dbo' AND TABLE_NAME = @t;
            """;
        cmd.Parameters.AddWithValue("@t", tableName);
        var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return result is not null && result is not DBNull;
    }

    private static async Task<string> SaveDefinitionAsync(
        ScanContext context,
        string relativePath,
        string definition,
        CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(definition);
        return await context.Workspace!.SaveArtifactAsync(
            CollectorId,
            relativePath,
            bytes,
            "text/plain",
            new Dictionary<string, string> { ["encoding"] = "utf-8", ["format"] = "prg" },
            ct).ConfigureAwait(false);
    }

    private static string S(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? "" : Convert.ToString(reader.GetValue(ordinal)) ?? "";

    private static string Sanitize(string? s, int max = 80)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return "sem_nome";
        }

        var t = Regex.Replace(s.Trim(), "[\\\\/:*?\"<>|\\r\\n\\t]+", "_");
        t = Regex.Replace(t, "\\s+", "_").Trim('.', '_', ' ');
        if (t.Length > max)
        {
            t = t[..max].TrimEnd('.', '_');
        }

        return string.IsNullOrWhiteSpace(t) ? "sem_nome" : t;
    }

    private static CollectionIssue Issue(string code, string message, Exception ex, string? objectName = null) =>
        new(
            CollectionIssueSeverity.Error,
            code,
            message,
            objectName,
            ex.GetType().Name + ": " + ex.Message);
}
