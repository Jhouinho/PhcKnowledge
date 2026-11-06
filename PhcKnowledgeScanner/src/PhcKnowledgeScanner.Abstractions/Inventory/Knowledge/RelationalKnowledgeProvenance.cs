namespace PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;

/// <summary>Proveniência completa de uma <see cref="KnowledgeRelation"/>.</summary>
public sealed record RelationalKnowledgeProvenance(
    KnowledgeRelation Relation,
    IReadOnlyList<RelationEvidence> LinkEvidences,
    IReadOnlyList<RelationEvidence> InterpretationEvidences);
