using PhcKnowledgeScanner.Abstractions.Coverage;

namespace PhcKnowledgeScanner.Abstractions.Workspace;

/// <summary>Capture metadata read from an existing workspace without modifying it.</summary>
public sealed record WorkspaceCaptureMetadata(
    string? CaptureId,
    string? ScanStatus,
    IReadOnlyList<DeclaredCapability>? Capabilities,
    IReadOnlyList<OperationCoverage>? Coverage,
    bool HasCaptureIdColumn,
    bool HasCapabilitiesColumn,
    bool HasCoverageColumn)
{
    public bool HasCompleteCoverageMetadata =>
        HasCapabilitiesColumn && HasCoverageColumn && Capabilities is not null && Coverage is not null;
}
