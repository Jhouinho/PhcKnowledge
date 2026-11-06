using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Engine;

namespace PhcKnowledgeScanner.Engine.Tests;

public class ScanEngineTests
{
    [Fact]
    public async Task RunAsync_FullMode_ExecutesAllCollectors()
    {
        var a = new FakeCollector("a", available: true);
        var b = new FakeCollector("b", available: true);
        var engine = new ScanEngine([a, b]);

        var manifest = await engine.RunAsync(new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.Full
        });

        Assert.Equal(2, manifest.Results.Count);
        Assert.All(manifest.Results, r => Assert.Equal(CollectorStatus.Succeeded, r.Status));
        Assert.Equal(1, a.CollectCalls);
        Assert.Equal(1, b.CollectCalls);
    }

    [Fact]
    public async Task RunAsync_SelectiveMode_ExecutesOnlySelected()
    {
        var a = new FakeCollector("a", available: true);
        var b = new FakeCollector("b", available: true);
        var engine = new ScanEngine([a, b]);

        var manifest = await engine.RunAsync(new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.Selective,
            SelectedCollectorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "b" }
        });

        Assert.Single(manifest.Results);
        Assert.Equal("b", manifest.Results[0].CollectorId);
        Assert.Equal(0, a.CollectCalls);
        Assert.Equal(1, b.CollectCalls);
    }

    [Fact]
    public async Task RunAsync_UnavailableCollector_IsSkipped()
    {
        var unavailable = new FakeCollector("x", available: false, reason: "objeto ausente");
        var engine = new ScanEngine([unavailable]);

        var manifest = await engine.RunAsync(new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.Full
        });

        Assert.Single(manifest.Results);
        Assert.Equal(CollectorStatus.Skipped, manifest.Results[0].Status);
        Assert.Equal(0, unavailable.CollectCalls);
        Assert.Contains(manifest.Results[0].Issues, i => i.Code == "collector.unavailable");
    }

    [Fact]
    public async Task RunAsync_ProbeOnly_DoesNotCollect()
    {
        var collector = new FakeCollector("p", available: true);
        var engine = new ScanEngine([collector]);

        var manifest = await engine.RunAsync(new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.ProbeOnly
        });

        Assert.Single(manifest.Results);
        Assert.Equal(CollectorStatus.Succeeded, manifest.Results[0].Status);
        Assert.Equal(0, collector.CollectCalls);
        Assert.Equal(1, collector.ProbeCalls);
    }

    [Fact]
    public void Constructor_RejectsDuplicateIds()
    {
        Assert.Throws<ArgumentException>(() =>
            new ScanEngine([new FakeCollector("dup", true), new FakeCollector("dup", true)]));
    }

    private sealed class FakeCollector : ICollector
    {
        private readonly bool _available;
        private readonly string? _reason;

        public FakeCollector(string id, bool available, string? reason = null)
        {
            Id = id;
            _available = available;
            _reason = reason;
        }

        public string Id { get; }
        public string DisplayName => Id;
        public int ProbeCalls { get; private set; }
        public int CollectCalls { get; private set; }

        public Task<CollectorCapability> ProbeAsync(ScanContext context, CancellationToken cancellationToken)
        {
            ProbeCalls++;
            return Task.FromResult(new CollectorCapability(_available, _reason));
        }

        public Task<CollectorResult> CollectAsync(ScanContext context, CancellationToken cancellationToken)
        {
            CollectCalls++;
            return Task.FromResult(new CollectorResult(
                Id,
                CollectorStatus.Succeeded,
                Array.Empty<CollectedArtifact>(),
                Array.Empty<CollectionIssue>(),
                TimeSpan.FromMilliseconds(1)));
        }
    }
}
