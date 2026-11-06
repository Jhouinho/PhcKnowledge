using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Inventory.Expression;
using PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;

namespace PhcKnowledgeScanner.Application.Services;

/// <summary>
/// Consulta e formatação da exploração de relações a partir de
/// <see cref="IRelationalKnowledgeQuery"/> (sem SQL, sem reprojectar).
/// </summary>
public static class InventoryRelationExplorer
{
    public static TableRelationBoard BuildBoard(
        IRelationalKnowledgeQuery knowledge,
        string selectedObjectId)
    {
        ArgumentNullException.ThrowIfNull(knowledge);
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedObjectId);

        var outgoing = knowledge.GetKnowledgeRelations(fromObjectId: selectedObjectId)
            .Select(kr => ToListItem(knowledge, kr, RelationDirection.Outgoing, selectedObjectId))
            .OrderBy(i => i.RelatedDisplay, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.KnowledgeRelationId, StringComparer.Ordinal)
            .ToList();

        var incoming = knowledge.GetKnowledgeRelations(toObjectId: selectedObjectId)
            .Select(kr => ToListItem(knowledge, kr, RelationDirection.Incoming, selectedObjectId))
            .OrderBy(i => i.RelatedDisplay, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.KnowledgeRelationId, StringComparer.Ordinal)
            .ToList();

        return new TableRelationBoard(selectedObjectId, outgoing, incoming);
    }

    public static RelationDetailView? BuildDetail(
        IRelationalKnowledgeQuery knowledge,
        string knowledgeRelationId)
    {
        ArgumentNullException.ThrowIfNull(knowledge);
        ArgumentException.ThrowIfNullOrWhiteSpace(knowledgeRelationId);

        var provenance = knowledge.GetProvenance(knowledgeRelationId);
        if (provenance is null)
        {
            return null;
        }

        var kr = provenance.Relation;
        var linkViews = provenance.LinkEvidences
            .Select(ToEvidenceView)
            .ToList();
        var interpViews = provenance.InterpretationEvidences
            .Select(ToEvidenceView)
            .ToList();

        var relatedCanNavigateFrom = CanNavigate(kr.From);
        var relatedCanNavigateTo = CanNavigate(kr.To);

        var fieldMappings = BuildFieldMappings(knowledge, knowledgeRelationId);

        return new RelationDetailView(
            kr.KnowledgeRelationId,
            FormatEndpoint(kr.From),
            kr.From.ObjectId,
            kr.From.Resolution,
            FormatEndpoint(kr.To),
            kr.To.ObjectId,
            kr.To.Resolution,
            kr.EndpointResolutionState,
            DescribeCorrespondence(knowledge, kr),
            DescribeBasis(kr),
            DescribeEvidenceKinds(provenance.LinkEvidences),
            kr.ExactCorrespondenceGroupId,
            kr.ProbableCorrespondenceGroupId,
            kr.LinkEvidenceIds.ToList(),
            kr.InterpretationEvidenceIds.ToList(),
            FormatDiagnostics(kr.Diagnostics),
            linkViews,
            interpViews,
            relatedCanNavigateFrom,
            relatedCanNavigateTo,
            fieldMappings);
    }

    /// <summary>
    /// FieldMappings da relação, ordem determinística (FieldMappingId),
    /// com proveniência da evidência original.
    /// </summary>
    public static IReadOnlyList<FieldMappingView> BuildFieldMappings(
        IRelationalKnowledgeQuery knowledge,
        string knowledgeRelationId)
    {
        ArgumentNullException.ThrowIfNull(knowledge);
        ArgumentException.ThrowIfNullOrWhiteSpace(knowledgeRelationId);

        return knowledge.GetFieldMappings(knowledgeRelationId: knowledgeRelationId)
            .OrderBy(m => m.FieldMappingId, StringComparer.Ordinal)
            .Select(m => ToFieldMappingView(knowledge, m))
            .ToList();
    }

    public static bool CanNavigate(RelationEndpoint endpoint) =>
        endpoint.Resolution == TableReferenceResolution.Resolved
        && !string.IsNullOrWhiteSpace(endpoint.ObjectId);

    private static RelationListItem ToListItem(
        IRelationalKnowledgeQuery knowledge,
        KnowledgeRelation kr,
        RelationDirection direction,
        string selectedObjectId)
    {
        var related = direction == RelationDirection.Outgoing ? kr.To : kr.From;
        var provenance = knowledge.GetProvenance(kr.KnowledgeRelationId);
        var links = provenance?.LinkEvidences ?? Array.Empty<RelationEvidence>();
        var correspondence = DescribeCorrespondence(knowledge, kr);
        var kinds = DescribeEvidenceKinds(links);
        var hasResolutionIssue = kr.EndpointResolutionState != EndpointResolutionState.BothResolved
                                 || links.Any(e => e.From.Resolution != TableReferenceResolution.Resolved
                                                   || e.To.Resolution != TableReferenceResolution.Resolved);

        return new RelationListItem(
            kr.KnowledgeRelationId,
            direction,
            FormatEndpoint(related),
            related.ObjectId,
            CanNavigate(related),
            kinds,
            correspondence,
            links.Count,
            kr.InterpretationEvidenceIds.Count,
            kr.EndpointResolutionState,
            hasResolutionIssue,
            selectedObjectId);
    }

    private static FieldMappingView ToFieldMappingView(
        IRelationalKnowledgeQuery knowledge,
        FieldMapping m)
    {
        var evidence = knowledge.GetEvidence(m.EvidenceId);
        var evidenceKind = evidence?.Kind;
        var evidenceKindLabel = evidenceKind is null
            ? "(evidência desconhecida)"
            : DescribeEvidenceKind(evidenceKind.Value);

        var left = FormatOperand(m.Left);
        var right = FormatOperand(m.Right);

        return new FieldMappingView(
            m.FieldMappingId,
            m.EvidenceId,
            evidenceKind,
            evidenceKindLabel,
            m.InterpretationEvidenceId,
            m.ParserVersion,
            m.Kind,
            DescribeMappingKind(m.Kind),
            m.Operator,
            m.PredicateText,
            m.AstPath,
            m.PredicateOrdinal,
            m.LogicalContext,
            DescribeLogicalContext(m.LogicalContext),
            m.LogicalPath,
            left,
            right,
            m.DiagnosticsJson,
            HasIncompleteResolution(m),
            BuildMappingCompactSummary(m, left, right, evidenceKindLabel));
    }

    private static FieldOperandView FormatOperand(FieldMappingOperand operand)
    {
        var primary = operand.Field is null ? null : FormatMappedField(operand.Field);
        var contained = operand.ContainedFields.Select(FormatMappedField).ToList();
        return new FieldOperandView(
            operand.Kind,
            DescribeOperandKind(operand.Kind),
            operand.OriginalText,
            primary,
            contained);
    }

    private static MappedFieldView FormatMappedField(MappedFieldRef field)
    {
        var resolutionLabel = DescribeTableResolution(field.TableResolution);
        var qualifierLabel = string.IsNullOrWhiteSpace(field.Qualifier)
            ? "Não qualificado"
            : field.Qualifier!;
        var tableLabel = field.TableObjectId
                         ?? (string.IsNullOrWhiteSpace(field.Qualifier) ? "Não especificado" : field.Qualifier!);
        var display = $"{qualifierLabel}.{field.FieldName} [{resolutionLabel}]";
        return new MappedFieldView(
            field.OriginalText,
            field.Qualifier,
            qualifierLabel,
            field.FieldName,
            field.TableResolution,
            resolutionLabel,
            field.TableObjectId,
            tableLabel,
            display);
    }

    public static string DescribeMappingKind(FieldMappingKind kind) => kind switch
    {
        FieldMappingKind.DirectFieldEquality => "Igualdade directa de campos",
        FieldMappingKind.ExpressionComparison => "Comparação com expressão",
        FieldMappingKind.NonFieldPredicate => "Predicado não-campo",
        _ => kind.ToString()
    };

    public static string DescribeLogicalContext(FieldMappingLogicalContext ctx) => ctx switch
    {
        FieldMappingLogicalContext.RequiredUnderAnd =>
            "RequiredUnderAnd — condições conjuntas (AND); não implica chave composta",
        FieldMappingLogicalContext.AlternativeUnderOr =>
            "AlternativeUnderOr — condição alternativa (OR)",
        FieldMappingLogicalContext.Unknown => "Unknown — contexto indeterminado",
        _ => ctx.ToString()
    };

    public static string DescribeTableResolution(TableReferenceResolution resolution) => resolution switch
    {
        TableReferenceResolution.Resolved => "Resolvido",
        TableReferenceResolution.Unresolved => "Não resolvido",
        TableReferenceResolution.Ambiguous => "Ambíguo",
        _ => resolution.ToString()
    };

    private static string DescribeOperandKind(string kind) => kind switch
    {
        FieldMappingOperand.KindField => "Campo",
        FieldMappingOperand.KindLiteral => "Literal",
        FieldMappingOperand.KindExpression => "Expressão",
        FieldMappingOperand.KindUnsupported => "Não suportado",
        _ => kind
    };

    private static bool HasIncompleteResolution(FieldMapping m)
    {
        static bool Incomplete(MappedFieldRef? f) =>
            f is not null && f.TableResolution != TableReferenceResolution.Resolved;

        if (Incomplete(m.Left.Field) || Incomplete(m.Right.Field))
        {
            return true;
        }

        return m.Left.ContainedFields.Any(Incomplete)
               || m.Right.ContainedFields.Any(Incomplete);
    }

    private static string BuildMappingCompactSummary(
        FieldMapping m,
        FieldOperandView left,
        FieldOperandView right,
        string evidenceKindLabel)
    {
        var leftText = left.PrimaryField?.Display
                       ?? (string.IsNullOrWhiteSpace(left.OriginalText) ? left.KindLabel : left.OriginalText);
        var rightText = right.PrimaryField?.Display
                        ?? (string.IsNullOrWhiteSpace(right.OriginalText) ? right.KindLabel : right.OriginalText);
        var incomplete = HasIncompleteResolution(m) ? " · incompleto" : string.Empty;
        return $"{DescribeMappingKindShort(m.Kind)} · {leftText} {m.Operator} {rightText} · {DescribeLogicalContextShort(m.LogicalContext)} · {evidenceKindLabel}{incomplete}";
    }

    private static string DescribeMappingKindShort(FieldMappingKind kind) => kind switch
    {
        FieldMappingKind.DirectFieldEquality => "DirectFieldEquality",
        FieldMappingKind.ExpressionComparison => "ExpressionComparison",
        FieldMappingKind.NonFieldPredicate => "NonFieldPredicate",
        _ => kind.ToString()
    };

    private static string DescribeLogicalContextShort(FieldMappingLogicalContext ctx) => ctx switch
    {
        FieldMappingLogicalContext.RequiredUnderAnd => "AND",
        FieldMappingLogicalContext.AlternativeUnderOr => "OR",
        FieldMappingLogicalContext.Unknown => "?",
        _ => ctx.ToString()
    };

    private static EvidenceView ToEvidenceView(RelationEvidence e)
    {
        return new EvidenceView(
            e.EvidenceId,
            e.Kind,
            DescribeEvidenceKind(e.Kind),
            e.SourceId,
            FormatEndpoint(e.From),
            e.From.ObjectId,
            e.From.Resolution,
            FormatEndpoint(e.To),
            e.To.ObjectId,
            e.To.Resolution,
            e.OriginalExpression,
            e.SummaryJson,
            e.LinkedDeclaredRelationId,
            e.ParserVersion,
            e.ParseStatus,
            e.Confidence,
            e.DiagnosticsJson,
            e.CorrespondenceKind,
            e.CorrespondenceGroupId,
            e.ObservedAtUtc,
            ExtractFkColumns(e),
            ExtractFkActions(e));
    }

    private static string FormatEndpoint(RelationEndpoint ep)
    {
        if (!string.IsNullOrWhiteSpace(ep.SchemaName) && !string.IsNullOrWhiteSpace(ep.RawLabel))
        {
            if (ep.RawLabel.Contains('.', StringComparison.Ordinal))
            {
                return ep.RawLabel;
            }

            return $"{ep.SchemaName}.{ep.RawLabel}";
        }

        return string.IsNullOrWhiteSpace(ep.RawLabel) ? "(sem rótulo)" : ep.RawLabel;
    }

    private static string DescribeEvidenceKinds(IReadOnlyList<RelationEvidence> links)
    {
        if (links.Count == 0)
        {
            return "(sem evidências de ligação)";
        }

        var labels = links
            .Select(e => DescribeEvidenceKind(e.Kind))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
        return string.Join(" + ", labels);
    }

    public static string DescribeEvidenceKind(RelationEvidenceKind kind) => kind switch
    {
        RelationEvidenceKind.SqlForeignKey => "FK SQL",
        RelationEvidenceKind.PhcRel => "PHC REL",
        RelationEvidenceKind.PhcDtl => "PHC DTL",
        RelationEvidenceKind.ExpressionInterpretation => "Interpretação",
        _ => kind.ToString()
    };

    private static string DescribeCorrespondence(
        IRelationalKnowledgeQuery knowledge,
        KnowledgeRelation kr)
    {
        if (!string.IsNullOrWhiteSpace(kr.ExactCorrespondenceGroupId))
        {
            return nameof(DeclaredRelationCorrespondenceKind.Exact);
        }

        if (!string.IsNullOrWhiteSpace(kr.ProbableCorrespondenceGroupId))
        {
            return nameof(DeclaredRelationCorrespondenceKind.Probable);
        }

        // Singleton: reflect link evidence correspondence when present (REL/DTL).
        var kinds = kr.LinkEvidenceIds
            .Select(knowledge.GetEvidence)
            .Where(e => e is not null)
            .Select(e => e!.CorrespondenceKind)
            .Where(c => c is not null)
            .Distinct()
            .ToList();

        if (kinds.Count == 1)
        {
            return kinds[0]!.Value.ToString();
        }

        if (kinds.Count > 1)
        {
            return string.Join("/", kinds.Select(k => k!.Value.ToString()));
        }

        // FK e semelhantes sem CorrespondenceKind.
        return nameof(DeclaredRelationCorrespondenceKind.None);
    }

    private static string DescribeBasis(KnowledgeRelation kr) => kr.ConsolidationBasis switch
    {
        KnowledgeConsolidationBasis.DeclaredExactCorrespondence =>
            "Correspondência Exact declarada (grupo consolidado)",
        KnowledgeConsolidationBasis.Singleton =>
            "Singleton (uma evidência de ligação)",
        _ => kr.ConsolidationBasis.ToString()
    };

    private static IReadOnlyList<string> FormatDiagnostics(IReadOnlyList<EvidenceDiagnostic> diagnostics)
    {
        if (diagnostics.Count == 0)
        {
            return Array.Empty<string>();
        }

        return diagnostics
            .Select(d =>
            {
                var kind = DescribeEvidenceKind(d.Kind);
                if (string.IsNullOrWhiteSpace(d.DiagnosticsJson))
                {
                    return $"{kind} · {d.EvidenceId}";
                }

                return $"{kind} · {d.EvidenceId}: {d.DiagnosticsJson}";
            })
            .ToList();
    }

    private static string? ExtractFkColumns(RelationEvidence e)
    {
        if (e.Kind != RelationEvidenceKind.SqlForeignKey || string.IsNullOrWhiteSpace(e.SummaryJson))
        {
            return null;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(e.SummaryJson);
            if (!doc.RootElement.TryGetProperty("columns", out var cols) || cols.ValueKind != System.Text.Json.JsonValueKind.Array)
            {
                return null;
            }

            var parts = new List<string>();
            foreach (var c in cols.EnumerateArray())
            {
                var parent = c.TryGetProperty("parent", out var p) ? p.GetString() : null;
                var referenced = c.TryGetProperty("referenced", out var r) ? r.GetString() : null;
                if (!string.IsNullOrWhiteSpace(parent) || !string.IsNullOrWhiteSpace(referenced))
                {
                    parts.Add($"{parent ?? "?"} → {referenced ?? "?"}");
                }
            }

            return parts.Count == 0 ? null : string.Join("; ", parts);
        }
        catch
        {
            return null;
        }
    }

    private static (string? DeleteAction, string? UpdateAction) ExtractFkActions(RelationEvidence e)
    {
        if (e.Kind != RelationEvidenceKind.SqlForeignKey || string.IsNullOrWhiteSpace(e.SummaryJson))
        {
            return (null, null);
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(e.SummaryJson);
            string? del = doc.RootElement.TryGetProperty("deleteReferentialAction", out var d)
                ? d.GetString()
                : null;
            string? upd = doc.RootElement.TryGetProperty("updateReferentialAction", out var u)
                ? u.GetString()
                : null;
            return (del, upd);
        }
        catch
        {
            return (null, null);
        }
    }
}

public enum RelationDirection
{
    Outgoing = 0,
    Incoming = 1
}

public sealed record TableRelationBoard(
    string SelectedObjectId,
    IReadOnlyList<RelationListItem> Outgoing,
    IReadOnlyList<RelationListItem> Incoming);

public sealed record RelationListItem(
    string KnowledgeRelationId,
    RelationDirection Direction,
    string RelatedDisplay,
    string? RelatedObjectId,
    bool CanNavigateToRelated,
    string EvidenceKindsLabel,
    string CorrespondenceLabel,
    int LinkEvidenceCount,
    int InterpretationEvidenceCount,
    EndpointResolutionState EndpointResolutionState,
    bool HasResolutionIssue,
    string SelectedObjectId)
{
    public string CompactDisplay
    {
        get
        {
            var issue = HasResolutionIssue ? " · resolução incompleta" : string.Empty;
            return $"{RelatedDisplay} · {EvidenceKindsLabel} · {CorrespondenceLabel} · {LinkEvidenceCount} ev.{issue}";
        }
    }
}

public sealed record RelationDetailView(
    string KnowledgeRelationId,
    string FromDisplay,
    string? FromObjectId,
    TableReferenceResolution FromResolution,
    string ToDisplay,
    string? ToObjectId,
    TableReferenceResolution ToResolution,
    EndpointResolutionState EndpointResolutionState,
    string CorrespondenceLabel,
    string BasisLabel,
    string EvidenceKindsLabel,
    string? ExactCorrespondenceGroupId,
    string? ProbableCorrespondenceGroupId,
    IReadOnlyList<string> LinkEvidenceIds,
    IReadOnlyList<string> InterpretationEvidenceIds,
    IReadOnlyList<string> DiagnosticsLines,
    IReadOnlyList<EvidenceView> LinkEvidences,
    IReadOnlyList<EvidenceView> InterpretationEvidences,
    bool CanNavigateFrom,
    bool CanNavigateTo,
    IReadOnlyList<FieldMappingView> FieldMappings);

public sealed record FieldMappingView(
    string FieldMappingId,
    string EvidenceId,
    RelationEvidenceKind? EvidenceKind,
    string EvidenceKindLabel,
    string? InterpretationEvidenceId,
    string? ParserVersion,
    FieldMappingKind Kind,
    string KindLabel,
    string Operator,
    string PredicateText,
    string AstPath,
    int PredicateOrdinal,
    FieldMappingLogicalContext LogicalContext,
    string LogicalContextLabel,
    string LogicalPath,
    FieldOperandView Left,
    FieldOperandView Right,
    string? DiagnosticsJson,
    bool HasIncompleteResolution,
    string CompactDisplay);

public sealed record FieldOperandView(
    string Kind,
    string KindLabel,
    string OriginalText,
    MappedFieldView? PrimaryField,
    IReadOnlyList<MappedFieldView> ContainedFields);

public sealed record MappedFieldView(
    string OriginalText,
    string? Qualifier,
    string QualifierLabel,
    string FieldName,
    TableReferenceResolution TableResolution,
    string ResolutionLabel,
    string? TableObjectId,
    string TableLabel,
    string Display);

public sealed record EvidenceView(
    string EvidenceId,
    RelationEvidenceKind Kind,
    string KindLabel,
    string SourceId,
    string FromDisplay,
    string? FromObjectId,
    TableReferenceResolution FromResolution,
    string ToDisplay,
    string? ToObjectId,
    TableReferenceResolution ToResolution,
    string? OriginalExpression,
    string? SummaryJson,
    string? LinkedDeclaredRelationId,
    string? ParserVersion,
    ExpressionParseStatus? ParseStatus,
    InterpretationConfidence? Confidence,
    string? DiagnosticsJson,
    DeclaredRelationCorrespondenceKind? CorrespondenceKind,
    string? CorrespondenceGroupId,
    DateTimeOffset? ObservedAtUtc,
    string? FkColumnsSummary,
    (string? DeleteAction, string? UpdateAction) FkActions);
