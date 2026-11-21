using System.Security.Cryptography;
using System.Text;
using PhcKnowledge.StandardCatalog.Extraction;

namespace PhcKnowledge.StandardCatalog.Tests.Extraction;

public sealed class StandardTriggerSourceTests
{
    [Fact]
    public void Standard_source_matches_the_confirmed_inventory_baseline()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Sources", "triggers phc.sql");
        Assert.True(File.Exists(path), $"Standard trigger source was not copied to test output: {path}");

        var source = File.ReadAllText(path, Encoding.UTF8);
        var triggers = new TriggerExtractor().Extract(source);

        Assert.Equal(552, triggers.Count);
        Assert.Equal(258, triggers.Select(x => x.TargetTable).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(552, triggers.Count(x => x.IsEncrypted));
        Assert.All(triggers, trigger => Assert.False(string.IsNullOrWhiteSpace(trigger.TargetTable)));
        Assert.All(triggers, trigger => Assert.StartsWith("CREATE TRIGGER", trigger.DefinitionSql.TrimStart(), StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, triggers.GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Count(group => group.Count() > 1));

        Assert.Equal(4, triggers.Count(x => x.Events.Count > 1));
        Assert.Equal(132, triggers.Sum(x => x.Events.Count(e => e == "INSERT")));
        Assert.Equal(229, triggers.Sum(x => x.Events.Count(e => e == "UPDATE")));
        Assert.Equal(187, triggers.Sum(x => x.Events.Count(e => e == "DELETE")));

        foreach (var trigger in triggers)
        {
            var expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(trigger.DefinitionSql))).ToLowerInvariant();
            Assert.Equal(expected, trigger.DefinitionFingerprint);
            Assert.Equal(trigger.DefinitionSql, source.Substring(
                trigger.SourceStartOffset,
                trigger.SourceEndOffsetExclusive - trigger.SourceStartOffset));
        }
    }
}
