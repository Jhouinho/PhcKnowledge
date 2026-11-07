using PhcKnowledgeScanner.Abstractions.Workspace;

namespace PhcKnowledgeScanner.Export.ObjectIndex;

/// <summary>
/// Loads the complete, typed input required by the pure Object Index resolvers.
/// This adapter performs reads only; it does not mutate the workspace or write an IKP file.
/// </summary>
public sealed class ObjectIndexProjectionInputReader
{
    public async Task<ObjectIndexProjectionInput> ReadAsync(
        IScanWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var integrity = await workspace.CheckIntegrityAsync(cancellationToken).ConfigureAwait(false);
        if (!integrity.IsValid)
        {
            return ObjectIndexProjectionInput.Failure(
                "workspace_integrity_check_failed",
                string.Join("; ", integrity.Messages));
        }

        var capture = await workspace.GetCaptureMetadataAsync(cancellationToken).ConfigureAwait(false);
        if (!capture.HasMetadataRow)
        {
            return ObjectIndexProjectionInput.Failure(
                "workspace_capture_metadata_missing",
                "The workspace has no scan_meta row.");
        }

        if (string.IsNullOrWhiteSpace(capture.CaptureId))
        {
            return ObjectIndexProjectionInput.Failure(
                "workspace_capture_id_missing",
                "The workspace does not contain a CaptureId.");
        }

        var objects = await workspace.GetObjectsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var issues = await workspace.GetIssuesAsync(cancellationToken).ConfigureAwait(false);
        var artifacts = await workspace.GetArtifactsAsync(cancellationToken).ConfigureAwait(false);

        var snapshots = new List<DefinitionArtifactSnapshot>(artifacts.Count);
        var diagnostics = new List<ObjectIndexProjectionDiagnostic>();

        foreach (var artifact in artifacts.OrderBy(x => x.ArtifactId, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bytes = await workspace.ReadArtifactBytesAsync(artifact.ArtifactId, cancellationToken)
                .ConfigureAwait(false);

            if (bytes is null)
            {
                diagnostics.Add(new ObjectIndexProjectionDiagnostic(
                    "workspace_artifact_file_missing",
                    $"Artifact '{artifact.ArtifactId}' has metadata but its file is missing."));
            }

            snapshots.Add(new DefinitionArtifactSnapshot(
                artifact.ArtifactId,
                bytes,
                artifact.Sha256,
                artifact.ByteLength));
        }

        return ObjectIndexProjectionInput.Success(
            capture,
            objects,
            issues,
            artifacts,
            snapshots,
            diagnostics);
    }
}

/// <summary>Read-only snapshot of all inputs needed by the Object Index projection.</summary>
public sealed record ObjectIndexProjectionInput(
    bool IsReady,
    WorkspaceCaptureMetadata? Capture,
    IReadOnlyList<PhcKnowledgeScanner.Abstractions.Inventory.TechnicalObjectRecord> Objects,
    IReadOnlyList<PhcKnowledgeScanner.Abstractions.Collectors.CollectionIssue> Issues,
    IReadOnlyList<WorkspaceArtifactMetadata> Artifacts,
    IReadOnlyList<DefinitionArtifactSnapshot> DefinitionArtifacts,
    IReadOnlyList<ObjectIndexProjectionDiagnostic> Diagnostics)
{
    public string? CaptureId => Capture?.CaptureId;

    public static ObjectIndexProjectionInput Success(
        WorkspaceCaptureMetadata capture,
        IReadOnlyList<PhcKnowledgeScanner.Abstractions.Inventory.TechnicalObjectRecord> objects,
        IReadOnlyList<PhcKnowledgeScanner.Abstractions.Collectors.CollectionIssue> issues,
        IReadOnlyList<WorkspaceArtifactMetadata> artifacts,
        IReadOnlyList<DefinitionArtifactSnapshot> definitionArtifacts,
        IReadOnlyList<ObjectIndexProjectionDiagnostic> diagnostics) =>
        new(true, capture, objects, issues, artifacts, definitionArtifacts, diagnostics);

    public static ObjectIndexProjectionInput Failure(string code, string message) =>
        new(
            false,
            null,
            Array.Empty<PhcKnowledgeScanner.Abstractions.Inventory.TechnicalObjectRecord>(),
            Array.Empty<PhcKnowledgeScanner.Abstractions.Collectors.CollectionIssue>(),
            Array.Empty<WorkspaceArtifactMetadata>(),
            Array.Empty<DefinitionArtifactSnapshot>(),
            [new ObjectIndexProjectionDiagnostic(code, message)]);
}

public sealed record ObjectIndexProjectionDiagnostic(string Code, string Message);
