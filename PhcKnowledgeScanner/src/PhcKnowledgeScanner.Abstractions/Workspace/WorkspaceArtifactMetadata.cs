namespace PhcKnowledgeScanner.Abstractions.Workspace;

/// <summary>Persisted metadata for one workspace artifact.</summary>
public sealed record WorkspaceArtifactMetadata(
    string ArtifactId,
    string CollectorId,
    string RelativePath,
    string ContentType,
    string Sha256,
    long ByteLength,
    IReadOnlyDictionary<string, string>? Metadata);
