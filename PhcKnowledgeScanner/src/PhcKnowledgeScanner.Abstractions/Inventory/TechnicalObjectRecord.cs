namespace PhcKnowledgeScanner.Abstractions.Inventory;

/// <summary>Facto de inventário recolhido (camada de factos).</summary>
public sealed record TechnicalObjectRecord(
    string ObjectId,
    string Kind,
    string Name,
    string? SchemaName,
    string? ParentObjectId,
    string CollectorId,
    string? DefinitionArtifactId,
    string? SummaryJson,
    DateTimeOffset CollectedAtUtc);
