using PhcKnowledgeScanner.Abstractions.Connection;
using PhcKnowledgeScanner.Abstractions.Progress;
using PhcKnowledgeScanner.Abstractions.SchemaSnapshot;
using PhcKnowledgeScanner.Export.SchemaSnapshot;

namespace PhcKnowledgeScanner.Application.Services;

public interface IDatabaseSchemaSnapshotService
{
    Task<SchemaSnapshotWriteResult> ExportAsync(
        SqlConnectionRequest request,
        string outputDirectory,
        SchemaSnapshotOptions? options = null,
        IScanProgressSink? progress = null,
        CancellationToken cancellationToken = default);

    Task<SchemaSnapshotWriteResult> ExportZipAsync(
        SqlConnectionRequest request,
        string zipPath,
        SchemaSnapshotOptions? options = null,
        IScanProgressSink? progress = null,
        CancellationToken cancellationToken = default);

    Task<SchemaSnapshotComparison> CompareDirectoriesAsync(
        string leftDirectory,
        string rightDirectory,
        CancellationToken cancellationToken = default);
}
