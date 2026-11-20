using System.Security.Cryptography;
using System.Text;
using PhcKnowledge.StandardCatalog.Extraction;

namespace PhcKnowledge.StandardCatalog.Tests.Extraction;

public sealed class TriggerExtractorTests
{
    [Fact]
    public void Extracts_bracketed_names_multiple_events_and_context_settings()
    {
        const string source = """
            SET ANSI_NULLS, QUOTED_IDENTIFIER OFF
            GO
            CREATE TRIGGER [dbo].[tr_orders] ON [dbo].[orders]
            WITH ENCRYPTION
            AFTER INSERT, UPDATE
            AS
            BEGIN
                DECLARE @value nvarchar(10) = 'GO';
                -- GO is not a separator here
                SELECT @value;
            END
            GO
            """;

        var trigger = Assert.Single(new TriggerExtractor().Extract(source));

        Assert.Equal("tr_orders", trigger.Name);
        Assert.Equal("dbo", trigger.Schema);
        Assert.Equal("orders", trigger.TargetTable);
        Assert.Equal("dbo", trigger.TargetSchema);
        Assert.Equal(new[] { "INSERT", "UPDATE" }, trigger.Events);
        Assert.Equal("AFTER", trigger.Timing);
        Assert.True(trigger.IsEncrypted);
        Assert.False(trigger.AnsiNulls);
        Assert.False(trigger.QuotedIdentifier);
        Assert.Contains("DECLARE @value", trigger.DefinitionSql, StringComparison.Ordinal);
        Assert.Equal(
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(trigger.DefinitionSql))).ToLowerInvariant(),
            trigger.DefinitionFingerprint);
    }

    [Fact]
    public void Leaves_schema_unknown_when_it_is_not_explicit()
    {
        const string source = """
            CREATE TRIGGER tr_orders ON orders
            AFTER DELETE
            AS
                SELECT 1;
            GO
            """;

        var trigger = Assert.Single(new TriggerExtractor().Extract(source));

        Assert.Equal("tr_orders", trigger.Name);
        Assert.Null(trigger.Schema);
        Assert.Equal("orders", trigger.TargetTable);
        Assert.Null(trigger.TargetSchema);
        Assert.Equal(new[] { "DELETE" }, trigger.Events);
    }

    [Fact]
    public void Rejects_incomplete_trigger_definition()
    {
        const string source = """
            CREATE TRIGGER tr_broken ON dbo.orders
            AFTER INSERT
            AS
            BEGIN
                SELECT 1;
            """;

        Assert.Throws<SqlBatchParseException>(() => new TriggerExtractor().Extract(source));
    }
}
