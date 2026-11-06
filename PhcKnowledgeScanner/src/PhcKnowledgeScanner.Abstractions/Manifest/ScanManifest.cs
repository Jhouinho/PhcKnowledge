using PhcKnowledgeScanner.Abstractions.Collectors;

namespace PhcKnowledgeScanner.Abstractions.Manifest;

/// <summary>
/// Manifesto de uma execução: o que foi pedido, o que correu, o que falhou e porquê.
/// </summary>
public sealed record ScanManifest(
    string InstallationId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset FinishedAtUtc,
    ScanMode Mode,
    string ScannerVersion,
    IReadOnlyList<CollectorResult> Results);
