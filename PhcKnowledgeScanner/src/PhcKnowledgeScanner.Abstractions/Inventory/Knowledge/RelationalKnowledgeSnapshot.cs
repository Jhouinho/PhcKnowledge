namespace PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;

/// <summary>Snapshot imutável da projeção relacional (V1 — sem persistência derivada).</summary>
public sealed class RelationalKnowledgeSnapshot : IRelationalKnowledgeQuery
{
    private readonly IReadOnlyDictionary<string, RelationEvidence> _evidenceById;
    private readonly IReadOnlyDictionary<string, KnowledgeRelation> _relationsById;
    private readonly IReadOnlyDictionary<string, FieldMapping> _mappingsById;
    private readonly IReadOnlyList<RelationEvidence> _evidences;
    private readonly IReadOnlyList<KnowledgeRelation> _relations;
    private readonly IReadOnlyList<FieldMapping> _mappings;

    public RelationalKnowledgeSnapshot(
        IReadOnlyList<RelationEvidence> evidences,
        IReadOnlyList<KnowledgeRelation> knowledgeRelations,
        IReadOnlyList<FieldMapping>? fieldMappings = null)
    {
        _evidences = evidences;
        _relations = knowledgeRelations;
        _mappings = fieldMappings ?? Array.Empty<FieldMapping>();
        _evidenceById = evidences.ToDictionary(e => e.EvidenceId, StringComparer.Ordinal);
        _relationsById = knowledgeRelations.ToDictionary(r => r.KnowledgeRelationId, StringComparer.Ordinal);
        _mappingsById = _mappings.ToDictionary(m => m.FieldMappingId, StringComparer.Ordinal);
    }

    public IReadOnlyList<RelationEvidence> Evidences => _evidences;

    public IReadOnlyList<KnowledgeRelation> KnowledgeRelations => _relations;

    public IReadOnlyList<FieldMapping> FieldMappings => _mappings;

    public IReadOnlyList<RelationEvidence> GetEvidences(
        RelationEvidenceKind? kind = null,
        string? fromObjectId = null,
        string? toObjectId = null,
        string? fromRawLabel = null,
        string? toRawLabel = null)
    {
        IEnumerable<RelationEvidence> q = _evidences;
        if (kind is not null)
        {
            q = q.Where(e => e.Kind == kind);
        }

        if (!string.IsNullOrWhiteSpace(fromObjectId))
        {
            q = q.Where(e => string.Equals(e.From.ObjectId, fromObjectId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(toObjectId))
        {
            q = q.Where(e => string.Equals(e.To.ObjectId, toObjectId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(fromRawLabel))
        {
            q = q.Where(e => string.Equals(e.From.RawLabel, fromRawLabel, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(toRawLabel))
        {
            q = q.Where(e => string.Equals(e.To.RawLabel, toRawLabel, StringComparison.OrdinalIgnoreCase));
        }

        return q.ToList();
    }

    public IReadOnlyList<KnowledgeRelation> GetKnowledgeRelations(
        string? fromObjectId = null,
        string? toObjectId = null,
        KnowledgeConsolidationBasis? basis = null)
    {
        IEnumerable<KnowledgeRelation> q = _relations;
        if (!string.IsNullOrWhiteSpace(fromObjectId))
        {
            q = q.Where(r => string.Equals(r.From.ObjectId, fromObjectId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(toObjectId))
        {
            q = q.Where(r => string.Equals(r.To.ObjectId, toObjectId, StringComparison.OrdinalIgnoreCase));
        }

        if (basis is not null)
        {
            q = q.Where(r => r.ConsolidationBasis == basis);
        }

        return q.ToList();
    }

    public KnowledgeRelation? GetKnowledgeRelation(string knowledgeRelationId) =>
        _relationsById.TryGetValue(knowledgeRelationId, out var r) ? r : null;

    public RelationEvidence? GetEvidence(string evidenceId) =>
        _evidenceById.TryGetValue(evidenceId, out var e) ? e : null;

    public RelationalKnowledgeProvenance? GetProvenance(string knowledgeRelationId)
    {
        if (!_relationsById.TryGetValue(knowledgeRelationId, out var relation))
        {
            return null;
        }

        var links = relation.LinkEvidenceIds
            .Select(id => _evidenceById[id])
            .ToList();
        var interps = relation.InterpretationEvidenceIds
            .Select(id => _evidenceById[id])
            .ToList();
        return new RelationalKnowledgeProvenance(relation, links, interps);
    }

    public IReadOnlyList<FieldMapping> GetFieldMappings(
        string? evidenceId = null,
        string? knowledgeRelationId = null,
        FieldMappingKind? kind = null,
        FieldMappingLogicalContext? logicalContext = null)
    {
        IEnumerable<FieldMapping> q = _mappings;
        if (!string.IsNullOrWhiteSpace(evidenceId))
        {
            q = q.Where(m => string.Equals(m.EvidenceId, evidenceId, StringComparison.Ordinal));
        }

        if (!string.IsNullOrWhiteSpace(knowledgeRelationId))
        {
            q = q.Where(m => string.Equals(m.KnowledgeRelationId, knowledgeRelationId, StringComparison.Ordinal));
        }

        if (kind is not null)
        {
            q = q.Where(m => m.Kind == kind);
        }

        if (logicalContext is not null)
        {
            q = q.Where(m => m.LogicalContext == logicalContext);
        }

        return q.ToList();
    }

    public FieldMapping? GetFieldMapping(string fieldMappingId) =>
        _mappingsById.TryGetValue(fieldMappingId, out var m) ? m : null;
}
