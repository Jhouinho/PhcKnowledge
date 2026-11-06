using Microsoft.Extensions.Logging;
using PhcKnowledgeScanner.Abstractions.Connection;
using PhcKnowledgeScanner.Abstractions.Progress;
using PhcKnowledgeScanner.Abstractions.SchemaSnapshot;
using PhcKnowledgeScanner.Collectors.Sql;
using PhcKnowledgeScanner.Collectors.Sql.SchemaSnapshot;
using PhcKnowledgeScanner.Engine;
using PhcKnowledgeScanner.Export.SchemaSnapshot;

namespace PhcKnowledgeScanner.Application.Services;

/// <summary>
/// Orquestra extracção e exportação de Database Schema Snapshot sem exigir scan completo.
/// </summary>
public sealed class DatabaseSchemaSnapshotService : IDatabaseSchemaSnapshotService
{
    private readonly DatabaseSchemaSnapshotExtractor _extractor = new();
    private readonly DatabaseSchemaSnapshotWriter _writer = new();
    private readonly DatabaseSchemaSnapshotComparer _comparer = new();
    private readonly ILogger<DatabaseSchemaSnapshotService> _logger;

    public DatabaseSchemaSnapshotService(ILogger<DatabaseSchemaSnapshotService> logger)
    {
        _logger = logger;
    }

    public async Task<SchemaSnapshotWriteResult> ExportAsync(
        SqlConnectionRequest request,
        string outputDirectory,
        SchemaSnapshotOptions? options = null,
        IScanProgressSink? progress = null,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await ExtractAsync(request, options, progress, cancellationToken).ConfigureAwait(false);
        return await _writer.WriteAsync(snapshot, outputDirectory, options, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SchemaSnapshotWriteResult> ExportZipAsync(
        SqlConnectionRequest request,
        string zipPath,
        SchemaSnapshotOptions? options = null,
        IScanProgressSink? progress = null,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await ExtractAsync(request, options, progress, cancellationToken).ConfigureAwait(false);
        return await _writer.WriteZipAsync(snapshot, zipPath, options, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SchemaSnapshotComparison> CompareDirectoriesAsync(
        string leftDirectory,
        string rightDirectory,
        CancellationToken cancellationToken = default)
    {
        var left = await DatabaseSchemaSnapshotWriter.ReadAsync(leftDirectory, cancellationToken).ConfigureAwait(false);
        var right = await DatabaseSchemaSnapshotWriter.ReadAsync(rightDirectory, cancellationToken).ConfigureAwait(false);
        return _comparer.Compare(left, right);
    }

    private async Task<DatabaseSchemaSnapshot> ExtractAsync(
        SqlConnectionRequest request,
        SchemaSnapshotOptions? options,
        IScanProgressSink? progress,
        CancellationToken cancellationToken)
    {
        var connectionString = SqlConnectionStringBuilderService.Build(request);
        _logger.LogInformation(
            "Início de schema snapshot: {Target}; includeSqlDefinitions={IncludeSql}",
            SqlConnectionStringBuilderService.ToLogSafe(request),
            options?.IncludeSqlDefinitions ?? false);

        try
        {
            return await _extractor.ExtractAsync(
                connectionString,
                ScannerVersion.Current,
                options,
                progress,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            connectionString = null!;
        }
    }
}
