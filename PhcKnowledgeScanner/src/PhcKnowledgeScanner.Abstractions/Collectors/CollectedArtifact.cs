namespace PhcKnowledgeScanner.Abstractions.Collectors;

/// <summary>
/// Artefacto bruto recolhido. O conteúdo deve ser o original (sem truncagem silenciosa).
/// </summary>
public sealed record CollectedArtifact(
    string RelativePath,
    string ContentType,
    ReadOnlyMemory<byte> Content,
    IReadOnlyDictionary<string, string>? Metadata = null);
