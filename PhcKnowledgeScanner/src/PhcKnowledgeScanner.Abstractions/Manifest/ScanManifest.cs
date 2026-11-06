using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Coverage;

namespace PhcKnowledgeScanner.Abstractions.Manifest;

/// <summary>
/// Manifesto de uma execução: o que foi pedido, o que correu, o que falhou e porquê.
/// </summary>
public sealed record ScanManifest(
    string CaptureId,
    string InstallationId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset FinishedAtUtc,
    ScanMode Mode,
    string ScannerVersion,
    IReadOnlyList<DeclaredCapability> CapabilityCatalog,
    IReadOnlyList<OperationCoverage> OperationCoverages,
    IReadOnlyList<CollectorResult> Results);
