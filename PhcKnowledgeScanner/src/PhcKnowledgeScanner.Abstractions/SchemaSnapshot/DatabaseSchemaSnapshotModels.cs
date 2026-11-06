namespace PhcKnowledgeScanner.Abstractions.SchemaSnapshot;

/// <summary>Snapshot estrutural completo de uma base SQL Server (apenas metadados).</summary>
public sealed class DatabaseSchemaSnapshot
{
    public const string CurrentSchemaVersion = "1.0.0";

    public SchemaSnapshotManifest Manifest { get; set; } = new();
    public List<SchemaTableSnapshot> Tables { get; set; } = [];
    public List<SchemaForeignKeySnapshot> Relationships { get; set; } = [];
    public List<SchemaIndexSnapshot> Indexes { get; set; } = [];
    public List<SchemaViewSnapshot> Views { get; set; } = [];
    public List<SchemaTriggerSnapshot> Triggers { get; set; } = [];
    public List<SchemaRoutineSnapshot> Procedures { get; set; } = [];
    public List<SchemaRoutineSnapshot> Functions { get; set; } = [];
    public List<SchemaUserTypeSnapshot> UserTypes { get; set; } = [];
    public List<SchemaSynonymSnapshot> Synonyms { get; set; } = [];
}

public sealed class SchemaSnapshotManifest
{
    public string SchemaVersion { get; set; } = DatabaseSchemaSnapshot.CurrentSchemaVersion;
    public string ScannerVersion { get; set; } = "";
    public string? PhcVersion { get; set; }
    public bool? PhcSignatureDetected { get; set; }
    public string? SqlServerVersion { get; set; }
    public string? SqlServerProductVersion { get; set; }
    public DateTimeOffset CollectedAtUtc { get; set; }
    public string OriginFingerprint { get; set; } = "";
    public string SnapshotHash { get; set; } = "";
    public string CompletionStatus { get; set; } = SchemaSnapshotCompletionStatus.Succeeded;
    public bool IncludesSqlDefinitions { get; set; }
    public bool SafeForPublicShare { get; set; } = true;
    public SchemaSnapshotObjectCounts ObjectCounts { get; set; } = new();
    public SchemaSnapshotClassificationSummary Classification { get; set; } = new();
    public List<string> Warnings { get; set; } = [];
    public List<string> Limitations { get; set; } = [];
    public List<SchemaSnapshotCategoryStatus> CategoryStatuses { get; set; } = [];
}

public static class SchemaSnapshotCompletionStatus
{
    public const string Succeeded = "Succeeded";
    public const string SucceededWithWarnings = "SucceededWithWarnings";
    public const string Partial = "Partial";
    public const string Failed = "Failed";
}

public sealed class SchemaSnapshotObjectCounts
{
    public int Tables { get; set; }
    public int Columns { get; set; }
    public int PrimaryKeys { get; set; }
    public int ForeignKeys { get; set; }
    public int Indexes { get; set; }
    public int Views { get; set; }
    public int Triggers { get; set; }
    public int Procedures { get; set; }
    public int Functions { get; set; }
    public int UserTypes { get; set; }
    public int Synonyms { get; set; }
}

public sealed class SchemaSnapshotClassificationSummary
{
    public int SqlSystem { get; set; }
    public int CandidateCustom { get; set; }
    public int Unknown { get; set; }
    public string Policy { get; set; } =
        "Conservative: ms_shipped → sql_system; name prefix u_ → candidate_custom; remaining → unknown. Never asserts PHC standard.";
}

public sealed class SchemaSnapshotCategoryStatus
{
    public string Category { get; set; } = "";
    public string Status { get; set; } = SchemaSnapshotCompletionStatus.Succeeded;
    public string? Detail { get; set; }
}

public sealed class SchemaTableSnapshot
{
    public string SchemaName { get; set; } = "";
    public string Name { get; set; } = "";
    public string ObjectType { get; set; } = "BASE TABLE";
    public string? Description { get; set; }
    public bool IsMsShipped { get; set; }
    public ObjectClassificationInfo Classification { get; set; } =
        new(SchemaObjectClassification.Unknown, SchemaObjectClassification.CriterionNoPositiveEvidence, SchemaObjectClassification.SourceTableInventoryClassification);
    public DateTimeOffset? CreateDateUtc { get; set; }
    public DateTimeOffset? ModifyDateUtc { get; set; }
    public List<SchemaColumnSnapshot> Columns { get; set; } = [];
    public SchemaPrimaryKeySnapshot? PrimaryKey { get; set; }
}

public sealed class SchemaColumnSnapshot
{
    public string Name { get; set; } = "";
    public string SqlType { get; set; } = "";
    public int? MaxLength { get; set; }
    public byte? Precision { get; set; }
    public byte? Scale { get; set; }
    public bool IsNullable { get; set; }
    public bool IsIdentity { get; set; }
    public long? IdentitySeed { get; set; }
    public long? IdentityIncrement { get; set; }
    public string? DefaultDefinition { get; set; }
    public int Ordinal { get; set; }
    public string? Collation { get; set; }
    public bool IsComputed { get; set; }
    public string? ComputedDefinition { get; set; }
    public bool IsPrimaryKey { get; set; }
}

public sealed class SchemaPrimaryKeySnapshot
{
    public string ConstraintName { get; set; } = "";
    public List<SchemaKeyColumnSnapshot> Columns { get; set; } = [];
    public bool IsDisabled { get; set; }
}

public sealed class SchemaKeyColumnSnapshot
{
    public string ColumnName { get; set; } = "";
    public int Ordinal { get; set; }
}

public sealed class SchemaForeignKeySnapshot
{
    public string ConstraintName { get; set; } = "";
    public string ParentSchema { get; set; } = "";
    public string ParentTable { get; set; } = "";
    public string ReferencedSchema { get; set; } = "";
    public string ReferencedTable { get; set; } = "";
    public List<SchemaForeignKeyColumnSnapshot> Columns { get; set; } = [];
    public string? OnDeleteAction { get; set; }
    public string? OnUpdateAction { get; set; }
    public bool IsDisabled { get; set; }
    public bool IsNotTrusted { get; set; }
    public string RelationSource { get; set; } = "sql.foreign_key";
}

public sealed class SchemaForeignKeyColumnSnapshot
{
    public string ParentColumn { get; set; } = "";
    public string ReferencedColumn { get; set; } = "";
    public int Ordinal { get; set; }
}

public sealed class SchemaIndexSnapshot
{
    public string SchemaName { get; set; } = "";
    public string TableName { get; set; } = "";
    public string IndexName { get; set; } = "";
    public string IndexType { get; set; } = "";
    public bool IsUnique { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsUniqueConstraint { get; set; }
    public bool IsDisabled { get; set; }
    public string? FilterDefinition { get; set; }
    public List<SchemaIndexColumnSnapshot> KeyColumns { get; set; } = [];
    public List<string> IncludedColumns { get; set; } = [];
}

public sealed class SchemaIndexColumnSnapshot
{
    public string ColumnName { get; set; } = "";
    public int Ordinal { get; set; }
    public bool IsDescending { get; set; }
}

public sealed class SchemaViewSnapshot
{
    public string SchemaName { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsMsShipped { get; set; }
    public ObjectClassificationInfo Classification { get; set; } =
        new(SchemaObjectClassification.Unknown, SchemaObjectClassification.CriterionNoPositiveEvidence, SchemaObjectClassification.SourceTableInventoryClassification);
    public DateTimeOffset? CreateDateUtc { get; set; }
    public DateTimeOffset? ModifyDateUtc { get; set; }
    public bool HasDefinitionAvailable { get; set; }
    public string? SqlDefinition { get; set; }
}

public sealed class SchemaTriggerSnapshot
{
    public string SchemaName { get; set; } = "";
    public string Name { get; set; } = "";
    public string ParentSchema { get; set; } = "";
    public string ParentObject { get; set; } = "";
    public string ParentObjectType { get; set; } = "";
    public bool IsDisabled { get; set; }
    public bool IsInsteadOf { get; set; }
    public List<string> Events { get; set; } = [];
    public ObjectClassificationInfo Classification { get; set; } =
        new(SchemaObjectClassification.Unknown, SchemaObjectClassification.CriterionNoPositiveEvidence, SchemaObjectClassification.SourceTableInventoryClassification);
    public bool HasDefinitionAvailable { get; set; }
    public string? SqlDefinition { get; set; }
}

public sealed class SchemaRoutineSnapshot
{
    public string SchemaName { get; set; } = "";
    public string Name { get; set; } = "";
    public string RoutineKind { get; set; } = "";
    public string? ReturnType { get; set; }
    public bool IsMsShipped { get; set; }
    public ObjectClassificationInfo Classification { get; set; } =
        new(SchemaObjectClassification.Unknown, SchemaObjectClassification.CriterionNoPositiveEvidence, SchemaObjectClassification.SourceTableInventoryClassification);
    public DateTimeOffset? CreateDateUtc { get; set; }
    public DateTimeOffset? ModifyDateUtc { get; set; }
    public List<SchemaParameterSnapshot> Parameters { get; set; } = [];
    public bool HasDefinitionAvailable { get; set; }
    public string? SqlDefinition { get; set; }
}

public sealed class SchemaParameterSnapshot
{
    public string Name { get; set; } = "";
    public int Ordinal { get; set; }
    public string SqlType { get; set; } = "";
    public int? MaxLength { get; set; }
    public byte? Precision { get; set; }
    public byte? Scale { get; set; }
    public bool IsOutput { get; set; }
    public bool IsReadonly { get; set; }
    public bool HasDefaultValue { get; set; }
}

public sealed class SchemaUserTypeSnapshot
{
    public string SchemaName { get; set; } = "";
    public string Name { get; set; } = "";
    public string SystemTypeName { get; set; } = "";
    public short MaxLength { get; set; }
    public byte Precision { get; set; }
    public byte Scale { get; set; }
    public bool IsNullable { get; set; }
    public bool IsTableType { get; set; }
}

public sealed class SchemaSynonymSnapshot
{
    public string SchemaName { get; set; } = "";
    public string Name { get; set; } = "";
    public string? BaseObjectName { get; set; }
}

/// <summary>Opções de extracção/exportação do snapshot.</summary>
public sealed class SchemaSnapshotOptions
{
    /// <summary>
    /// Quando true, inclui definições SQL completas de views/procedures/functions/triggers.
    /// Por omissão false — modo seguro para partilha.
    /// </summary>
    public bool IncludeSqlDefinitions { get; init; }

    /// <summary>Se true, falha a escrita quando a validação de privacidade encontra problemas.</summary>
    public bool FailOnPrivacyFindings { get; init; } = true;
}

/// <summary>Resultado da comparação entre dois snapshots.</summary>
public sealed class SchemaSnapshotComparison
{
    public string LeftHash { get; set; } = "";
    public string RightHash { get; set; } = "";
    public DateTimeOffset? LeftCollectedAtUtc { get; set; }
    public DateTimeOffset? RightCollectedAtUtc { get; set; }
    public List<string> TablesAdded { get; set; } = [];
    public List<string> TablesRemoved { get; set; } = [];
    public List<SchemaFieldChange> ColumnsAdded { get; set; } = [];
    public List<SchemaFieldChange> ColumnsRemoved { get; set; } = [];
    public List<SchemaFieldChange> ColumnPropertyChanges { get; set; } = [];
    public List<string> RelationshipsAdded { get; set; } = [];
    public List<string> RelationshipsRemoved { get; set; } = [];
    public List<string> IndexesAdded { get; set; } = [];
    public List<string> IndexesRemoved { get; set; } = [];
    public List<string> OtherObjectChanges { get; set; } = [];
}

public sealed class SchemaFieldChange
{
    public string TableKey { get; set; } = "";
    public string ColumnName { get; set; } = "";
    public string? Property { get; set; }
    public string? Before { get; set; }
    public string? After { get; set; }
}

/// <summary>Resultado da validação de privacidade pré-exportação.</summary>
public sealed class SchemaSnapshotPrivacyReport
{
    public bool IsSafe { get; set; } = true;
    public List<SchemaSnapshotPrivacyFinding> Findings { get; set; } = [];
}

public sealed class SchemaSnapshotPrivacyFinding
{
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";
    public string Severity { get; set; } = "Error";
}
