namespace PhcKnowledgeScanner.Abstractions.Inventory;

public static class ObjectKind
{
    public const string Database = "database";
    public const string Schema = "schema";
    public const string Table = "table";
    public const string Column = "column";
    public const string PrimaryKey = "primary_key";
    public const string ForeignKey = "foreign_key";
    public const string Index = "index";
    public const string View = "view";
    public const string StoredProcedure = "stored_procedure";
    public const string Trigger = "trigger";

    // Personalizações PHC — contrato alinhado com PhcCustomizationCodeExport
    public const string PhcEvent = "phc.event";
    public const string PhcWebEvent = "phc.web_event";
    public const string PhcUserRule = "phc.user_rule";
    public const string PhcWebUserRule = "phc.web_user_rule";
    public const string PhcDefaultValue = "phc.default_value";
    public const string PhcWebDefaultValue = "phc.web_default_value";
    /// <summary>Colunas físicas PHC com prefixo <c>u_</c>.</summary>
    public const string PhcUserField = "phc.user_field";
    /// <summary>Linhas de <c>dbo.campos</c> com expressão/valores (configuração, não a coluna física).</summary>
    public const string PhcCampoDefinition = "phc.campo_definition";
    public const string PhcHotkey = "phc.hotkey";
    /// <summary>Scripts Framework CS Web (<c>dbo.escr</c>).</summary>
    public const string PhcWebScript = "phc.web_script";
    /// <summary>JavaScript de Utilizador (<c>dbo.jsu</c>).</summary>
    public const string PhcUserJavascript = "phc.user_javascript";
    /// <summary>Recursos CSS/JS Web (<c>dbo.cssjs</c>).</summary>
    public const string PhcWebCssJs = "phc.web_cssjs";
    /// <summary>Grelhas/objectos ocultos em <c>formc</c> com <c>Xexpressao</c> (DOC-12).</summary>
    public const string PhcHiddenGrid = "phc.hidden_grid";
    /// <summary>Ecrãs personalizados Framework (<c>dbo.formb</c>).</summary>
    public const string PhcCustomScreen = "phc.custom_screen";
    /// <summary>Funções T-SQL de utilizador (<c>dbo.ftu</c> / instaladas <c>Fx_*</c>).</summary>
    public const string PhcUserTsqlFunction = "phc.user_tsql_function";
}
