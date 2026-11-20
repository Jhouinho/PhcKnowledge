using PhcKnowledge.StandardCatalog.Parsing;

namespace PhcKnowledge.StandardCatalog.Tests.Parsing;

public sealed class SqlBatchParserTests
{
    [Fact]
    public void Splits_only_lexer_recognised_go_lines()
    {
        const string source = """
            SELECT 'GO';
            -- GO
            /* GO
               still comment */
            GO
            SELECT 2;
            """;

        var batches = new SqlBatchParser().Parse(source);

        Assert.Equal(2, batches.Count);
        Assert.Contains("SELECT 'GO'", batches[0].Text, StringComparison.Ordinal);
        Assert.Contains("still comment", batches[0].Text, StringComparison.Ordinal);
        Assert.Contains("SELECT 2", batches[1].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Preserves_mixed_line_endings_and_source_offsets()
    {
        const string source = "SELECT 1;\r\nGO\rSELECT 2;\nGO\nSELECT 3;";

        var batches = new SqlBatchParser().Parse(source);

        Assert.Equal(3, batches.Count);
        foreach (var batch in batches)
            Assert.Equal(source.Substring(batch.SourceStartOffset, batch.SourceEndOffset - batch.SourceStartOffset), batch.Text);
    }
}
