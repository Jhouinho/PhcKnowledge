namespace PhcKnowledgeScanner.Abstractions.Coverage;

/// <summary>Identificadores estáveis de operações lógicas suportadas pelos collectors atuais.</summary>
public static class KnownOperations
{
    public const string Version = "1.0";

    // sql.server.structure
    public const string SqlDatabase = "sql.server.structure.database";
    public const string SqlTablesInventory = "sql.server.structure.tables.inventory";
    public const string SqlPrimaryKeys = "sql.server.structure.primary_keys";
    public const string SqlForeignKeys = "sql.server.structure.foreign_keys";
    public const string SqlViews = "sql.server.structure.views";
    public const string SqlProcedures = "sql.server.structure.procedures";
    public const string SqlTriggers = "sql.server.structure.triggers";
    public const string SqlTriggersVersion = "2.0";

    // phc.customizations
    public const string PhcBrule = "phc.customizations.brule";
    public const string PhcEbrule = "phc.customizations.ebrule";
    public const string PhcTeclas = "phc.customizations.teclas";
    public const string PhcUdefs = "phc.customizations.udefs";
    public const string PhcEudefs = "phc.customizations.eudefs";
    public const string PhcEventos = "phc.customizations.eventos";
    public const string PhcEeventos = "phc.customizations.eeventos";
    public const string PhcUserFields = "phc.customizations.user_fields";
    public const string PhcEscr = "phc.customizations.escr";
    public const string PhcJsu = "phc.customizations.jsu";
    public const string PhcCssjs = "phc.customizations.cssjs";
    public const string PhcFormc = "phc.customizations.formc";
    public const string PhcFormb = "phc.customizations.formb";
    public const string PhcFtu = "phc.customizations.ftu";

    // phc.declared-relations
    public const string PhcRel = "phc.declared-relations.rel";
    public const string PhcDtl = "phc.declared-relations.dtl";
}
