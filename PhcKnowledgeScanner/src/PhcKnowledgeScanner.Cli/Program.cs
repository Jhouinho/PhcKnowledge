using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhcKnowledgeScanner.Abstractions.Connection;
using PhcKnowledgeScanner.Abstractions.Progress;
using PhcKnowledgeScanner.Abstractions.SchemaSnapshot;
using PhcKnowledgeScanner.Application;
using PhcKnowledgeScanner.Application.Services;
using PhcKnowledgeScanner.Engine;
using PhcKnowledgeScanner.Export.Ikp;

var services = new ServiceCollection();
services.AddLogging(b =>
{
    b.SetMinimumLevel(LogLevel.Information);
    b.AddConsole();
});
services.AddPhcKnowledgeScanner();
await using var provider = services.BuildServiceProvider();

if (args.Length == 0 || args[0] is "--help" or "-h" or "help")
{
    PrintHelp();
    return 0;
}

if (args[0] is "--version" or "-v" or "version")
{
    Console.WriteLine(ScannerVersion.Current);
    return 0;
}

if (args[0] is "smoke")
{
    Console.WriteLine("Smoke legado removido. Use: test-connection | scan | scan-ikp | schema-snapshot");
    return 0;
}

if (args[0] is "test-connection")
{
    var request = ParseConnection(args.Skip(1).ToArray());
    var connection = provider.GetRequiredService<IConnectionAppService>();
    var result = await connection.TestConnectionAsync(request);
    Console.WriteLine(result.Succeeded ? "OK: " + result.Message : "FAIL: " + result.Message);
    if (result.ServerVersion is not null)
    {
        Console.WriteLine(result.ServerVersion);
    }

    return result.Succeeded ? 0 : 1;
}

if (args[0] is "scan")
{
    var (request, collectors) = ParseScanArgs(args.Skip(1).ToArray());
    var workspace = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner", "cli-workspace");
    Directory.CreateDirectory(workspace);
    var scan = provider.GetRequiredService<IScanAppService>();
    var export = provider.GetRequiredService<IWorkspaceExportService>();
    var sink = new ConsoleProgressSink();
    var manifest = await scan.RunStructureScanAsync(request, workspace, sink, default, collectors);
    Console.WriteLine($"Scan concluído. Results={manifest.Results.Count}");
    foreach (var r in manifest.Results)
    {
        Console.WriteLine($"  {r.CollectorId}: {r.Status}");
    }

    if (scan.CurrentWorkspace is not null)
    {
        var counts = await scan.CurrentWorkspace.GetObjectCountsByKindAsync();
        foreach (var (kind, count) in counts.OrderBy(x => x.Key))
        {
            Console.WriteLine($"  objects[{kind}]={count}");
        }
    }

    var zip = Path.Combine(workspace, $"export-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.zip");
    await export.ExportCurrentAsync(zip);
    Console.WriteLine("ZIP: " + zip);
    return 0;
}

if (args[0] is "scan-ikp")
{
    var (request, collectors) = ParseScanArgs(args.Skip(1).ToArray());
    var workspace = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner", "cli-ikp-workspace");
    Directory.CreateDirectory(workspace);
    var scan = provider.GetRequiredService<IScanAppService>();
    var sink = new ConsoleProgressSink();
    var manifest = await scan.RunStructureScanAsync(request, workspace, sink, default, collectors);
    if (scan.CurrentWorkspace is null)
    {
        throw new InvalidOperationException("O scan terminou sem workspace ativo.");
    }

    var output = GetArgValue(args, "--output");
    var zipPath = string.IsNullOrWhiteSpace(output)
        ? Path.Combine(workspace, $"ikp-{manifest.CaptureId}.zip")
        : output;
    var package = await new IkpPackageExporter().ExportAsync(
        scan.CurrentWorkspace,
        manifest,
        new IkpOriginDescriptor(DatabaseName: request.Database),
        zipPath);
    Console.WriteLine("IKP 0.1 exportado: " + package.ZipPath);
    Console.WriteLine("PackageId: " + package.PackageId);
    Console.WriteLine("CaptureId: " + package.CaptureId);
    Console.WriteLine("SHA-256 ZIP: " + package.ZipSha256);
    Console.WriteLine("Logical content digest: " + package.LogicalContentDigest);
    Console.WriteLine($"Ficheiros: {package.PackageFileCount}; artefactos payload: {package.PayloadArtifactCount}");
    return 0;
}

if (args[0] is "schema-snapshot")
{
    var (request, output, zipPath, includeSql) = ParseSchemaSnapshotArgs(args.Skip(1).ToArray());
    var schema = provider.GetRequiredService<IDatabaseSchemaSnapshotService>();
    var sink = new ConsoleProgressSink();
    var options = new SchemaSnapshotOptions
    {
        IncludeSqlDefinitions = includeSql,
        FailOnPrivacyFindings = !includeSql
    };

    if (!string.IsNullOrWhiteSpace(zipPath))
    {
        var zipResult = await schema.ExportZipAsync(request, zipPath, options, sink);
        if (!zipResult.Succeeded)
        {
            Console.WriteLine("Exportação bloqueada pela validação de privacidade:");
            foreach (var finding in zipResult.Privacy.Findings)
            {
                Console.WriteLine($"  [{finding.Severity}] {finding.Code}: {finding.Message}");
            }

            return 2;
        }

        Console.WriteLine("Schema snapshot ZIP: " + zipResult.ZipPath);
        Console.WriteLine("Hash: " + zipResult.SnapshotHash);
        return 0;
    }

    var directory = string.IsNullOrWhiteSpace(output)
        ? Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner", "database-schema")
        : output;
    var result = await schema.ExportAsync(request, directory, options, sink);
    if (!result.Succeeded)
    {
        Console.WriteLine("Exportação bloqueada pela validação de privacidade:");
        foreach (var finding in result.Privacy.Findings)
        {
            Console.WriteLine($"  [{finding.Severity}] {finding.Code}: {finding.Message}");
        }

        return 2;
    }

    Console.WriteLine("Schema snapshot: " + result.OutputDirectory);
    Console.WriteLine("Hash: " + result.SnapshotHash);
    Console.WriteLine($"Privacy OK: {result.Privacy.IsSafe}");
    return 0;
}

if (args[0] is "compare-schema-snapshot")
{
    var left = GetArgValue(args, "--left");
    var right = GetArgValue(args, "--right");
    if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
    {
        Console.WriteLine("Indicação --left e --right é obrigatória.");
        return 1;
    }

    var schema = provider.GetRequiredService<IDatabaseSchemaSnapshotService>();
    var comparison = await schema.CompareDirectoriesAsync(left, right);
    Console.WriteLine($"LeftHash={comparison.LeftHash}");
    Console.WriteLine($"RightHash={comparison.RightHash}");
    Console.WriteLine($"Tables +{comparison.TablesAdded.Count} -{comparison.TablesRemoved.Count}");
    Console.WriteLine($"Columns +{comparison.ColumnsAdded.Count} -{comparison.ColumnsRemoved.Count} ~{comparison.ColumnPropertyChanges.Count}");
    Console.WriteLine($"Relationships +{comparison.RelationshipsAdded.Count} -{comparison.RelationshipsRemoved.Count}");
    Console.WriteLine($"Indexes +{comparison.IndexesAdded.Count} -{comparison.IndexesRemoved.Count}");
    foreach (var change in comparison.TablesAdded.Take(20))
    {
        Console.WriteLine("  +table " + change);
    }

    foreach (var change in comparison.TablesRemoved.Take(20))
    {
        Console.WriteLine("  -table " + change);
    }

    foreach (var change in comparison.ColumnPropertyChanges.Take(40))
    {
        Console.WriteLine($"  ~{change.TableKey}.{change.ColumnName}.{change.Property}: {change.Before} → {change.After}");
    }

    return 0;
}

PrintHelp();
return 1;

static void PrintHelp()
{
    Console.WriteLine("PhcKnowledgeScanner CLI");
    Console.WriteLine("  --help");
    Console.WriteLine("  --version");
    Console.WriteLine("  test-connection --server <s> --database <db> [--auth windows|sql] [--user u] [--password p]");
    Console.WriteLine("  scan            --server <s> --database <db> [--auth windows|sql] [--user u] [--password p]");
    Console.WriteLine("  scan-ikp        --server <s> --database <db> [--auth windows|sql] [--user u] [--password p] [--output <file.zip>]");
    Console.WriteLine("                  [--collectors id1,id2]  (ex.: phc.customizations)");
    Console.WriteLine("  schema-snapshot --server <s> --database <db> [--auth windows|sql] [--user u] [--password p]");
    Console.WriteLine("                  [--output <dir>] [--zip <file.zip>] [--include-sql-definitions]");
    Console.WriteLine("  compare-schema-snapshot --left <dir> --right <dir>");
    Console.WriteLine();
    Console.WriteLine("Password: preferir variável de ambiente PHC_SCANNER_SQL_PASSWORD.");
    Console.WriteLine("schema-snapshot: metadados estruturais apenas (sem registos; SQL completo só com --include-sql-definitions).");
}

static SqlConnectionRequest ParseConnection(string[] args) => ParseScanArgs(args).Request;

static (SqlConnectionRequest Request, IReadOnlySet<string>? Collectors) ParseScanArgs(string[] args)
{
    string? server = null;
    string? database = null;
    var auth = SqlAuthenticationMode.Windows;
    string? user = null;
    string? password = Environment.GetEnvironmentVariable("PHC_SCANNER_SQL_PASSWORD");
    HashSet<string>? collectors = null;

    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--server":
                server = args[++i];
                break;
            case "--database":
                database = args[++i];
                break;
            case "--auth":
                auth = args[++i].Equals("sql", StringComparison.OrdinalIgnoreCase)
                    ? SqlAuthenticationMode.SqlPassword
                    : SqlAuthenticationMode.Windows;
                break;
            case "--user":
                user = args[++i];
                break;
            case "--password":
                password = args[++i];
                break;
            case "--collectors":
                collectors = args[++i]
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                break;
        }
    }

    if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database))
    {
        throw new ArgumentException("Indicação --server e --database é obrigatória.");
    }

    return (new SqlConnectionRequest
    {
        Server = server,
        Database = database,
        AuthenticationMode = auth,
        UserName = user,
        Password = password
    }, collectors);
}

static (SqlConnectionRequest Request, string? Output, string? Zip, bool IncludeSql) ParseSchemaSnapshotArgs(string[] args)
{
    string? server = null;
    string? database = null;
    var auth = SqlAuthenticationMode.Windows;
    string? user = null;
    string? password = Environment.GetEnvironmentVariable("PHC_SCANNER_SQL_PASSWORD");
    string? output = null;
    string? zip = null;
    var includeSql = false;

    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--server":
                server = args[++i];
                break;
            case "--database":
                database = args[++i];
                break;
            case "--auth":
                auth = args[++i].Equals("sql", StringComparison.OrdinalIgnoreCase)
                    ? SqlAuthenticationMode.SqlPassword
                    : SqlAuthenticationMode.Windows;
                break;
            case "--user":
                user = args[++i];
                break;
            case "--password":
                password = args[++i];
                break;
            case "--output":
                output = args[++i];
                break;
            case "--zip":
                zip = args[++i];
                break;
            case "--include-sql-definitions":
                includeSql = true;
                break;
        }
    }

    if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database))
    {
        throw new ArgumentException("Indicação --server e --database é obrigatória.");
    }

    var request = new SqlConnectionRequest
    {
        Server = server,
        Database = database,
        AuthenticationMode = auth,
        UserName = user,
        Password = password
    };
    return (request, output, zip, includeSql);
}

static string? GetArgValue(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    return null;
}

file sealed class ConsoleProgressSink : IScanProgressSink
{
    public void Report(ScanProgressEvent progress)
    {
        Console.WriteLine($"[{progress.Elapsed:mm\\:ss}] {progress.Stage}: {progress.Message}");
    }
}
