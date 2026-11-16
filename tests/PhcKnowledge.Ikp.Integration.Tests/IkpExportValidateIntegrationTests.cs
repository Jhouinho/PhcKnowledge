using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PhcKnowledge.Import.Ikp;
using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Coverage;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Inventory.Expression;
using PhcKnowledgeScanner.Abstractions.Manifest;
using PhcKnowledgeScanner.Export.Ikp;
using PhcKnowledgeScanner.Workspace;

namespace PhcKnowledge.Ikp.Integration.Tests;

public sealed class IkpExportValidateIntegrationTests
{
    private static string SchemaDirectory => Path.Combine(AppContext.BaseDirectory, "Schemas");

    [Fact]
    public async Task P1_ExportAsync_Produces_Zip_Accepted_By_IkpPackageValidator()
    {
        await using var harness = await SyntheticIkpWorkspace.CreateAsync();
        var zipPath = Path.Combine(harness.Root, "p1-package.ikp.zip");

        var export = await new IkpPackageExporter().ExportAsync(
            harness.Workspace,
            harness.CreateCompleteManifest(),
            new IkpOriginDescriptor("DEMO_IKP"),
            zipPath);

        await using var packageStream = File.OpenRead(zipPath);
        var validation = await new IkpPackageValidator(SchemaDirectory).ValidateAsync(packageStream);

        Assert.Equal(Guid.Parse(export.PackageId), validation.PackageId);
        Assert.Equal(Guid.Parse(export.CaptureId), validation.CaptureId);
        Assert.Equal(Guid.Parse(harness.CaptureId), validation.CaptureId);
        Assert.Equal(5, validation.ContractFileCount);
        Assert.Equal(export.ZipSha256, validation.ZipSha256);
        Assert.Equal(export.LogicalContentDigest, validation.LogicalContentDigest);
        Assert.Equal(export.PayloadArtifactCount, validation.ArtifactCount);
        Assert.Equal(2, validation.ObjectCount);
        Assert.Equal(4, validation.OperationCount);
        Assert.Equal(1, validation.IssueCount);
        Assert.Equal("DEMO_IKP", validation.OriginDatabaseName);
        Assert.Equal(Sha256(await File.ReadAllBytesAsync(zipPath)), validation.ZipSha256);

        using var archive = ZipFile.OpenRead(zipPath);
        var names = archive.Entries.Select(x => x.FullName).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("manifest.json", names);
        Assert.Contains("capabilities.json", names);
        Assert.Contains("coverage.json", names);
        Assert.Contains("object-index.json", names);
        Assert.Contains("checksums.json", names);
        Assert.Contains("payload/diagnostics/issues.json", names);
        Assert.Contains("payload/relations/declared-relations.json", names);
        Assert.Contains($"payload/phc.customizations/brule/{harness.ReferencedRelativePath}", names);

        await using var relationsStream = archive.GetEntry("payload/relations/declared-relations.json")!.Open();
        using var relationsJson = await JsonDocument.ParseAsync(relationsStream);
        Assert.Equal(harness.CaptureId, relationsJson.RootElement.GetProperty("captureId").GetString());
        Assert.Equal(2, relationsJson.RootElement.GetProperty("declaredRelations").GetArrayLength());
        Assert.Single(relationsJson.RootElement.GetProperty("interpretations").EnumerateArray());
        Assert.Contains(
            relationsJson.RootElement.GetProperty("declaredRelations").EnumerateArray(),
            x => x.GetProperty("sourceType").GetString() == DeclaredRelationSourceTypes.PhcRel);
        Assert.Contains(
            relationsJson.RootElement.GetProperty("declaredRelations").EnumerateArray(),
            x => x.GetProperty("sourceType").GetString() == DeclaredRelationSourceTypes.PhcDtl);
    }

    [Fact]
    public async Task P2_PartialUnknown_Metadata_Is_Accepted_By_Exporter_And_Validator()
    {
        await using var harness = await SyntheticIkpWorkspace.CreateAsync();
        var zipPath = Path.Combine(harness.Root, "p2-historical.ikp.zip");

        var export = await new IkpPackageExporter().ExportAsync(
            harness.Workspace,
            new IkpCaptureDescriptor(harness.CaptureId, null, null, null, null),
            new IkpOriginDescriptor("DEMO_IKP"),
            zipPath);

        using (var archive = ZipFile.OpenRead(zipPath))
        await using (var manifestStream = archive.GetEntry("manifest.json")!.Open())
        using (var manifest = await JsonDocument.ParseAsync(manifestStream))
        {
            var capture = manifest.RootElement.GetProperty("capture");
            Assert.Equal("Unknown", capture.GetProperty("scannerVersion").GetString());
            Assert.Equal("Unknown", capture.GetProperty("mode").GetString());
            Assert.Equal("Partial", capture.GetProperty("metadataStatus").GetString());
            Assert.Equal(4, capture.GetProperty("unknownFields").GetArrayLength());
            Assert.False(capture.TryGetProperty("startedAtUtc", out _));
            Assert.False(capture.TryGetProperty("completedAtUtc", out _));
        }

        await using var packageStream = File.OpenRead(zipPath);
        var validation = await new IkpPackageValidator(SchemaDirectory).ValidateAsync(packageStream);

        Assert.Equal(Guid.Parse(export.PackageId), validation.PackageId);
        Assert.Equal(Guid.Parse(harness.CaptureId), validation.CaptureId);
        Assert.Equal(5, validation.ContractFileCount);
        Assert.Equal(export.ZipSha256, validation.ZipSha256);
        Assert.Equal(export.LogicalContentDigest, validation.LogicalContentDigest);
        Assert.True(validation.ObjectCount > 0);
    }

    [Fact]
    public async Task N1_Tampered_Payload_Is_Rejected_By_IkpPackageValidator()
    {
        await using var harness = await SyntheticIkpWorkspace.CreateAsync();
        var zipPath = Path.Combine(harness.Root, "n1-valid.ikp.zip");

        await new IkpPackageExporter().ExportAsync(
            harness.Workspace,
            harness.CreateCompleteManifest(),
            new IkpOriginDescriptor("DEMO_IKP"),
            zipPath);

        TamperZipEntry(
            zipPath,
            $"payload/phc.customizations/brule/{harness.ReferencedRelativePath}");

        await using var packageStream = File.OpenRead(zipPath);
        var ex = await Assert.ThrowsAsync<IkpPackageValidationException>(
            () => new IkpPackageValidator(SchemaDirectory).ValidateAsync(packageStream));

        Assert.Contains("SHA-256 divergente", ex.Message, StringComparison.Ordinal);
    }

    private static void TamperZipEntry(string zipPath, string entryName)
    {
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Update);
        var entry = archive.GetEntry(entryName)
            ?? throw new InvalidOperationException($"Entrada ZIP em falta para adulteração: {entryName}");
        byte[] original;
        using (var input = entry.Open())
        using (var buffer = new MemoryStream())
        {
            input.CopyTo(buffer);
            original = buffer.ToArray();
        }

        entry.Delete();
        var replacement = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var output = replacement.Open();
        var tampered = (byte[])original.Clone();
        tampered[0] ^= 0x01;
        output.Write(tampered);
    }

    private static string Sha256(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private sealed class SyntheticIkpWorkspace : IAsyncDisposable
    {
        private SyntheticIkpWorkspace(
            string root,
            SqliteScanWorkspace workspace,
            string captureId,
            DeclaredCapability[] capabilities,
            OperationCoverage[] coverages,
            string referencedRelativePath)
        {
            Root = root;
            Workspace = workspace;
            CaptureId = captureId;
            Capabilities = capabilities;
            Coverages = coverages;
            ReferencedRelativePath = referencedRelativePath;
        }

        public string Root { get; }
        public SqliteScanWorkspace Workspace { get; }
        public string CaptureId { get; }
        public DeclaredCapability[] Capabilities { get; }
        public OperationCoverage[] Coverages { get; }
        public string ReferencedRelativePath { get; }

        public static async Task<SyntheticIkpWorkspace> CreateAsync()
        {
            var root = Path.Combine(Path.GetTempPath(), "phc-ikp-integration", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            var workspace = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None);
            var captureId = Guid.NewGuid().ToString("D");
            await workspace.InitializeAsync(Guid.NewGuid().ToString("N"), "server/demo-ikp", CancellationToken.None);

            var capabilities = new[]
            {
                new DeclaredCapability(
                    "phc.customizations.brule",
                    KnownOperations.PhcBrule,
                    KnownOperations.Version,
                    "phc.customizations",
                    true),
                new DeclaredCapability(
                    "sql.server.structure.procedures",
                    KnownOperations.SqlProcedures,
                    KnownOperations.Version,
                    "sql.server.structure",
                    true),
                new DeclaredCapability(
                    "phc.declared-relations.rel",
                    KnownOperations.PhcRel,
                    KnownOperations.Version,
                    "phc.declared-relations",
                    true),
                new DeclaredCapability(
                    "phc.declared-relations.dtl",
                    KnownOperations.PhcDtl,
                    KnownOperations.Version,
                    "phc.declared-relations",
                    true)
            };

            var coverages = new[]
            {
                new OperationCoverage(
                    KnownOperations.PhcBrule,
                    KnownOperations.Version,
                    true,
                    OperationExecutionResult.Succeeded,
                    SourceAvailability.Present,
                    ContentAvailability.Available,
                    1,
                    new[] { "issue-brule-warning" }),
                new OperationCoverage(
                    KnownOperations.SqlProcedures,
                    KnownOperations.Version,
                    true,
                    OperationExecutionResult.Succeeded,
                    SourceAvailability.Present,
                    ContentAvailability.Available,
                    1,
                    Array.Empty<string>()),
                new OperationCoverage(
                    KnownOperations.PhcRel,
                    KnownOperations.Version,
                    true,
                    OperationExecutionResult.Succeeded,
                    SourceAvailability.Present,
                    ContentAvailability.Available,
                    1,
                    Array.Empty<string>()),
                new OperationCoverage(
                    KnownOperations.PhcDtl,
                    KnownOperations.Version,
                    true,
                    OperationExecutionResult.Succeeded,
                    SourceAvailability.Present,
                    ContentAvailability.Available,
                    1,
                    Array.Empty<string>())
            };

            await workspace.SaveCaptureCoverageAsync(captureId, capabilities, coverages, CancellationToken.None);

            await workspace.AddIssueAsync(
                "integration-collector",
                new CollectionIssue(
                    CollectionIssueSeverity.Warning,
                    "test.warning",
                    "Aviso de integração",
                    "Regra de teste",
                    "Detalhe de teste",
                    "issue-brule-warning",
                    KnownOperations.PhcBrule),
                CancellationToken.None);

            const string referencedRelativePath = "S1.prg";
            var referencedArtifactId = await workspace.SaveArtifactAsync(
                "phc.customizations",
                $"brule/{referencedRelativePath}",
                Encoding.UTF8.GetBytes("FUNCTION BODY\r\n"),
                "text/plain",
                null,
                CancellationToken.None);
            await workspace.SaveArtifactAsync(
                "phc.customizations",
                "old/obsolete.prg",
                Encoding.UTF8.GetBytes("STALE"),
                "text/plain",
                null,
                CancellationToken.None);

            await workspace.UpsertObjectsAsync(
            [
                new TechnicalObjectRecord(
                    "phc.brule:S1",
                    ObjectKind.PhcUserRule,
                    "Regra de teste",
                    "dbo",
                    null,
                    "phc.customizations",
                    referencedArtifactId,
                    """{"source":"brule","stamp":"S1","tabela":"FT","inactivo":"0"}""",
                    DateTimeOffset.UtcNow),
                new TechnicalObjectRecord(
                    "sql.procedure:dbo.usp_missing_definition",
                    ObjectKind.StoredProcedure,
                    "usp_missing_definition",
                    "dbo",
                    null,
                    "sql.server.structure",
                    null,
                    null,
                    DateTimeOffset.UtcNow)
            ],
            CancellationToken.None);

            await workspace.UpsertDeclaredRelationsAsync(
            [
                new DeclaredRelationRecord(
                    "rel:1",
                    DeclaredRelationSourceTypes.PhcRel,
                    "1",
                    "FT",
                    "Faturação",
                    "CL",
                    "Clientes",
                    "FT.CL=CL.NO",
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    DateTimeOffset.UtcNow,
                    null,
                    null,
                    TableReferenceResolution.Unresolved,
                    TableReferenceResolution.Unresolved,
                    DeclaredRelationCorrespondenceKind.None,
                    null),
                new DeclaredRelationRecord(
                    "dtl:1",
                    DeclaredRelationSourceTypes.PhcDtl,
                    "1",
                    "FT",
                    "Faturação",
                    "FI",
                    "Linhas de faturação",
                    "FT.FI=FI.FT",
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    DateTimeOffset.UtcNow,
                    null,
                    null,
                    TableReferenceResolution.Unresolved,
                    TableReferenceResolution.Unresolved,
                    DeclaredRelationCorrespondenceKind.None,
                    null)
            ],
            CancellationToken.None);

            await workspace.UpsertDeclaredRelationInterpretationsAsync(
            [
                new DeclaredRelationInterpretationRecord(
                    "rel:1",
                    "test-parser-1",
                    ExpressionParseStatus.Unsupported,
                    InterpretationConfidence.None,
                    null,
                    null,
                    0,
                    0,
                    0,
                    0,
                    DateTimeOffset.UtcNow)
            ],
            CancellationToken.None);

            return new SyntheticIkpWorkspace(
                root, workspace, captureId, capabilities, coverages, referencedRelativePath);
        }

        public ScanManifest CreateCompleteManifest()
        {
            var started = DateTimeOffset.UtcNow.AddMinutes(-1);
            var finished = DateTimeOffset.UtcNow;
            return new ScanManifest(
                CaptureId,
                "server/demo-ikp",
                started,
                finished,
                ScanMode.Full,
                "test-version",
                Capabilities,
                Coverages,
                Array.Empty<CollectorResult>());
        }

        public async ValueTask DisposeAsync()
        {
            await Workspace.DisposeAsync();
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
    }
}
