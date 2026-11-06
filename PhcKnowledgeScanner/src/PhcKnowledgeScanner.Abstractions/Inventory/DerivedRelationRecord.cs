namespace PhcKnowledgeScanner.Abstractions.Inventory;

/// <summary>Relação derivada com evidências — não é um facto bruto.</summary>
public sealed record DerivedRelationRecord(
    string RelationId,
    string RelationType,
    string FromObjectId,
    string ToObjectId,
    string Evidence,
    KnowledgeLayer Layer,
    double? Confidence,
    string? Explanation,
    string ValidationState);
