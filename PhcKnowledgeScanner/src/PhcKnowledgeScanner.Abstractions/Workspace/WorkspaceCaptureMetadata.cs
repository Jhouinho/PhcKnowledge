using PhcKnowledgeScanner.Abstractions.Coverage;

namespace PhcKnowledgeScanner.Abstractions.Workspace;

/// <summary>
/// Capture metadata read from an existing workspace without modifying it.
/// Null collections mean unavailable/not recorded; empty collections mean an explicit JSON empty array.
/// </summary>
public sealed record WorkspaceCaptureMetadata(
    string? CaptureId,
    string? ScanStatus,
    IReadOnlyList<DeclaredCapability>? Capabilities,
    IReadOnlyList<OperationCoverage>? Coverage,
    bool HasCaptureIdColumn,
    bool HasCapabilitiesColumn,
    bool HasCoverageColumn,
    bool HasMetadataRow = true,
    IReadOnlyList<string>? Diagnostics = null)
{
    public bool HasCapabilitiesData => HasCapabilitiesColumn && Capabilities is not null;

    public bool HasCoverageData => HasCoverageColumn && Coverage is not null;

    public bool HasCompleteCoverageMetadata =>
        HasCapabilitiesData && HasCoverageData;
}
