using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Application.Services;

namespace PhcKnowledgeScanner.Tests;

public class TableSearchTests
{
    [Fact]
    public void Exact_Name_Ranks_Above_Prefix_Contains_And_Schema()
    {
        var tables = new[]
        {
            Table("dbo", "xbox"),       // contains "bo" in name? no - "xbox" contains "bo"? x-b-o - "xbox".Contains("bo") = true (positions 1-2? x b o - "bo" at index 1)
            Table("dbo", "bo"),         // exact
            Table("dbo", "board"),      // prefix
            Table("boarding", "zz"),    // schema contains? boarding contains "bo"
            Table("dbo", "cl"),
        };

        var ranked = TableSearch.FilterAndRank(tables, "bo");
        Assert.Equal("bo", ranked[0].Name);
        Assert.Equal(1, TableSearch.Rank(ranked[0], "bo"));

        // board is prefix of "bo"? No - "board".StartsWith("bo") = true → rank 2
        Assert.Contains(ranked, t => t.Name == "board");
        Assert.True(TableSearch.Rank(Table("dbo", "board"), "bo") == 2);

        // xbox contains "bo"
        Assert.Equal(3, TableSearch.Rank(Table("dbo", "xbox"), "bo"));

        // schema-only match rank 4; must appear after name matches
        var schemaOnly = Table("boarding", "zz");
        Assert.Equal(4, TableSearch.Rank(schemaOnly, "bo"));

        var namesBeforeSchema = ranked
            .TakeWhile(t => TableSearch.Rank(t, "bo") < 4)
            .ToList();
        Assert.Contains(namesBeforeSchema, t => t.Name == "bo");
        Assert.Contains(namesBeforeSchema, t => t.Name == "board");
        Assert.DoesNotContain(namesBeforeSchema, t => t.Name == "zz");
        Assert.Contains(ranked, t => t.Name == "zz");
    }

    [Fact]
    public void Case_Insensitive_Exact_And_Prefix()
    {
        // Schema "sales" evita falsos positivos de "dbo" ⊃ "bo".
        var tables = new[] { Table("sales", "BO"), Table("sales", "Booster"), Table("sales", "cl") };
        var ranked = TableSearch.FilterAndRank(tables, "bo");
        Assert.Equal("BO", ranked[0].Name);
        Assert.Equal("Booster", ranked[1].Name);
        Assert.Equal(2, ranked.Count);
    }

    [Fact]
    public void Empty_Search_Returns_Schema_Then_Name_Order()
    {
        var tables = new[] { Table("sales", "z"), Table("dbo", "a"), Table("dbo", "b") };
        var ranked = TableSearch.FilterAndRank(tables, null);
        Assert.Equal(["a", "b", "z"], ranked.Select(t => t.Name).ToArray());
    }

    [Fact]
    public void Bo_Prefers_Table_Name_Over_Dbo_Schema()
    {
        // Regression 1d.2.1: "bo" was matching schema "dbo" for every table.
        var tables = new[]
        {
            Table("dbo", "cl"),
            Table("dbo", "bo"),
            Table("dbo", "bi"),
        };

        var ranked = TableSearch.FilterAndRank(tables, "bo");
        Assert.Equal("bo", ranked[0].Name);
        // cl/bi only match via schema "dbo" containing "bo"
        Assert.All(ranked.Skip(1), t => Assert.Equal(4, TableSearch.Rank(t, "bo")));
    }

    private static TechnicalObjectRecord Table(string schema, string name) =>
        new(
            TableInventoryClassification.BuildObjectId(schema, name),
            ObjectKind.Table,
            name,
            schema,
            null,
            "test",
            null,
            null,
            DateTimeOffset.UtcNow);
}
