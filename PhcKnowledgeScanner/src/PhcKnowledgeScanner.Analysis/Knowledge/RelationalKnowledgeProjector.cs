using System.Text.Json;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Inventory.Expression;
using PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;
using PhcKnowledgeScanner.Abstractions.Workspace;

namespace PhcKnowledgeScanner.Analysis.Knowledge;

/// <summary>
/// Projeta RelationEvidence + KnowledgeRelation a partir dos factos do workspace.
/// Regenerável, determinístico, sem persistência derivada nem re-parse.
/// </summary>
public sealed class RelationalKnowledgeProjector : IRelationalKnowledgeProjector
{
    public async Task<RelationalKnowledgeSnapshot> ProjectAsync(
        IScanWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var fks = await workspace.GetObjectsAsync(ObjectKind.ForeignKey, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var declared = await workspace.GetDeclaredRelationsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var interpretations = await workspace.GetDeclaredRelationInterpretationsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return Project(fks, declared, interpretations);
    }

    public RelationalKnowledgeSnapshot Project(
        IReadOnlyList<TechnicalObjectRecord> foreignKeys,
        IReadOnlyList<DeclaredRelationRecord> declaredRelations,
        IReadOnlyList<DeclaredRelationInterpretationRecord> interpretations)
    {
        ArgumentNullException.ThrowIfNull(foreignKeys);
        ArgumentNullException.ThrowIfNull(declaredRelations);
        ArgumentNullException.ThrowIfNull(interpretations);

        var evidences = new List<RelationEvidence>();
        var evidenceById = new Dictionary<string, RelationEvidence>(StringComparer.Ordinal);

        void AddEvidence(RelationEvidence evidence)
        {
            if (evidenceById.ContainsKey(evidence.EvidenceId))
            {
                throw new InvalidOperationException($"EvidenceId duplicado na projeção: {evidence.EvidenceId}");
            }

            evidenceById[evidence.EvidenceId] = evidence;
            evidences.Add(evidence);
        }

        // --- FK ---
        foreach (var fk in foreignKeys.OrderBy(f => f.ObjectId, StringComparer.Ordinal))
        {
            AddEvidence(BuildForeignKeyEvidence(fk));
        }

        // --- Declared REL/DTL ---
        foreach (var d in declaredRelations.OrderBy(r => r.RelationId, StringComparer.Ordinal))
        {
            AddEvidence(BuildDeclaredEvidence(d));
        }

        // --- Interpretations (complementares; endpoints herdados do facto declared) ---
        var declaredById = declaredRelations.ToDictionary(d => d.RelationId, StringComparer.Ordinal);
        var interpsByDeclared = new Dictionary<string, List<RelationEvidence>>(StringComparer.Ordinal);
        foreach (var interp in interpretations.OrderBy(i => i.RelationId, StringComparer.Ordinal)
                     .ThenBy(i => i.ParserVersion, StringComparer.Ordinal))
        {
            declaredById.TryGetValue(interp.RelationId, out var linked);
            var ev = BuildInterpretationEvidence(interp, linked);
            AddEvidence(ev);
            if (!interpsByDeclared.TryGetValue(interp.RelationId, out var list))
            {
                list = [];
                interpsByDeclared[interp.RelationId] = list;
            }

            list.Add(ev);
        }

        var knowledgeRelations = new List<KnowledgeRelation>();
        var krIds = new HashSet<string>(StringComparer.Ordinal);
        var consumedDeclared = new HashSet<string>(StringComparer.Ordinal);

        void AddKr(KnowledgeRelation kr)
        {
            if (!krIds.Add(kr.KnowledgeRelationId))
            {
                throw new InvalidOperationException($"KnowledgeRelationId duplicado: {kr.KnowledgeRelationId}");
            }

            knowledgeRelations.Add(kr);
        }

        // Exact groups (não recalcular correspondence — usar a persistida)
        var exactGroups = declaredRelations
            .Where(r => r.CorrespondenceKind == DeclaredRelationCorrespondenceKind.Exact
                        && !string.IsNullOrWhiteSpace(r.CorrespondenceGroupId))
            .GroupBy(r => r.CorrespondenceGroupId!, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        foreach (var group in exactGroups)
        {
            var members = group.OrderBy(m => m.RelationId, StringComparer.Ordinal).ToList();
            foreach (var m in members)
            {
                consumedDeclared.Add(m.RelationId);
            }

            var linkIds = KnowledgeRelationIds.CanonicalizeEvidenceIds(members.Select(m => m.RelationId));
            var interpIds = KnowledgeRelationIds.CanonicalizeEvidenceIds(
                linkIds.SelectMany(id =>
                    interpsByDeclared.TryGetValue(id, out var list)
                        ? list.Select(e => e.EvidenceId)
                        : []));

            var canonical = members[0];
            var from = BuildEndpoint(canonical.FromTable, null, canonical.FromObjectId, canonical.FromTableResolution);
            var to = BuildEndpoint(canonical.ToTable, null, canonical.ToObjectId, canonical.ToTableResolution);
            // Prefer object ids when all members agree on Resolved ids
            from = PreferAgreedEndpoint(members, isFrom: true, fallback: from);
            to = PreferAgreedEndpoint(members, isFrom: false, fallback: to);

            AddKr(new KnowledgeRelation(
                KnowledgeRelationIds.ExactKnowledgeRelationId(group.Key),
                from,
                to,
                KnowledgeRelationIds.ResolveEndpointState(from, to),
                KnowledgeConsolidationBasis.DeclaredExactCorrespondence,
                linkIds,
                interpIds,
                ExactCorrespondenceGroupId: group.Key,
                ProbableCorrespondenceGroupId: null,
                BuildDiagnostics(linkIds.Concat(interpIds), evidenceById)));
        }

        // Singletons: None, Probable (não consolidar), e qualquer declared não consumido
        foreach (var d in declaredRelations.OrderBy(r => r.RelationId, StringComparer.Ordinal))
        {
            if (consumedDeclared.Contains(d.RelationId))
            {
                continue;
            }

            var linkIds = KnowledgeRelationIds.CanonicalizeEvidenceIds([d.RelationId]);
            var interpIds = KnowledgeRelationIds.CanonicalizeEvidenceIds(
                interpsByDeclared.TryGetValue(d.RelationId, out var list)
                    ? list.Select(e => e.EvidenceId)
                    : []);

            var from = BuildEndpoint(d.FromTable, null, d.FromObjectId, d.FromTableResolution);
            var to = BuildEndpoint(d.ToTable, null, d.ToObjectId, d.ToTableResolution);
            string? probableGroup = d.CorrespondenceKind == DeclaredRelationCorrespondenceKind.Probable
                ? d.CorrespondenceGroupId
                : null;

            AddKr(new KnowledgeRelation(
                KnowledgeRelationIds.SingletonDeclaredKnowledgeRelationId(d.RelationId),
                from,
                to,
                KnowledgeRelationIds.ResolveEndpointState(from, to),
                KnowledgeConsolidationBasis.Singleton,
                linkIds,
                interpIds,
                ExactCorrespondenceGroupId: null,
                ProbableCorrespondenceGroupId: probableGroup,
                BuildDiagnostics(linkIds.Concat(interpIds), evidenceById)));
        }

        // FK singletons — nunca fundir com REL/DTL
        foreach (var fk in foreignKeys.OrderBy(f => f.ObjectId, StringComparer.Ordinal))
        {
            var ev = evidenceById[KnowledgeRelationIds.ForeignKeyEvidenceId(fk.ObjectId)];
            AddKr(new KnowledgeRelation(
                KnowledgeRelationIds.ForeignKeyKnowledgeRelationId(fk.ObjectId),
                ev.From,
                ev.To,
                KnowledgeRelationIds.ResolveEndpointState(ev.From, ev.To),
                KnowledgeConsolidationBasis.Singleton,
                KnowledgeRelationIds.CanonicalizeEvidenceIds([ev.EvidenceId]),
                Array.Empty<string>(),
                ExactCorrespondenceGroupId: null,
                ProbableCorrespondenceGroupId: null,
                BuildDiagnostics([ev.EvidenceId], evidenceById)));
        }

        // --- FieldMappings (AST das interpretações; origem = evidência declared) ---
        var declaredToKr = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var kr in knowledgeRelations)
        {
            foreach (var linkId in kr.LinkEvidenceIds)
            {
                // Uma evidência declared pertence a exactamente uma KR.
                declaredToKr[linkId] = kr.KnowledgeRelationId;
            }
        }

        var fieldMappings = new List<FieldMapping>();
        var mappingIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var interp in interpretations.OrderBy(i => i.RelationId, StringComparer.Ordinal)
                     .ThenBy(i => i.ParserVersion, StringComparer.Ordinal))
        {
            declaredToKr.TryGetValue(interp.RelationId, out var krId);
            var interpEvidenceId = KnowledgeRelationIds.InterpretationEvidenceId(
                interp.RelationId, interp.ParserVersion);
            var extracted = FieldMappingExtractor.Extract(
                interp.RelationId,
                krId,
                interpEvidenceId,
                interp.ParserVersion,
                interp.Ast,
                interp.DiagnosticsJson);
            foreach (var m in extracted)
            {
                if (!mappingIds.Add(m.FieldMappingId))
                {
                    throw new InvalidOperationException($"FieldMappingId duplicado: {m.FieldMappingId}");
                }

                fieldMappings.Add(m);
            }
        }

        // Ordenação estável do snapshot
        evidences = evidences.OrderBy(e => e.EvidenceId, StringComparer.Ordinal).ToList();
        knowledgeRelations = knowledgeRelations.OrderBy(r => r.KnowledgeRelationId, StringComparer.Ordinal).ToList();
        fieldMappings = fieldMappings
            .OrderBy(m => m.FieldMappingId, StringComparer.Ordinal)
            .ToList();

        return new RelationalKnowledgeSnapshot(evidences, knowledgeRelations, fieldMappings);
    }

    private static RelationEvidence BuildForeignKeyEvidence(TechnicalObjectRecord fk)
    {
        string? refSchema = null;
        string? refTable = null;
        if (!string.IsNullOrWhiteSpace(fk.SummaryJson))
        {
            using var doc = JsonDocument.Parse(fk.SummaryJson);
            if (doc.RootElement.TryGetProperty("referencedSchema", out var s))
            {
                refSchema = s.GetString();
            }

            if (doc.RootElement.TryGetProperty("referencedTable", out var t))
            {
                refTable = t.GetString();
            }
        }

        var fromObjectId = fk.ParentObjectId;
        var fromLabel = !string.IsNullOrWhiteSpace(fromObjectId) && fromObjectId.StartsWith("table:", StringComparison.Ordinal)
            ? fromObjectId["table:".Length..]
            : $"{fk.SchemaName}.{fk.Name}";
        var from = new RelationEndpoint(
            fromLabel,
            fk.SchemaName,
            fromObjectId,
            string.IsNullOrWhiteSpace(fromObjectId)
                ? TableReferenceResolution.Unresolved
                : TableReferenceResolution.Resolved);

        var toObjectId = refSchema is not null && refTable is not null
            ? TableInventoryClassification.BuildObjectId(refSchema, refTable)
            : null;
        var toLabel = refSchema is not null && refTable is not null
            ? $"{refSchema}.{refTable}"
            : (refTable ?? string.Empty);
        var to = new RelationEndpoint(
            toLabel,
            refSchema,
            toObjectId,
            toObjectId is null ? TableReferenceResolution.Unresolved : TableReferenceResolution.Resolved);

        return new RelationEvidence(
            KnowledgeRelationIds.ForeignKeyEvidenceId(fk.ObjectId),
            RelationEvidenceKind.SqlForeignKey,
            fk.ObjectId,
            from,
            to,
            OriginalExpression: null,
            SummaryJson: fk.SummaryJson,
            LinkedDeclaredRelationId: null,
            ParserVersion: null,
            ParseStatus: null,
            Confidence: null,
            DiagnosticsJson: null,
            CorrespondenceKind: null,
            CorrespondenceGroupId: null,
            ObservedAtUtc: fk.CollectedAtUtc);
    }

    private static RelationEvidence BuildDeclaredEvidence(DeclaredRelationRecord d)
    {
        var kind = string.Equals(d.SourceType, DeclaredRelationSourceTypes.PhcDtl, StringComparison.Ordinal)
            ? RelationEvidenceKind.PhcDtl
            : RelationEvidenceKind.PhcRel;

        return new RelationEvidence(
            KnowledgeRelationIds.DeclaredEvidenceId(d.RelationId),
            kind,
            d.RelationId,
            BuildEndpoint(d.FromTable, null, d.FromObjectId, d.FromTableResolution),
            BuildEndpoint(d.ToTable, null, d.ToObjectId, d.ToTableResolution),
            d.OriginalExpression,
            SummaryJson: d.RawMetadataJson,
            LinkedDeclaredRelationId: null,
            ParserVersion: null,
            ParseStatus: null,
            Confidence: null,
            DiagnosticsJson: null,
            d.CorrespondenceKind,
            d.CorrespondenceGroupId,
            d.CollectedAtUtc);
    }

    private static RelationEvidence BuildInterpretationEvidence(
        DeclaredRelationInterpretationRecord interp,
        DeclaredRelationRecord? linkedDeclared)
    {
        RelationEndpoint from;
        RelationEndpoint to;
        if (linkedDeclared is not null)
        {
            from = BuildEndpoint(
                linkedDeclared.FromTable, null, linkedDeclared.FromObjectId, linkedDeclared.FromTableResolution);
            to = BuildEndpoint(
                linkedDeclared.ToTable, null, linkedDeclared.ToObjectId, linkedDeclared.ToTableResolution);
        }
        else
        {
            // Sem facto linked: não inventar endpoints.
            from = new RelationEndpoint(string.Empty, null, null, TableReferenceResolution.Unresolved);
            to = from;
        }

        return new RelationEvidence(
            KnowledgeRelationIds.InterpretationEvidenceId(interp.RelationId, interp.ParserVersion),
            RelationEvidenceKind.ExpressionInterpretation,
            interp.RelationId,
            from,
            to,
            OriginalExpression: linkedDeclared?.OriginalExpression,
            SummaryJson: null,
            LinkedDeclaredRelationId: interp.RelationId,
            interp.ParserVersion,
            interp.ParseStatus,
            interp.Confidence,
            interp.DiagnosticsJson,
            CorrespondenceKind: linkedDeclared?.CorrespondenceKind,
            CorrespondenceGroupId: linkedDeclared?.CorrespondenceGroupId,
            ObservedAtUtc: interp.InterpretedAtUtc);
    }

    private static RelationEndpoint BuildEndpoint(
        string rawLabel,
        string? schemaName,
        string? objectId,
        TableReferenceResolution resolution)
    {
        string? schema = schemaName;
        if (schema is null && !string.IsNullOrWhiteSpace(objectId)
            && objectId.StartsWith("table:", StringComparison.Ordinal))
        {
            var rest = objectId["table:".Length..];
            var dot = rest.IndexOf('.');
            if (dot > 0)
            {
                schema = rest[..dot];
            }
        }

        return new RelationEndpoint(rawLabel, schema, objectId, resolution);
    }

    private static RelationEndpoint PreferAgreedEndpoint(
        IReadOnlyList<DeclaredRelationRecord> members,
        bool isFrom,
        RelationEndpoint fallback)
    {
        var ids = members
            .Select(m => isFrom ? m.FromObjectId : m.ToObjectId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var resolutions = members
            .Select(m => isFrom ? m.FromTableResolution : m.ToTableResolution)
            .ToList();

        if (ids.Count == 1
            && resolutions.All(r => r == TableReferenceResolution.Resolved))
        {
            var id = ids[0]!;
            var label = isFrom ? members[0].FromTable : members[0].ToTable;
            return BuildEndpoint(label, null, id, TableReferenceResolution.Resolved);
        }

        return fallback;
    }

    private static IReadOnlyList<EvidenceDiagnostic> BuildDiagnostics(
        IEnumerable<string> evidenceIds,
        IReadOnlyDictionary<string, RelationEvidence> byId)
    {
        var list = new List<EvidenceDiagnostic>();
        foreach (var id in KnowledgeRelationIds.CanonicalizeEvidenceIds(evidenceIds))
        {
            if (!byId.TryGetValue(id, out var ev))
            {
                continue;
            }

            // Preservar entrada mesmo sem JSON (união sem perda de associação).
            list.Add(new EvidenceDiagnostic(ev.EvidenceId, ev.Kind, ev.DiagnosticsJson));
        }

        return list;
    }
}
