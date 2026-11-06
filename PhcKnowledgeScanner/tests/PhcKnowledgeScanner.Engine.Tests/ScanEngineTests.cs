using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Coverage;
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

    [Fact]
    public async Task RunAsync_AssignsValidCaptureId()
    {
        var engine = new ScanEngine([new FakeCollector("a", true)]);
        var manifest = await engine.RunAsync(new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.Full
        });

        Assert.True(Guid.TryParse(manifest.CaptureId, out _));
    }

    [Fact]
    public async Task RunAsync_NewExecution_GetsDifferentCaptureId()
    {
        var engine = new ScanEngine([new FakeCollector("a", true)]);
        var ctx = () => new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.Full
        };

        var first = await engine.RunAsync(ctx());
        var second = await engine.RunAsync(ctx());

        Assert.NotEqual(first.CaptureId, second.CaptureId);
    }

    [Fact]
    public async Task RunAsync_SameCaptureId_PropagatedOnContextAndResults()
    {
        var a = new FakeCollector("a", true);
        var b = new FakeCollector("b", true);
        var engine = new ScanEngine([a, b]);
        var context = new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.Full
        };

        var manifest = await engine.RunAsync(context);

        Assert.Equal(manifest.CaptureId, context.CaptureId);
        Assert.Equal(2, manifest.Results.Count);
        Assert.All(manifest.OperationCoverages, c => Assert.True(c.Requested));
    }

    [Fact]
    public async Task CapabilityCatalog_IndependentOfExecutionResults()
    {
        var empty = new FakeCollector(
            "empty",
            available: true,
            capabilities:
            [
                new DeclaredCapability("cat", "op.empty", "1.0", "empty", true, "empty op")
            ],
            collectCoverages:
            [
                CoverageSemantics.Succeeded(
                    new DeclaredCapability("cat", "op.empty", "1.0", "empty", true),
                    requested: true,
                    observedCount: 0,
                    issueIds: Array.Empty<string>())
            ]);
        var engine = new ScanEngine([empty]);

        var before = engine.BuildCapabilityCatalog();
        var manifest = await engine.RunAsync(new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.Full
        });

        Assert.Equal(before, manifest.CapabilityCatalog);
        Assert.Contains(manifest.CapabilityCatalog, c => c.OperationId == "op.empty" && c.Supported);
        Assert.Contains(
            manifest.OperationCoverages,
            c => c.OperationId == "op.empty"
                 && c.Result == OperationExecutionResult.Succeeded
                 && c.ContentAvailability == ContentAvailability.Empty);
    }

    [Fact]
    public async Task UnsupportedOperation_IsDistinguishableFromNotRequested()
    {
        var supported = new FakeCollector(
            "s",
            true,
            capabilities: [new DeclaredCapability("cat", "op.supported", "1.0", "s", true)]);
        var engine = new ScanEngine([supported]);

        var catalog = engine.BuildCapabilityCatalog();
        Assert.DoesNotContain(catalog, c => c.OperationId == "op.missing");

        var unsupported = ScannerCapabilityCatalog.Unsupported("op.missing");
        Assert.False(unsupported.Supported);

        var manifest = await engine.RunAsync(new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.Selective,
            SelectedCollectorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "s" }
        });

        Assert.DoesNotContain(manifest.OperationCoverages, c => c.OperationId == "op.missing");
        Assert.Contains(manifest.OperationCoverages, c => c.OperationId == "op.supported" && c.Requested);
    }

    [Fact]
    public async Task EmptySuccessfulOperation_HasContentEmpty()
    {
        var cap = new DeclaredCapability("cat", "op.z", "1.0", "z", true);
        var collector = new FakeCollector(
            "z",
            true,
            capabilities: [cap],
            collectCoverages: [CoverageSemantics.Succeeded(cap, true, 0, Array.Empty<string>())]);
        var engine = new ScanEngine([collector]);

        var manifest = await engine.RunAsync(new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.Full
        });

        var coverage = Assert.Single(manifest.OperationCoverages);
        Assert.Equal(OperationExecutionResult.Succeeded, coverage.Result);
        Assert.Equal(ContentAvailability.Empty, coverage.ContentAvailability);
        Assert.Equal(0, coverage.ObservedCount);
    }

    [Fact]
    public async Task AbsentStructure_IsDistinguishableFromReadFailure()
    {
        var cap = new DeclaredCapability("cat", "op.src", "1.0", "x", true);
        var absent = new FakeCollector(
            "absent",
            true,
            capabilities: [cap],
            collectCoverages: [CoverageSemantics.NotPresent(cap, true, ["issue:absent"])]);
        var failed = new FakeCollector(
            "failed",
            true,
            capabilities: [cap with { CollectorId = "failed", OperationId = "op.read" }],
            collectCoverages:
            [
                CoverageSemantics.Failed(
                    cap with { CollectorId = "failed", OperationId = "op.read" },
                    true,
                    0,
                    ["issue:fail"],
                    SourceAvailability.Present)
            ]);

        var engine = new ScanEngine([absent, failed]);
        var manifest = await engine.RunAsync(new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.Full
        });

        var absentCov = Assert.Single(manifest.OperationCoverages, c => c.OperationId == "op.src");
        Assert.Equal(SourceAvailability.NotPresent, absentCov.SourceAvailability);
        Assert.Equal(OperationExecutionResult.Succeeded, absentCov.Result);

        var failCov = Assert.Single(manifest.OperationCoverages, c => c.OperationId == "op.read");
        Assert.Equal(SourceAvailability.Present, failCov.SourceAvailability);
        Assert.Equal(OperationExecutionResult.Failed, failCov.Result);
        Assert.Equal(ContentAvailability.Unavailable, failCov.ContentAvailability);
    }

    [Fact]
    public async Task SuccessfulOperation_PreservedWhenSiblingFails()
    {
        var okCap = new DeclaredCapability("cat", "op.ok", "1.0", "mix", true);
        var failCap = new DeclaredCapability("cat", "op.fail", "1.0", "mix", true);
        var collector = new FakeCollector(
            "mix",
            true,
            capabilities: [okCap, failCap],
            collectCoverages:
            [
                CoverageSemantics.Succeeded(okCap, true, 3, Array.Empty<string>()),
                CoverageSemantics.Failed(failCap, true, 0, ["issue:op.fail"], SourceAvailability.Present)
            ],
            collectIssues:
            [
                new CollectionIssue(
                    CollectionIssueSeverity.Error,
                    "op.fail.read",
                    "falha",
                    IssueId: "issue:op.fail",
                    OperationId: "op.fail")
            ],
            collectStatus: CollectorStatus.Partial);

        var engine = new ScanEngine([collector]);
        var manifest = await engine.RunAsync(new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.Full
        });

        var ok = Assert.Single(manifest.OperationCoverages, c => c.OperationId == "op.ok");
        Assert.Equal(OperationExecutionResult.Succeeded, ok.Result);
        Assert.Equal(3, ok.ObservedCount);

        var fail = Assert.Single(manifest.OperationCoverages, c => c.OperationId == "op.fail");
        Assert.Equal(OperationExecutionResult.Failed, fail.Result);
    }

    [Fact]
    public async Task PartialFailure_LeavesRemainingAsNotAttempted()
    {
        var first = new DeclaredCapability("cat", "op.1", "1.0", "seq", true);
        var second = new DeclaredCapability("cat", "op.2", "1.0", "seq", true);
        var third = new DeclaredCapability("cat", "op.3", "1.0", "seq", true);
        var collector = new FakeCollector(
            "seq",
            true,
            capabilities: [first, second, third],
            collectCoverages:
            [
                CoverageSemantics.Succeeded(first, true, 1, Array.Empty<string>()),
                CoverageSemantics.Failed(second, true, 0, ["issue:op.2"], SourceAvailability.Present),
                CoverageSemantics.NotAttempted(third, requested: true)
            ],
            collectStatus: CollectorStatus.Partial);

        var engine = new ScanEngine([collector]);
        var manifest = await engine.RunAsync(new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.Full
        });

        Assert.Equal(OperationExecutionResult.Succeeded,
            Assert.Single(manifest.OperationCoverages, c => c.OperationId == "op.1").Result);
        Assert.Equal(OperationExecutionResult.Failed,
            Assert.Single(manifest.OperationCoverages, c => c.OperationId == "op.2").Result);
        Assert.Equal(OperationExecutionResult.NotAttempted,
            Assert.Single(manifest.OperationCoverages, c => c.OperationId == "op.3").Result);
    }

    [Fact]
    public async Task SelectiveScan_DoesNotMarkUnselectedAsAbsent()
    {
        var a = new FakeCollector(
            "a",
            true,
            capabilities: [new DeclaredCapability("cat", "op.a", "1.0", "a", true)]);
        var b = new FakeCollector(
            "b",
            true,
            capabilities: [new DeclaredCapability("cat", "op.b", "1.0", "b", true)]);
        var engine = new ScanEngine([a, b]);

        var manifest = await engine.RunAsync(new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.Selective,
            SelectedCollectorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a" }
        });

        var unselected = Assert.Single(manifest.OperationCoverages, c => c.OperationId == "op.b");
        Assert.False(unselected.Requested);
        Assert.Equal(OperationExecutionResult.Skipped, unselected.Result);
        Assert.Equal(SourceAvailability.NotApplicable, unselected.SourceAvailability);
        Assert.Equal(ContentAvailability.NotCollected, unselected.ContentAvailability);
        Assert.NotEqual(SourceAvailability.NotPresent, unselected.SourceAvailability);
    }

    [Fact]
    public async Task ProbeOnly_DoesNotDeclareContentCollected()
    {
        var collector = new FakeCollector(
            "p",
            true,
            capabilities: [new DeclaredCapability("cat", "op.p", "1.0", "p", true)]);
        var engine = new ScanEngine([collector]);

        var manifest = await engine.RunAsync(new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.ProbeOnly
        });

        var coverage = Assert.Single(manifest.OperationCoverages);
        Assert.Equal(ContentAvailability.NotCollected, coverage.ContentAvailability);
        Assert.Equal(OperationExecutionResult.Succeeded, coverage.Result);
        Assert.Equal(0, collector.CollectCalls);
    }

    [Fact]
    public async Task Issues_ReferenceCorrectOperation()
    {
        var cap = new DeclaredCapability("cat", "op.issue", "1.0", "i", true);
        var collector = new FakeCollector(
            "i",
            true,
            capabilities: [cap],
            collectCoverages:
            [
                CoverageSemantics.Failed(cap, true, 0, ["issue:op.issue:1"], SourceAvailability.Present)
            ],
            collectIssues:
            [
                new CollectionIssue(
                    CollectionIssueSeverity.Error,
                    "op.issue.read",
                    "erro",
                    IssueId: "issue:op.issue:1",
                    OperationId: "op.issue")
            ],
            collectStatus: CollectorStatus.Failed);

        var engine = new ScanEngine([collector]);
        var manifest = await engine.RunAsync(new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.Full
        });

        var issue = Assert.Single(manifest.Results[0].Issues);
        Assert.Equal("op.issue", issue.OperationId);
        Assert.Equal("issue:op.issue:1", issue.IssueId);
        Assert.Contains(
            Assert.Single(manifest.OperationCoverages).IssueIds,
            id => id == issue.IssueId);
    }

    [Fact]
    public async Task RunAsync_UnhandledException_IsCollectorLevel_AllOperationsNotAttempted()
    {
        var caps = new[]
        {
            new DeclaredCapability("cat", "op.x", "1.0", "boom", true),
            new DeclaredCapability("cat", "op.y", "1.0", "boom", true)
        };
        var collector = new FakeCollector("boom", true, capabilities: caps, throwOnCollect: true);
        var engine = new ScanEngine([collector]);

        var manifest = await engine.RunAsync(new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.Full
        });

        Assert.Equal(CollectorStatus.Failed, manifest.Results[0].Status);
        var issue = Assert.Single(manifest.Results[0].Issues);
        Assert.Equal("collector.unhandled", issue.Code);
        Assert.Null(issue.OperationId);
        Assert.All(
            manifest.OperationCoverages,
            c => Assert.Equal(OperationExecutionResult.NotAttempted, c.Result));
        Assert.All(
            manifest.OperationCoverages,
            c => Assert.Empty(c.IssueIds));
        Assert.DoesNotContain(
            manifest.OperationCoverages,
            c => c.Result is OperationExecutionResult.Failed or OperationExecutionResult.Succeeded);
    }

    [Fact]
    public void FromIssues_InfoOnly_RemainsSucceededWithEmptyContent()
    {
        var cap = new DeclaredCapability("cat", "op.info", "1.0", "c", true);
        var coverage = CoverageSemantics.FromIssues(
            cap,
            requested: true,
            observedCount: 0,
            [
                new CollectionIssue(
                    CollectionIssueSeverity.Info,
                    "op.info.empty",
                    "sem registos",
                    IssueId: "issue:info",
                    OperationId: "op.info")
            ]);

        Assert.Equal(OperationExecutionResult.Succeeded, coverage.Result);
        Assert.Equal(ContentAvailability.Empty, coverage.ContentAvailability);
        Assert.Contains("issue:info", coverage.IssueIds);
    }

    [Fact]
    public void FromIssues_Warning_YieldsSucceededWithWarnings()
    {
        var cap = new DeclaredCapability("cat", "op.warn", "1.0", "c", true);
        var coverage = CoverageSemantics.FromIssues(
            cap,
            requested: true,
            observedCount: 2,
            [
                new CollectionIssue(
                    CollectionIssueSeverity.Warning,
                    "op.warn.partial_meta",
                    "metadados incompletos",
                    IssueId: "issue:warn",
                    OperationId: "op.warn")
            ]);

        Assert.Equal(OperationExecutionResult.SucceededWithWarnings, coverage.Result);
        Assert.Equal(ContentAvailability.Available, coverage.ContentAvailability);
        Assert.Contains("issue:warn", coverage.IssueIds);
    }

    [Fact]
    public void FromIssues_Error_YieldsFailed()
    {
        var cap = new DeclaredCapability("cat", "op.err", "1.0", "c", true);
        var coverage = CoverageSemantics.FromIssues(
            cap,
            requested: true,
            observedCount: 0,
            [
                new CollectionIssue(
                    CollectionIssueSeverity.Error,
                    "op.err.read",
                    "falha de leitura",
                    IssueId: "issue:err",
                    OperationId: "op.err")
            ]);

        Assert.Equal(OperationExecutionResult.Failed, coverage.Result);
        Assert.Equal(ContentAvailability.Unavailable, coverage.ContentAvailability);
        Assert.Equal(SourceAvailability.Present, coverage.SourceAvailability);
        Assert.Contains("issue:err", coverage.IssueIds);
    }

    [Fact]
    public void Succeeded_DoesNotInferWarningsFromIssueIdsAlone()
    {
        var cap = new DeclaredCapability("cat", "op.ids", "1.0", "c", true);
        var coverage = CoverageSemantics.Succeeded(cap, true, 0, ["issue:any"]);

        Assert.Equal(OperationExecutionResult.Succeeded, coverage.Result);
        Assert.Equal(ContentAvailability.Empty, coverage.ContentAvailability);
    }

    [Fact]
    public void BuildCapabilityCatalog_CollapsesIdenticalDuplicates()
    {
        var cap = new DeclaredCapability("cat", "op.same", "1.0", "a", true, "primeira");
        var identical = cap with { Description = "segunda descrição" };
        var a = new FakeCollector("a", true, capabilities: [cap, identical]);
        var engine = new ScanEngine([a]);

        var catalog = engine.BuildCapabilityCatalog();
        var entry = Assert.Single(catalog, c => c.OperationId == "op.same");
        Assert.Equal("primeira", entry.Description);
    }

    [Fact]
    public void BuildCapabilityCatalog_RejectsIncompatibleDuplicates()
    {
        var left = new DeclaredCapability("cat", "op.conflict", "1.0", "a", true);
        var right = new DeclaredCapability("cat", "op.conflict", "2.0", "a", true);
        var a = new FakeCollector("a", true, capabilities: [left]);
        var b = new FakeCollector("b", true, capabilities: [right with { CollectorId = "b" }]);

        var ex = Assert.Throws<InvalidOperationException>(() => new ScanEngine([a, b]));
        Assert.Contains("op.conflict", ex.Message);
        Assert.Contains("incompatíveis", ex.Message);
    }

    [Fact]
    public void CapabilityCatalogBuilder_RejectsDifferentCollectorSameOperation()
    {
        var left = new DeclaredCapability("cat", "op.x", "1.0", "a", true);
        var right = new DeclaredCapability("cat", "op.x", "1.0", "b", true);

        var ex = Assert.Throws<InvalidOperationException>(
            () => CapabilityCatalogBuilder.Build([left, right]));
        Assert.Contains("op.x", ex.Message);
    }

    [Fact]
    public async Task ExceptionAfterSuccessfulOperation_PreservesKnownCoverage()
    {
        var ok = new DeclaredCapability("cat", "op.ok", "1.0", "c", true);
        var later = new DeclaredCapability("cat", "op.later", "1.0", "c", true);
        var okCoverage = CoverageSemantics.Succeeded(ok, true, 4, Array.Empty<string>());
        var collector = new FakeCollector(
            "c",
            true,
            capabilities: [ok, later],
            throwOnCollect: true,
            beforeThrow: ctx => ctx.ExecutionProgress.CompleteOperation(okCoverage));

        var manifest = await new ScanEngine([collector]).RunAsync(new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.Full
        });

        var preserved = Assert.Single(manifest.OperationCoverages, c => c.OperationId == "op.ok");
        Assert.Equal(OperationExecutionResult.Succeeded, preserved.Result);
        Assert.Equal(4, preserved.ObservedCount);
        Assert.Equal(ContentAvailability.Available, preserved.ContentAvailability);

        var pending = Assert.Single(manifest.OperationCoverages, c => c.OperationId == "op.later");
        Assert.Equal(OperationExecutionResult.NotAttempted, pending.Result);

        Assert.Equal(2, manifest.OperationCoverages.Count);
        Assert.Equal(2, manifest.OperationCoverages.Select(c => c.OperationId).Distinct().Count());
    }

    [Fact]
    public async Task ExceptionAfterMixedResults_PreservesSuccessAndFailure()
    {
        var ok = new DeclaredCapability("cat", "op.ok", "1.0", "mix", true);
        var fail = new DeclaredCapability("cat", "op.fail", "1.0", "mix", true);
        var pending = new DeclaredCapability("cat", "op.pending", "1.0", "mix", true);
        var failIssue = new CollectionIssue(
            CollectionIssueSeverity.Error,
            "op.fail.read",
            "falhou",
            IssueId: "issue:op.fail",
            OperationId: "op.fail");

        var collector = new FakeCollector(
            "mix",
            true,
            capabilities: [ok, fail, pending],
            throwOnCollect: true,
            beforeThrow: ctx =>
            {
                ctx.ExecutionProgress.CompleteOperation(
                    CoverageSemantics.Succeeded(ok, true, 2, Array.Empty<string>()));
                ctx.ExecutionProgress.ReportIssue(failIssue);
                ctx.ExecutionProgress.CompleteOperation(
                    CoverageSemantics.Failed(fail, true, 0, ["issue:op.fail"]));
            });

        var manifest = await new ScanEngine([collector]).RunAsync(new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.Full
        });

        Assert.Equal(OperationExecutionResult.Succeeded,
            Assert.Single(manifest.OperationCoverages, c => c.OperationId == "op.ok").Result);
        var failed = Assert.Single(manifest.OperationCoverages, c => c.OperationId == "op.fail");
        Assert.Equal(OperationExecutionResult.Failed, failed.Result);
        Assert.Contains("issue:op.fail", failed.IssueIds);
        Assert.Equal(OperationExecutionResult.NotAttempted,
            Assert.Single(manifest.OperationCoverages, c => c.OperationId == "op.pending").Result);
        Assert.Equal(3, manifest.OperationCoverages.Count);
    }

    [Fact]
    public async Task ExceptionWhileOperationInFlight_LeavesItNotAttempted()
    {
        var inFlight = new DeclaredCapability("cat", "op.inflight", "1.0", "f", true);
        var other = new DeclaredCapability("cat", "op.other", "1.0", "f", true);
        var collector = new FakeCollector(
            "f",
            true,
            capabilities: [inFlight, other],
            throwOnCollect: true,
            beforeThrow: ctx => ctx.ExecutionProgress.BeginOperation(inFlight.OperationId));

        var manifest = await new ScanEngine([collector]).RunAsync(new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.Full
        });

        Assert.All(
            manifest.OperationCoverages,
            c => Assert.Equal(OperationExecutionResult.NotAttempted, c.Result));
        Assert.DoesNotContain(
            manifest.OperationCoverages,
            c => c.Result is OperationExecutionResult.Succeeded or OperationExecutionResult.Failed);
        Assert.Equal(2, manifest.OperationCoverages.Count);
    }

    [Fact]
    public async Task ExceptionBeforeAnyOperation_AllNotAttempted_CollectorIssueOnly()
    {
        var caps = new[]
        {
            new DeclaredCapability("cat", "op.a", "1.0", "early", true),
            new DeclaredCapability("cat", "op.b", "1.0", "early", true)
        };
        var collector = new FakeCollector("early", true, capabilities: caps, throwOnCollect: true);
        var manifest = await new ScanEngine([collector]).RunAsync(new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.Full
        });

        Assert.All(manifest.OperationCoverages, c => Assert.Equal(OperationExecutionResult.NotAttempted, c.Result));
        var issue = Assert.Single(manifest.Results[0].Issues);
        Assert.Equal("collector.unhandled", issue.Code);
        Assert.Null(issue.OperationId);
    }

    [Fact]
    public async Task PartialProgressAndFinalResult_DoNotDuplicateCoverages()
    {
        var cap = new DeclaredCapability("cat", "op.one", "1.0", "d", true);
        var coverage = CoverageSemantics.Succeeded(cap, true, 1, Array.Empty<string>());
        var collector = new FakeCollector(
            "d",
            true,
            capabilities: [cap],
            collectCoverages: [coverage, coverage],
            beforeThrow: null);

        var manifest = await new ScanEngine([collector]).RunAsync(new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.Full
        });

        Assert.Single(manifest.OperationCoverages);
        Assert.Single(manifest.Results[0].Coverages);
    }

    [Fact]
    public async Task PersistenceFailureAfterKnownCoverage_KeepsExecutionResult()
    {
        // Simula falha de finalização/persistência após operações já publicadas no progresso.
        var rel = new DeclaredCapability("cat", "op.rel", "1.0", "persist", true);
        var dtl = new DeclaredCapability("cat", "op.dtl", "1.0", "persist", true);
        var relCov = CoverageSemantics.Succeeded(rel, true, 10, Array.Empty<string>());
        var dtlCov = CoverageSemantics.Succeeded(dtl, true, 3, Array.Empty<string>());
        var collector = new FakeCollector(
            "persist",
            true,
            capabilities: [rel, dtl],
            throwOnCollect: true,
            beforeThrow: ctx =>
            {
                ctx.ExecutionProgress.CompleteOperation(relCov);
                ctx.ExecutionProgress.CompleteOperation(dtlCov);
            });

        var manifest = await new ScanEngine([collector]).RunAsync(new ScanContext
        {
            InstallationId = "test",
            OutputRoot = Path.GetTempPath(),
            Mode = ScanMode.Full
        });

        Assert.Equal(CollectorStatus.Failed, manifest.Results[0].Status);
        Assert.All(
            manifest.OperationCoverages,
            c => Assert.Equal(OperationExecutionResult.Succeeded, c.Result));
        Assert.Equal(10, Assert.Single(manifest.OperationCoverages, c => c.OperationId == "op.rel").ObservedCount);
        Assert.Equal(3, Assert.Single(manifest.OperationCoverages, c => c.OperationId == "op.dtl").ObservedCount);

        var collectorIssue = Assert.Single(
            manifest.Results[0].Issues,
            i => i.Code == "collector.unhandled");
        Assert.Null(collectorIssue.OperationId);
        Assert.All(manifest.OperationCoverages, c => Assert.DoesNotContain(collectorIssue.IssueId!, c.IssueIds));
    }

    [Fact]
    public void MergeKnownCoverages_DeduplicatesAndFillsNotAttempted()
    {
        var a = new DeclaredCapability("cat", "op.a", "1.0", "m", true);
        var b = new DeclaredCapability("cat", "op.b", "1.0", "m", true);
        var known = new[]
        {
            CoverageSemantics.Succeeded(a, true, 1, Array.Empty<string>()),
            CoverageSemantics.Succeeded(a, true, 99, Array.Empty<string>())
        };

        var merged = ScanEngine.MergeKnownCoveragesWithNotAttempted([a, b], known);

        Assert.Equal(2, merged.Count);
        Assert.Equal(1, Assert.Single(merged, c => c.OperationId == "op.a").ObservedCount);
        Assert.Equal(OperationExecutionResult.NotAttempted,
            Assert.Single(merged, c => c.OperationId == "op.b").Result);
    }

    private sealed class FakeCollector : ICollector
    {
        private readonly bool _available;
        private readonly string? _reason;
        private readonly IReadOnlyList<DeclaredCapability> _capabilities;
        private readonly IReadOnlyList<OperationCoverage>? _collectCoverages;
        private readonly IReadOnlyList<CollectionIssue> _collectIssues;
        private readonly CollectorStatus _collectStatus;
        private readonly bool _throwOnCollect;
        private readonly Action<ScanContext>? _beforeThrow;

        public FakeCollector(
            string id,
            bool available,
            string? reason = null,
            IReadOnlyList<DeclaredCapability>? capabilities = null,
            IReadOnlyList<OperationCoverage>? collectCoverages = null,
            IReadOnlyList<CollectionIssue>? collectIssues = null,
            CollectorStatus collectStatus = CollectorStatus.Succeeded,
            bool throwOnCollect = false,
            Action<ScanContext>? beforeThrow = null)
        {
            Id = id;
            _available = available;
            _reason = reason;
            _capabilities = capabilities ??
            [
                new DeclaredCapability("test", id + ".op", "1.0", id, true, "fake")
            ];
            _collectCoverages = collectCoverages;
            _collectIssues = collectIssues ?? Array.Empty<CollectionIssue>();
            _collectStatus = collectStatus;
            _throwOnCollect = throwOnCollect;
            _beforeThrow = beforeThrow;
        }

        public string Id { get; }
        public string DisplayName => Id;
        public IReadOnlyList<DeclaredCapability> DeclaredCapabilities => _capabilities;
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
            _beforeThrow?.Invoke(context);
            if (_throwOnCollect)
            {
                throw new InvalidOperationException("boom");
            }

            var coverages = _collectCoverages ?? _capabilities
                .Select(c => CoverageSemantics.Succeeded(c, true, 1, Array.Empty<string>()))
                .ToArray();

            return Task.FromResult(new CollectorResult(
                Id,
                _collectStatus,
                Array.Empty<CollectedArtifact>(),
                _collectIssues,
                TimeSpan.FromMilliseconds(1),
                coverages));
        }
    }
}
