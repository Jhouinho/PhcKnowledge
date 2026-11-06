namespace PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;

/// <summary>
/// Contrato de leitura da camada de conhecimento relacional (projeção em memória).
/// O consumidor não executa o parser nem regras de consolidação.
/// </summary>
public interface IRelationalKnowledgeQuery
{
    IReadOnlyList<RelationEvidence> Evidences { get; }

    IReadOnlyList<KnowledgeRelation> KnowledgeRelations { get; }

    IReadOnlyList<FieldMapping> FieldMappings { get; }

    IReadOnlyList<RelationEvidence> GetEvidences(
        RelationEvidenceKind? kind = null,
        string? fromObjectId = null,
        string? toObjectId = null,
        string? fromRawLabel = null,
        string? toRawLabel = null);

    IReadOnlyList<KnowledgeRelation> GetKnowledgeRelations(
        string? fromObjectId = null,
        string? toObjectId = null,
        KnowledgeConsolidationBasis? basis = null);

    KnowledgeRelation? GetKnowledgeRelation(string knowledgeRelationId);

    RelationEvidence? GetEvidence(string evidenceId);

    RelationalKnowledgeProvenance? GetProvenance(string knowledgeRelationId);

    IReadOnlyList<FieldMapping> GetFieldMappings(
        string? evidenceId = null,
        string? knowledgeRelationId = null,
        FieldMappingKind? kind = null,
        FieldMappingLogicalContext? logicalContext = null);

    FieldMapping? GetFieldMapping(string fieldMappingId);
}
