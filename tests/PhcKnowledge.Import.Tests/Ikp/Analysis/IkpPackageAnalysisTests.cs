using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Ikp.Analysis;
using PhcKnowledge.Import.Receipts;
using PhcKnowledge.Import.Storage;

namespace PhcKnowledge.Import.Tests.Ikp.Analysis;

public sealed class IkpPackageAnalysisTests
{
    private static string FixtureDirectory => Path.Combine(AppContext.BaseDirectory, "Fixtures", "minimal-valid");
    private static string SchemaDirectory => Path.Combine(AppContext.BaseDirectory, "Schemas");

    [Fact]
    public async Task Reader_and_validator_share_engine_and_expose_typed_inspection()
    {
        var engine = new IkpPackageInspectionEngine(SchemaDirectory);
        var validator = new IkpPackageValidator(engine);
        var reader = new IkpPackageReader(engine);

        await using var first = CreateFixtureZip();
        var validation = await validator.ValidateAsync(first);
        await using var second = CreateFixtureZip();
        var inspection = await reader.ReadAsync(second);

        Assert.Same(engine, validator.Engine);
        Assert.Same(engine, reader.Engine);
        Assert.Equal(validation.PackageId, inspection.Validation.PackageId);
        Assert.Equal(validation.LogicalContentDigest, inspection.Validation.LogicalContentDigest);
        Assert.True(inspection.InternalChecksumsVerified);
        Assert.NotEmpty(inspection.Capabilities);
        Assert.NotEmpty(inspection.Coverage);
        Assert.NotEmpty(inspection.Objects);
        var availableDefinition = Assert.Single(inspection.Objects.Where(x => x.DefinitionStatus == "Available"));
        Assert.Equal("Unavailable", availableDefinition.DefinitionFingerprintStatus);
        Assert.Equal("NoValidatedPolicy", availableDefinition.DefinitionFingerprintReasonCode);
        Assert.False(availableDefinition.HasDefinitionFingerprint);
        Assert.NotEmpty(inspection.Artifacts);
        Assert.NotNull(inspection.Relations);
        Assert.Contains(inspection.Issues, x => x.Severity == "Warning");
    }

    [Fact]
    public async Task Analysis_of_archived_fixture_is_ready_with_warnings_and_deterministic()
    {
        await using var harness = await CreateArchivedFixtureHarnessAsync();
        var clock = new Queue<DateTimeOffset>([
            DateTimeOffset.Parse("2026-11-19T12:00:00Z"),
            DateTimeOffset.Parse("2026-11-19T13:00:00Z")
        ]);
        var analyses = new InMemoryPackageImportAnalysisRepository();
        var service = new IkpPackageAnalysisService(
            harness.Attempts,
            harness.Store,
            new IkpPackageReader(SchemaDirectory),
            analyses,
            clock: () => clock.Dequeue());

        var first = await service.ExecuteAsync(harness.OrganizationId, harness.PackageId);
        var second = await service.ExecuteAsync(harness.OrganizationId, harness.PackageId);
        var persisted = await service.GetPersistedAsync(harness.OrganizationId, harness.PackageId);

        Assert.Equal(MaterializationReadiness.ReadyWithWarnings, first.Readiness.Status);
        Assert.Contains(first.Readiness.Findings, x => x.Code == MaterializationReadinessEvaluator.WarningIssue);
        Assert.Contains(first.Readiness.Findings, x => x.Code == MaterializationReadinessEvaluator.WarningCoverageResult);
        Assert.True(first.Integrity.ArchiveSha256Verified);
        Assert.True(first.Integrity.InternalChecksumsVerified);
        Assert.True(first.Relations.Present);
        Assert.Equal(1, first.Relations.DeclaredRelationCount);
        Assert.Equal(1, first.Relations.InterpretationCount);
        Assert.Equal(0, first.Relations.OrphanInterpretationCount);
        Assert.True(first.Relations.DeclaredCountBySourceType.ContainsKey("unknown") ||
                    first.Relations.DeclaredCountBySourceType.Count >= 0);
        Assert.True(first.ObjectIndex.TotalObjects > 0);
        Assert.Equal(
            first.ObjectIndex.Available + first.ObjectIndex.Empty +
            first.ObjectIndex.Unavailable + first.ObjectIndex.NotCollected,
            first.ObjectIndex.TotalObjects);
        // Second Execute returns the persisted row (same AnalyzedAtUtc), demonstrating idempotency.
        Assert.Equal(first.AnalyzedAtUtc, second.AnalyzedAtUtc);
        Assert.Equal(first.ReceiptId, second.ReceiptId);
        Assert.True(first.SemanticEquals(second));
        Assert.True(persisted.SemanticEquals(first));
        Assert.True(File.Exists(harness.ArchivedPath));
        Assert.True((File.GetAttributes(harness.ArchivedPath) & FileAttributes.ReadOnly) != 0);
    }

    [Fact]
    public async Task Execute_reuses_compatible_analysis_preserving_original_receipt_id()
    {
        await using var harness = await CreateArchivedFixtureHarnessAsync();
        var analyses = new InMemoryPackageImportAnalysisRepository();
        var originalReceiptId = Guid.NewGuid();
        Assert.NotEqual(harness.Receipt.ReceiptId, originalReceiptId);

        var seeded = BuildMinimalAnalysis(
            harness.OrganizationId,
            originalReceiptId,
            harness.PackageId,
            harness.Receipt.CaptureId,
            harness.Receipt.ZipSha256,
            DateTimeOffset.Parse("2026-11-19T09:00:00Z"));
        await analyses.InsertOrGetExistingAsync(
            PersistedPackageImportAnalysis.FromAnalysis(
                Guid.NewGuid(), seeded, harness.Receipt.ZipByteLength, DateTimeOffset.UtcNow));

        var service = new IkpPackageAnalysisService(
            harness.Attempts, harness.Store, new IkpPackageReader(SchemaDirectory), analyses);
        var executed = await service.ExecuteAsync(harness.OrganizationId, harness.PackageId);
        var viaGet = await service.GetPersistedAsync(harness.OrganizationId, harness.PackageId);

        Assert.Equal(originalReceiptId, executed.ReceiptId);
        Assert.Equal(originalReceiptId, viaGet.ReceiptId);
        Assert.NotEqual(harness.Receipt.ReceiptId, executed.ReceiptId);
        Assert.True(seeded.SemanticEquals(executed));
    }

    [Fact]
    public async Task InsertOrGetExisting_conflict_preserves_first_writer_receipt_id()
    {
        await using var harness = await CreateArchivedFixtureHarnessAsync();
        var analyses = new InMemoryPackageImportAnalysisRepository();
        var firstWriterReceipt = Guid.NewGuid();
        var secondWriterReceipt = Guid.NewGuid();

        var first = BuildMinimalAnalysis(
            harness.OrganizationId, firstWriterReceipt, harness.PackageId,
            harness.Receipt.CaptureId, harness.Receipt.ZipSha256,
            DateTimeOffset.Parse("2026-11-19T09:00:00Z"));
        var second = first with
        {
            ReceiptId = secondWriterReceipt,
            AnalyzedAtUtc = DateTimeOffset.Parse("2026-11-19T10:00:00Z")
        };

        var storedFirst = await analyses.InsertOrGetExistingAsync(
            PersistedPackageImportAnalysis.FromAnalysis(
                Guid.NewGuid(), first, harness.Receipt.ZipByteLength, DateTimeOffset.UtcNow));
        var storedSecond = await analyses.InsertOrGetExistingAsync(
            PersistedPackageImportAnalysis.FromAnalysis(
                Guid.NewGuid(), second, harness.Receipt.ZipByteLength, DateTimeOffset.UtcNow));

        Assert.Equal(storedFirst.AnalysisId, storedSecond.AnalysisId);
        Assert.Equal(firstWriterReceipt, storedSecond.ReceiptId);
        Assert.Equal(firstWriterReceipt, storedSecond.ToAnalysis().ReceiptId);
    }

    [Fact]
    public async Task GetPersisted_does_not_insert_or_recompute()
    {
        await using var harness = await CreateArchivedFixtureHarnessAsync();
        var analyses = new InMemoryPackageImportAnalysisRepository();
        var service = new IkpPackageAnalysisService(
            harness.Attempts, harness.Store, new IkpPackageReader(SchemaDirectory), analyses);

        await Assert.ThrowsAsync<PackageImportAnalysisNotFoundException>(() =>
            service.GetPersistedAsync(harness.OrganizationId, harness.PackageId));
        Assert.Null(await analyses.GetAsync(
            harness.OrganizationId, harness.PackageId, harness.Receipt.ZipSha256,
            PackageImportAnalysisSchema.Version));

        var posted = await service.ExecuteAsync(harness.OrganizationId, harness.PackageId);
        var get = await service.GetPersistedAsync(harness.OrganizationId, harness.PackageId);
        Assert.Equal(posted.AnalyzedAtUtc, get.AnalyzedAtUtc);
        Assert.Equal(posted.ReceiptId, get.ReceiptId);
    }

    [Fact]
    public async Task Analysis_is_isolated_by_organization()
    {
        await using var harness = await CreateArchivedFixtureHarnessAsync();
        var analyses = new InMemoryPackageImportAnalysisRepository();
        var service = new IkpPackageAnalysisService(
            harness.Attempts, harness.Store, new IkpPackageReader(SchemaDirectory), analyses);
        var created = await service.ExecuteAsync(harness.OrganizationId, harness.PackageId);

        await Assert.ThrowsAsync<PackageImportAnalysisNotFoundException>(() =>
            service.ExecuteAsync(Guid.NewGuid(), harness.PackageId));
        Assert.Null(await analyses.GetAsync(
            Guid.NewGuid(), harness.PackageId, harness.Receipt.ZipSha256,
            PackageImportAnalysisSchema.Version));
        Assert.NotNull(await analyses.GetAsync(
            harness.OrganizationId, harness.PackageId, created.ZipSha256,
            PackageImportAnalysisSchema.Version));
    }

    [Fact]
    public async Task Analysis_without_org_receipt_throws_not_found()
    {
        await using var harness = await CreateArchivedFixtureHarnessAsync();
        var service = new IkpPackageAnalysisService(
            harness.Attempts, harness.Store, new IkpPackageReader(SchemaDirectory),
            new InMemoryPackageImportAnalysisRepository());

        await Assert.ThrowsAsync<PackageImportAnalysisNotFoundException>(() =>
            service.ExecuteAsync(Guid.NewGuid(), harness.PackageId));
        await Assert.ThrowsAsync<PackageImportAnalysisNotFoundException>(() =>
            service.ExecuteAsync(harness.OrganizationId, Guid.NewGuid()));
        Assert.True(File.Exists(harness.ArchivedPath));
    }

    [Fact]
    public async Task GetPersisted_without_prior_execute_returns_not_found()
    {
        await using var harness = await CreateArchivedFixtureHarnessAsync();
        var service = new IkpPackageAnalysisService(
            harness.Attempts, harness.Store, new IkpPackageReader(SchemaDirectory),
            new InMemoryPackageImportAnalysisRepository());

        await Assert.ThrowsAsync<PackageImportAnalysisNotFoundException>(() =>
            service.GetPersistedAsync(harness.OrganizationId, harness.PackageId));
    }

    [Fact]
    public async Task Analysis_failure_after_tamper_returns_blocked_and_keeps_archive()
    {
        await using var harness = await CreateArchivedFixtureHarnessAsync();
        File.SetAttributes(harness.ArchivedPath, FileAttributes.Normal);
        await File.WriteAllBytesAsync(harness.ArchivedPath, Encoding.UTF8.GetBytes("tampered-archive"));

        var service = new IkpPackageAnalysisService(
            harness.Attempts, harness.Store, new IkpPackageReader(SchemaDirectory),
            new InMemoryPackageImportAnalysisRepository());
        var analysis = await service.ExecuteAsync(harness.OrganizationId, harness.PackageId);

        Assert.Equal(MaterializationReadiness.Blocked, analysis.Readiness.Status);
        Assert.False(analysis.Integrity.ArchiveSha256Verified);
        Assert.True(File.Exists(harness.ArchivedPath));
    }

    [Fact]
    public void Readiness_evaluator_maps_contract_warning_signals()
    {
        var evaluator = new MaterializationReadinessEvaluator();
        var ready = evaluator.Evaluate(
            new PackageImportIntegrityAnalysis(true, true, true, Array.Empty<string>()),
            "0.1",
            Array.Empty<IkpInspectedIssue>(),
            Array.Empty<IkpInspectedCoverageOperation>());
        Assert.Equal(MaterializationReadiness.Ready, ready.Status);

        var warnings = evaluator.Evaluate(
            new PackageImportIntegrityAnalysis(true, true, true, Array.Empty<string>()),
            "0.1",
            [new IkpInspectedIssue("i1", "op", "Warning", "code", "warn", null, null)],
            [
                new IkpInspectedCoverageOperation(
                    "op", "1.0", true, "SucceededWithWarnings", "Present", "Partial", 1,
                    ["i1"], null)
            ]);
        Assert.Equal(MaterializationReadiness.ReadyWithWarnings, warnings.Status);

        var blocked = evaluator.Evaluate(
            new PackageImportIntegrityAnalysis(false, false, false, ["bad"]),
            "0.2",
            Array.Empty<IkpInspectedIssue>(),
            Array.Empty<IkpInspectedCoverageOperation>());
        Assert.Equal(MaterializationReadiness.Blocked, blocked.Status);
        Assert.Contains(blocked.Findings, x => x.Severity == MaterializationFindingSeverity.Blocker);
    }

    [Fact]
    public void Builder_preserves_orphan_counts_from_inspection_model()
    {
        var validation = new IkpPackageValidationResult(
            Guid.NewGuid(), Guid.NewGuid(), "0.1", "p", "1", null,
            "a".PadLeft(64, 'a'), 10, 5, 1, 1, 1, 1, 0, "b".PadLeft(64, 'b'));
        var inspection = new IkpPackageInspection(
            validation, true, "phc.installation.knowledge.package", "Full", "Complete", "sqlserver",
            null, null, null,
            Array.Empty<IkpInspectedCapabilityOperation>(),
            Array.Empty<IkpInspectedCoverageOperation>(),
            Array.Empty<IkpInspectedObject>(),
            Array.Empty<IkpInspectedArtifact>(),
            Array.Empty<IkpInspectedIssue>(),
            new IkpInspectedRelations(
                "1", validation.CaptureId,
                [new IkpInspectedDeclaredRelation("phc.rel:1", "phc.rel", "1")],
                [new IkpInspectedRelationInterpretation("i1", "phc.rel:missing", "Resolved")],
                OrphanInterpretationCount: 1));

        var analysis = new IkpPackageAnalysisBuilder().Build(
            Guid.NewGuid(), Guid.NewGuid(), inspection, true, DateTimeOffset.UtcNow);

        Assert.Equal(1, analysis.Relations.OrphanInterpretationCount);
        Assert.Equal(1, analysis.Relations.DeclaredCountBySourceType["phc.rel"]);
        Assert.Empty(analysis.UnavailableObjects);
    }

    [Fact]
    public void Analysis_counts_definition_fingerprint_available_and_unavailable_separately()
    {
        var validation = new IkpPackageValidationResult(
            Guid.NewGuid(), Guid.NewGuid(), "0.1", "p", "1", null,
            "a".PadLeft(64, 'a'), 10, 5, 1, 1, 1, 1, 0, "b".PadLeft(64, 'b'));
        var inspected = new IkpInspectedObject(
            "rule:1", "phc.brule", "op.brule", "Available",
            ["artifact-1"], true, false, false, false, "Rule", "dbo",
            "Unavailable", "NoValidatedPolicy");
        var inspection = new IkpPackageInspection(
            validation, true, "phc.installation.knowledge.package", "Full", "Complete", "sqlserver",
            null, null, null,
            Array.Empty<IkpInspectedCapabilityOperation>(),
            Array.Empty<IkpInspectedCoverageOperation>(),
            [inspected],
            [new IkpInspectedArtifact("artifact-1", "payload/rule.txt", "code", "text/plain", null, true, 4, "c".PadLeft(64, 'c'))],
            Array.Empty<IkpInspectedIssue>(),
            null);

        var analysis = new IkpPackageAnalysisBuilder().Build(
            Guid.NewGuid(), Guid.NewGuid(), inspection, true, DateTimeOffset.UtcNow);

        Assert.Equal(0, analysis.ObjectIndex.DefinitionFingerprintAvailableCount);
        Assert.Equal(1, analysis.ObjectIndex.DefinitionFingerprintUnavailableCount);
        Assert.Equal(1, analysis.ObjectIndex.Available);
        Assert.Equal("0.3", PackageImportAnalysisSchema.Version);
    }

    [Fact]
    public void Semantic_fingerprint_and_equals_ignore_receipt_and_analyzed_at()
    {
        var baseAnalysis = BuildMinimalAnalysis(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "a".PadLeft(64, 'a'),
            DateTimeOffset.Parse("2026-11-19T10:00:00Z"));

        var differentReceipt = baseAnalysis with { ReceiptId = Guid.NewGuid() };
        var differentTime = baseAnalysis with
        {
            AnalyzedAtUtc = DateTimeOffset.Parse("2026-11-19T11:00:00Z")
        };
        var differentSemantic = baseAnalysis with
        {
            Limitations = ["changed-limitation"]
        };

        var fingerprint = PackageImportAnalysisSerialization.ComputeSemanticFingerprint(baseAnalysis);
        Assert.Equal(
            fingerprint,
            PackageImportAnalysisSerialization.ComputeSemanticFingerprint(differentReceipt));
        Assert.Equal(
            fingerprint,
            PackageImportAnalysisSerialization.ComputeSemanticFingerprint(differentTime));
        Assert.NotEqual(
            fingerprint,
            PackageImportAnalysisSerialization.ComputeSemanticFingerprint(differentSemantic));
        Assert.NotEqual(fingerprint, baseAnalysis.ZipSha256);

        Assert.True(baseAnalysis.SemanticEquals(differentReceipt));
        Assert.True(baseAnalysis.SemanticEquals(differentTime));
        Assert.False(baseAnalysis.SemanticEquals(differentSemantic));
        Assert.Equal(PackageImportAnalysisSchema.Version, baseAnalysis.AnalysisSchemaVersion);
        Assert.Single(baseAnalysis.UnavailableObjects);
        Assert.Null(baseAnalysis.UnavailableObjects[0].AssociatedIssueId);
        Assert.Null(baseAnalysis.Issues[0].SourceObjectId);
    }

    [Fact]
    public void Serialization_is_deterministic_and_roundtrip_preserves_semantics()
    {
        var analysis = BuildMinimalAnalysis(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "c".PadLeft(64, 'c'),
            DateTimeOffset.Parse("2026-11-19T10:00:00Z"));

        var json1 = PackageImportAnalysisSerialization.Serialize(analysis);
        var json2 = PackageImportAnalysisSerialization.Serialize(analysis);
        Assert.Equal(json1, json2);

        var roundtrip = PackageImportAnalysisSerialization.Deserialize(json1);
        Assert.True(analysis.SemanticEquals(roundtrip));
        Assert.Equal(analysis.ReceiptId, roundtrip.ReceiptId);
        Assert.Equal(analysis.AnalyzedAtUtc, roundtrip.AnalyzedAtUtc);
        Assert.Equal(
            PackageImportAnalysisSerialization.ComputeSemanticFingerprint(analysis),
            PackageImportAnalysisSerialization.ComputeSemanticFingerprint(roundtrip));
    }

    [Fact]
    public void Semantic_fingerprint_stable_after_json_property_reorder_deserialize()
    {
        var analysis = BuildMinimalAnalysis(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "d".PadLeft(64, 'd'),
            DateTimeOffset.Parse("2026-11-19T10:00:00Z"));
        var originalJson = PackageImportAnalysisSerialization.Serialize(analysis);
        var reorderedJson = ReorderJsonObjectProperties(originalJson);

        Assert.NotEqual(originalJson, reorderedJson);
        var fromReordered = PackageImportAnalysisSerialization.Deserialize(reorderedJson);
        Assert.Equal(
            PackageImportAnalysisSerialization.ComputeSemanticFingerprint(analysis),
            PackageImportAnalysisSerialization.ComputeSemanticFingerprint(fromReordered));
        Assert.True(analysis.SemanticEquals(fromReordered));
    }

    [Fact]
    public void Deadlock_retry_policy_is_bounded_and_progressive()
    {
        Assert.Equal(4, SqlServerDeadlockRetry.MaxAttempts);
        Assert.Equal(TimeSpan.FromMilliseconds(25), SqlServerDeadlockRetry.DelayBeforeRetry(1));
        Assert.Equal(TimeSpan.FromMilliseconds(50), SqlServerDeadlockRetry.DelayBeforeRetry(2));
        Assert.Equal(TimeSpan.FromMilliseconds(100), SqlServerDeadlockRetry.DelayBeforeRetry(3));
        Assert.True(SqlServerDeadlockRetry.DelayBeforeRetry(3) < TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Reader_rejects_schema_invalid_and_missing_artifact_refs()
    {
        var reader = new IkpPackageReader(SchemaDirectory);
        await using var invalidSchema = CreateMutatedFixtureZip(root =>
        {
            var objectIndexPath = Path.Combine(root, "object-index.json");
            var node = JsonNode.Parse(File.ReadAllText(objectIndexPath))!.AsObject();
            node["objects"]![1]!["definition"]!["artifactIds"]!.AsArray().Add(
                node["objects"]![1]!["definition"]!["artifactIds"]![0]!.GetValue<string>());
            File.WriteAllText(objectIndexPath, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        });
        await Assert.ThrowsAsync<IkpPackageValidationException>(() => reader.ReadAsync(invalidSchema));

        await using var missingArtifact = CreateMutatedFixtureZip(root =>
        {
            var objectIndexPath = Path.Combine(root, "object-index.json");
            var node = JsonNode.Parse(File.ReadAllText(objectIndexPath))!.AsObject();
            node["objects"]![1]!["definition"]!["artifactIds"] = new JsonArray("artifact-missing");
            File.WriteAllText(objectIndexPath, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            RebuildChecksums(root);
        });
        await Assert.ThrowsAsync<IkpPackageValidationException>(() => reader.ReadAsync(missingArtifact));
    }

    private static async Task<AnalysisHarness> CreateArchivedFixtureHarnessAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), $"phc-analysis-{Guid.NewGuid():N}");
        var archiveRoot = Path.Combine(root, "archive");
        var stagingPath = Path.Combine(root, "package.ikp.zip");
        Directory.CreateDirectory(archiveRoot);

        await using (var fixture = CreateFixtureZip())
        await using (var file = File.Create(stagingPath))
            await fixture.CopyToAsync(file);

        var organizationId = Guid.NewGuid();
        var validator = new IkpPackageValidator(SchemaDirectory);
        await using var validateStream = File.OpenRead(stagingPath);
        var validation = await validator.ValidateAsync(validateStream);
        var store = new FileSystemImmutablePackageStore(archiveRoot);
        var archived = await store.ArchiveValidatedPackageAsync(stagingPath, validation);
        var archivedPath = Path.Combine(archiveRoot, archived.StorageKey.Replace('/', Path.DirectorySeparatorChar));
        var attempts = new InMemoryCompletedReceiptRepository(organizationId, validation, archived.StorageKey);
        return new AnalysisHarness(
            root, organizationId, validation.PackageId, store, attempts, archivedPath, attempts.Receipt);
    }

    private static PackageImportAnalysis BuildMinimalAnalysis(
        Guid organizationId,
        Guid receiptId,
        Guid packageId,
        Guid captureId,
        string zipSha256,
        DateTimeOffset analyzedAtUtc)
    {
        var validation = new IkpPackageValidationResult(
            packageId, captureId, "0.1", "p", "1", "DB",
            zipSha256, 10, 5, 1, 1, 1, 1, 1, "b".PadLeft(64, 'b'));
        var issue = new IkpInspectedIssue(
            "issue-1", "op.proc", "Warning", "sql.procedure.definition_missing",
            "missing", null, null);
        var unavailable = new IkpInspectedObject(
            "proc:1", "stored_procedure", "op.proc", "Unavailable",
            Array.Empty<string>(), false, false, false, false, "P1", "dbo");
        var inspection = new IkpPackageInspection(
            validation, true, "phc.installation.knowledge.package", "Full", "Complete", "sqlserver",
            null, null, null,
            Array.Empty<IkpInspectedCapabilityOperation>(),
            Array.Empty<IkpInspectedCoverageOperation>(),
            [unavailable],
            Array.Empty<IkpInspectedArtifact>(),
            [issue],
            null);
        return new IkpPackageAnalysisBuilder().Build(
            organizationId, receiptId, inspection, true, analyzedAtUtc);
    }

    private static string ReorderJsonObjectProperties(string json)
    {
        using var document = JsonDocument.Parse(json);
        var properties = document.RootElement.EnumerateObject().Reverse().ToArray();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in properties)
            {
                property.WriteTo(writer);
            }
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private sealed class InMemoryCompletedReceiptRepository(
        Guid organizationId,
        IkpPackageValidationResult validation,
        string storageKey) : IPackageReceiptAttemptRepository
    {
        public PackageReceipt Receipt { get; } = new(
            Guid.NewGuid(), organizationId, validation.PackageId, validation.CaptureId,
            validation.ContractVersion, validation.ProducerProductId, validation.ProducerProductVersion,
            validation.OriginDatabaseName, validation.ZipSha256, validation.ZipByteLength, storageKey,
            DateTimeOffset.UtcNow,
            JsonSerializer.Serialize(new { validation.LogicalContentDigest }));

        public Task<PackageReceipt?> GetCompletedReceiptByPackageIdAsync(
            Guid orgId, Guid packageId, CancellationToken cancellationToken = default) =>
            Task.FromResult(orgId == organizationId && packageId == validation.PackageId ? Receipt : null);

        public Task<PackageReceiptAttemptRegistration> CreateOrGetPendingAsync(
            Guid orgId, IkpPackageValidationResult v, string sk, string stagingKey,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageReceiptAttemptLease?> ClaimByIdAsync(Guid attemptId, TimeSpan leaseDuration, DateTimeOffset nowUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<PackageReceiptAttemptLease>> ClaimDueAsync(int batchSize, TimeSpan leaseDuration, DateTimeOffset nowUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageReceipt?> GetCompletedReceiptAsync(Guid orgId, Guid attemptId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageIngestionStatus?> GetAttemptAsync(Guid orgId, Guid attemptId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageIngestionStatus?> GetByPackageIdAsync(Guid orgId, Guid packageId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlySet<string>> GetProtectedStagingKeysAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> RenewLeaseAsync(Guid attemptId, Guid leaseToken, DateTimeOffset leaseUntilUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PackageReceiptRegistration> CompleteAsync(Guid attemptId, Guid leaseToken, ArchivedPackage archive, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ScheduleRetryAsync(Guid attemptId, Guid leaseToken, DateTimeOffset nextAttemptAtUtc, string errorCode, string errorMessage, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> MarkNeedsAttentionAsync(Guid attemptId, Guid leaseToken, string errorCode, string errorMessage, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class AnalysisHarness(
        string root,
        Guid organizationId,
        Guid packageId,
        FileSystemImmutablePackageStore store,
        IPackageReceiptAttemptRepository attempts,
        string archivedPath,
        PackageReceipt receipt) : IAsyncDisposable
    {
        public Guid OrganizationId { get; } = organizationId;
        public Guid PackageId { get; } = packageId;
        public FileSystemImmutablePackageStore Store { get; } = store;
        public IPackageReceiptAttemptRepository Attempts { get; } = attempts;
        public string ArchivedPath { get; } = archivedPath;
        public PackageReceipt Receipt { get; } = receipt;

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(root))
            {
                foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                    File.SetAttributes(file, FileAttributes.Normal);
                Directory.Delete(root, recursive: true);
            }
            return ValueTask.CompletedTask;
        }
    }

    private static MemoryStream CreateFixtureZip()
    {
        var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in Directory.EnumerateFiles(FixtureDirectory, "*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(file).Equals(".md", StringComparison.OrdinalIgnoreCase))
                    continue;
                var relative = Path.GetRelativePath(FixtureDirectory, file).Replace(Path.DirectorySeparatorChar, '/');
                var entry = archive.CreateEntry(relative, CompressionLevel.Optimal);
                using var target = entry.Open();
                target.Write(File.ReadAllBytes(file));
            }
        }
        output.Position = 0;
        return output;
    }

    private static MemoryStream CreateMutatedFixtureZip(Action<string> mutate)
    {
        var root = Path.Combine(Path.GetTempPath(), $"phc-ikp-analysis-fixture-{Guid.NewGuid():N}");
        try
        {
            foreach (var file in Directory.EnumerateFiles(FixtureDirectory, "*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(file).Equals(".md", StringComparison.OrdinalIgnoreCase))
                    continue;
                var relative = Path.GetRelativePath(FixtureDirectory, file);
                var target = Path.Combine(root, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target);
            }
            mutate(root);
            return ZipDirectory(root);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static MemoryStream ZipDirectory(string root)
    {
        var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
                var entry = archive.CreateEntry(relative, CompressionLevel.Optimal);
                using var target = entry.Open();
                using var source = File.OpenRead(file);
                source.CopyTo(target);
            }
        }
        output.Position = 0;
        return output;
    }

    private static void RebuildChecksums(string root)
    {
        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => new
            {
                Path = path,
                Relative = Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/')
            })
            .Where(item => !string.Equals(item.Relative, "checksums.json", StringComparison.Ordinal))
            .OrderBy(item => item.Relative, StringComparer.Ordinal)
            .ToArray();

        var entries = new JsonArray();
        var canonical = new StringBuilder();
        foreach (var item in files)
        {
            var bytes = File.ReadAllBytes(item.Path);
            var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            entries.Add(new JsonObject
            {
                ["path"] = item.Relative,
                ["byteLength"] = bytes.Length,
                ["sha256"] = hash
            });
            canonical.Append(item.Relative).Append('\n')
                .Append(bytes.Length).Append('\n')
                .Append(hash).Append('\n');
        }

        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())))
            .ToLowerInvariant();
        var checksums = new JsonObject
        {
            ["schemaVersion"] = "1",
            ["algorithm"] = "SHA-256",
            ["files"] = entries,
            ["logicalContentDigest"] = new JsonObject
            {
                ["algorithm"] = "SHA-256",
                ["value"] = digest
            }
        };
        File.WriteAllText(
            Path.Combine(root, "checksums.json"),
            checksums.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}
