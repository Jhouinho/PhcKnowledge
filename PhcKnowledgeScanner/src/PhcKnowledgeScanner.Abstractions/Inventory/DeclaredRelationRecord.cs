namespace PhcKnowledgeScanner.Abstractions.Inventory;

/// <summary>
/// Facto PHC declarado (Camada A): ligação configurada em <c>dbo.rel</c> / <c>dbo.dtl</c>.
/// Não é FK SQL nem inferência — não usar <see cref="DerivedRelationRecord"/>.
/// </summary>
public sealed record DeclaredRelationRecord(
    string RelationId,
    string SourceType,
    string? SourceRecordId,
    string FromTable,
    string? FromTableName,
    string ToTable,
    string? ToTableName,
    string OriginalExpression,
    string? CreatedBy,
    DateTimeOffset? CreatedAt,
    string? ModifiedBy,
    DateTimeOffset? ModifiedAt,
    bool? IsMarked,
    string? RawMetadataJson,
    DateTimeOffset CollectedAtUtc,
    string? FromObjectId,
    string? ToObjectId,
    TableReferenceResolution FromTableResolution,
    TableReferenceResolution ToTableResolution,
    DeclaredRelationCorrespondenceKind CorrespondenceKind,
    string? CorrespondenceGroupId)
{
    /// <summary>Compatibilidade: verdadeiro apenas quando a resolução é inequívoca.</summary>
    public bool FromTableResolved => FromTableResolution == TableReferenceResolution.Resolved;

    /// <summary>Compatibilidade: verdadeiro apenas quando a resolução é inequívoca.</summary>
    public bool ToTableResolved => ToTableResolution == TableReferenceResolution.Resolved;
}

public enum DeclaredRelationCorrespondenceKind
{
    None = 0,
    Exact = 1,
    Probable = 2
}

public static class DeclaredRelationSourceTypes
{
    public const string PhcRel = "phc.rel";
    public const string PhcDtl = "phc.dtl";
}
