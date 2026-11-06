namespace PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;

/// <summary>Endpoint de uma evidência ou relação, com rótulo original e resolução opcional.</summary>
public sealed record RelationEndpoint(
    string RawLabel,
    string? SchemaName,
    string? ObjectId,
    TableReferenceResolution Resolution);
