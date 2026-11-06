namespace PhcKnowledgeScanner.Abstractions.Coverage;

/// <summary>
/// Catálogo estático das capacidades suportadas pelos collectors atuais.
/// Independente dos resultados de uma captura concreta.
/// </summary>
public static class ScannerCapabilityCatalog
{
    public const string CategorySqlStructure = "sql.server.structure";
    public const string CategoryPhcCustomizations = "phc.customizations";
    public const string CategoryPhcDeclaredRelations = "phc.declared-relations";

    public static IReadOnlyList<DeclaredCapability> ForSqlServerStructure { get; } =
    [
        Cap(CategorySqlStructure, KnownOperations.SqlDatabase, "sql.server.structure", "Metadados da base de dados."),
        Cap(CategorySqlStructure, KnownOperations.SqlTablesInventory, "sql.server.structure", "Inventário de tabelas físicas."),
        Cap(CategorySqlStructure, KnownOperations.SqlPrimaryKeys, "sql.server.structure", "Chaves primárias."),
        Cap(CategorySqlStructure, KnownOperations.SqlForeignKeys, "sql.server.structure", "Chaves estrangeiras."),
        Cap(CategorySqlStructure, KnownOperations.SqlViews, "sql.server.structure", "Views."),
        Cap(CategorySqlStructure, KnownOperations.SqlProcedures, "sql.server.structure", "Procedures."),
        Cap(CategorySqlStructure, KnownOperations.SqlTriggers, "sql.server.structure", "Triggers.")
    ];

    public static IReadOnlyList<DeclaredCapability> ForPhcCustomizations { get; } =
    [
        Cap(CategoryPhcCustomizations, KnownOperations.PhcBrule, "phc.customizations", "Regras BRULE."),
        Cap(CategoryPhcCustomizations, KnownOperations.PhcEbrule, "phc.customizations", "Regras EBRULE (opcional)."),
        Cap(CategoryPhcCustomizations, KnownOperations.PhcTeclas, "phc.customizations", "Teclas PHC."),
        Cap(CategoryPhcCustomizations, KnownOperations.PhcUdefs, "phc.customizations", "Valores por defeito UDEFS."),
        Cap(CategoryPhcCustomizations, KnownOperations.PhcEudefs, "phc.customizations", "Valores por defeito EUDEFS (opcional)."),
        Cap(CategoryPhcCustomizations, KnownOperations.PhcEventos, "phc.customizations", "Eventos."),
        Cap(CategoryPhcCustomizations, KnownOperations.PhcEeventos, "phc.customizations", "Eventos EEVENTOS (opcional)."),
        Cap(CategoryPhcCustomizations, KnownOperations.PhcUserFields, "phc.customizations", "Campos de utilizador."),
        Cap(CategoryPhcCustomizations, KnownOperations.PhcEscr, "phc.customizations", "Scripts ESCR (opcional)."),
        Cap(CategoryPhcCustomizations, KnownOperations.PhcJsu, "phc.customizations", "JSU (opcional)."),
        Cap(CategoryPhcCustomizations, KnownOperations.PhcCssjs, "phc.customizations", "CSS/JS (opcional)."),
        Cap(CategoryPhcCustomizations, KnownOperations.PhcFormc, "phc.customizations", "FORMC (opcional)."),
        Cap(CategoryPhcCustomizations, KnownOperations.PhcFormb, "phc.customizations", "FORMB (opcional)."),
        Cap(CategoryPhcCustomizations, KnownOperations.PhcFtu, "phc.customizations", "FTU (opcional).")
    ];

    public static IReadOnlyList<DeclaredCapability> ForPhcDeclaredRelations { get; } =
    [
        Cap(CategoryPhcDeclaredRelations, KnownOperations.PhcRel, "phc.declared-relations", "Relações declaradas dbo.rel."),
        Cap(CategoryPhcDeclaredRelations, KnownOperations.PhcDtl, "phc.declared-relations", "Relações declaradas dbo.dtl.")
    ];

    public static IReadOnlyList<DeclaredCapability> AllSupported { get; } =
        ForSqlServerStructure
            .Concat(ForPhcCustomizations)
            .Concat(ForPhcDeclaredRelations)
            .ToArray();

    public static DeclaredCapability Unsupported(string operationId, string categoryId = "unknown")
        => new(categoryId, operationId, KnownOperations.Version, CollectorId: string.Empty, Supported: false,
            Description: "Operação não suportada por esta versão do Scanner.");

    private static DeclaredCapability Cap(
        string categoryId,
        string operationId,
        string collectorId,
        string description)
        => new(categoryId, operationId, KnownOperations.Version, collectorId, Supported: true, description);
}
