using System.Text.Json;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;
using PhcKnowledgeScanner.Analysis.Knowledge;
using PhcKnowledgeScanner.Workspace;

if (args.Length == 0)
{
    Console.Error.WriteLine("Uso: ValidateRelationalKnowledge <workspaceRoot> [--json <out.json>]");
    return 1;
}

var root = args[0];
string? jsonOut = null;
for (var i = 1; i < args.Length; i++)
{
    if (args[i] == "--json" && i + 1 < args.Length)
    {
        jsonOut = args[++i];
    }
}

await using var workspace = await SqliteScanWorkspace.OpenExistingAsync(root, readOnly: true);
var projector = new RelationalKnowledgeProjector();
var a = await projector.ProjectAsync(workspace);
var b = await projector.ProjectAsync(workspace);

var linkKinds = new[]
{
    RelationEvidenceKind.SqlForeignKey,
    RelationEvidenceKind.PhcRel,
    RelationEvidenceKind.PhcDtl
};

var report = new Dictionary<string, object?>
{
    ["workspace"] = root,
    ["scanId"] = workspace.ScanId,
    ["evidencesTotal"] = a.Evidences.Count,
    ["evidencesByKind"] = a.Evidences.GroupBy(e => e.Kind.ToString())
        .ToDictionary(g => g.Key, g => g.Count()),
    ["knowledgeRelations"] = a.KnowledgeRelations.Count,
    ["exactGroups"] = a.KnowledgeRelations.Count(r =>
        r.ConsolidationBasis == KnowledgeConsolidationBasis.DeclaredExactCorrespondence),
    ["singletons"] = a.KnowledgeRelations.Count(r =>
        r.ConsolidationBasis == KnowledgeConsolidationBasis.Singleton),
    ["probableAnnotatedSingletons"] = a.KnowledgeRelations.Count(r =>
        !string.IsNullOrWhiteSpace(r.ProbableCorrespondenceGroupId)),
    ["declaredNoneOrProbableLinkEvidences"] = a.Evidences.Count(e =>
        e.Kind is RelationEvidenceKind.PhcRel or RelationEvidenceKind.PhcDtl
        && e.CorrespondenceKind is DeclaredRelationCorrespondenceKind.None
            or DeclaredRelationCorrespondenceKind.Probable),
    ["exactLinkEvidences"] = a.Evidences.Count(e =>
        e.Kind is RelationEvidenceKind.PhcRel or RelationEvidenceKind.PhcDtl
        && e.CorrespondenceKind == DeclaredRelationCorrespondenceKind.Exact),
    ["unresolvedOrPartialKr"] = a.KnowledgeRelations.Count(r =>
        r.EndpointResolutionState != EndpointResolutionState.BothResolved),
    ["partialKr"] = a.KnowledgeRelations.Count(r =>
        r.EndpointResolutionState == EndpointResolutionState.Partial),
    ["unresolvedKr"] = a.KnowledgeRelations.Count(r =>
        r.EndpointResolutionState == EndpointResolutionState.Unresolved),
    ["duplicateEvidenceIds"] = a.Evidences.Count - a.Evidences.Select(e => e.EvidenceId).Distinct(StringComparer.Ordinal).Count(),
    ["duplicateKrIds"] = a.KnowledgeRelations.Count
        - a.KnowledgeRelations.Select(r => r.KnowledgeRelationId).Distinct(StringComparer.Ordinal).Count(),
    ["linkEvidenceCoverage"] = linkKinds.Sum(k => a.GetEvidences(k).Count),
    ["linkEvidencesInKr"] = a.KnowledgeRelations.SelectMany(r => r.LinkEvidenceIds).Distinct(StringComparer.Ordinal).Count(),
    ["interpEvidences"] = a.GetEvidences(RelationEvidenceKind.ExpressionInterpretation).Count,
    ["interpAttached"] = a.KnowledgeRelations.SelectMany(r => r.InterpretationEvidenceIds).Distinct(StringComparer.Ordinal).Count(),
    ["determinismIdsEqual"] = a.KnowledgeRelations.Select(r => r.KnowledgeRelationId)
        .SequenceEqual(b.KnowledgeRelations.Select(r => r.KnowledgeRelationId)),
    ["determinismEvidenceEqual"] = a.Evidences.Select(e => e.EvidenceId)
        .SequenceEqual(b.Evidences.Select(e => e.EvidenceId)),
    ["fdPeExact"] = a.GetKnowledgeRelation("kr:exact:corr:exact:FD:PE:bee6de5d5942ed36") is { } fd
        ? new { fd.LinkEvidenceIds.Count, Members = fd.LinkEvidenceIds }
        : null,
    ["fieldMappingsTotal"] = a.FieldMappings.Count,
    ["fieldMappingsByKind"] = a.FieldMappings.GroupBy(m => m.Kind.ToString())
        .ToDictionary(g => g.Key, g => g.Count()),
    ["fieldMappingsByLogicalContext"] = a.FieldMappings.GroupBy(m => m.LogicalContext.ToString())
        .ToDictionary(g => g.Key, g => g.Count()),
    ["directEq"] = a.GetFieldMappings(kind: FieldMappingKind.DirectFieldEquality).Count,
    ["directEqNotUnderOr"] = a.FieldMappings.Count(m =>
        m.Kind == FieldMappingKind.DirectFieldEquality
        && m.LogicalContext != FieldMappingLogicalContext.AlternativeUnderOr),
    ["directEqBothResolved"] = a.FieldMappings.Count(m =>
        m.Kind == FieldMappingKind.DirectFieldEquality
        && m.Left.Field?.TableResolution == TableReferenceResolution.Resolved
        && m.Right.Field?.TableResolution == TableReferenceResolution.Resolved),
    ["directEqUniquePairs"] = a.FieldMappings
        .Where(m => m.Kind == FieldMappingKind.DirectFieldEquality
                    && m.Left.Field is not null && m.Right.Field is not null)
        .Select(m => (m.Left.Field!.OriginalText.ToLowerInvariant(), m.Right.Field!.OriginalText.ToLowerInvariant()))
        .Distinct()
        .Count(),
    ["qualifiedFieldOperands"] = a.FieldMappings.Count(m =>
        (m.Left.Field?.Qualifier is not null) || (m.Right.Field?.Qualifier is not null)),
    ["unqualifiedLeftOrRight"] = a.FieldMappings.Count(m =>
        (m.Left.Kind == FieldMappingOperand.KindField && m.Left.Field?.Qualifier is null)
        || (m.Right.Kind == FieldMappingOperand.KindField && m.Right.Field?.Qualifier is null)),
    ["fieldRefResolved"] = CountFieldRes(a.FieldMappings, TableReferenceResolution.Resolved),
    ["fieldRefAmbiguous"] = CountFieldRes(a.FieldMappings, TableReferenceResolution.Ambiguous),
    ["fieldRefUnresolved"] = CountFieldRes(a.FieldMappings, TableReferenceResolution.Unresolved),
    ["evidencesWithMappings"] = a.FieldMappings.Select(m => m.EvidenceId).Distinct(StringComparer.Ordinal).Count(),
    ["krWithMappings"] = a.FieldMappings.Select(m => m.KnowledgeRelationId).Where(id => id is not null)
        .Distinct(StringComparer.Ordinal).Count(),
    ["duplicateMappingIds"] = a.FieldMappings.Count
        - a.FieldMappings.Select(m => m.FieldMappingId).Distinct(StringComparer.Ordinal).Count(),
    ["determinismMappingsEqual"] = a.FieldMappings.Select(m => m.FieldMappingId)
        .SequenceEqual(b.FieldMappings.Select(m => m.FieldMappingId)),
    ["interpsProcessed"] = a.GetEvidences(RelationEvidenceKind.ExpressionInterpretation).Count
};

static int CountFieldRes(IReadOnlyList<FieldMapping> maps, TableReferenceResolution res)
{
    var n = 0;
    foreach (var m in maps)
    {
        foreach (var side in new[] { m.Left, m.Right })
        {
            if (side.Field is not null && side.Field.TableResolution == res)
            {
                n++;
            }

            foreach (var f in side.ContainedFields)
            {
                // ContainedFields da KindField duplica Field — contar só em expression
                if (side.Kind == FieldMappingOperand.KindExpression && f.TableResolution == res)
                {
                    n++;
                }
            }
        }
    }

    return n;
}

foreach (var (k, v) in report)
{
    Console.WriteLine($"{k}={JsonSerializer.Serialize(v)}");
}

if (jsonOut is not null)
{
    await File.WriteAllTextAsync(jsonOut, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine("wrote=" + jsonOut);
}

return 0;
