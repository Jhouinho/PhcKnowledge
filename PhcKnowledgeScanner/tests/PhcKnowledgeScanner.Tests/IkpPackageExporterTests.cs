using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PhcKnowledgeScanner.Abstractions.Collectors;
using PhcKnowledgeScanner.Abstractions.Coverage;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Inventory.Expression;
using PhcKnowledgeScanner.Abstractions.Manifest;
using PhcKnowledgeScanner.Export.Ikp;
using PhcKnowledgeScanner.Workspace;

namespace PhcKnowledgeScanner.Tests;

public sealed class IkpPackageExporterTests
{
    [Fact]
    public async Task ExportAsync_Writes_Contract_Files_And_Only_Referenced_Artifacts()
    {
        var root = Path.Combine(Path.GetTempPath(), "phc-ikp-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            await using var workspace = await SqliteScanWorkspace.CreateAsync(root, CancellationToken.None);
            var captureId = Guid.NewGuid().ToString("D");
            await workspace.InitializeAsync(Guid.NewGuid().ToString("N"), "server/demo", CancellationToken.None);

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
                    Array.Empty<string>()),
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

            await workspace.SaveCaptureCoverageAsync(
                captureId, capabilities, coverages, CancellationToken.None);

            var sourceBytes = Encoding.UTF8.GetBytes("FUNCTION BODY\r\n");
            var referencedArtifactId = await workspace.SaveArtifactAsync(
                "phc.customizations",
                "brule/S1.prg",
                sourceBytes,
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

            var declaredRelations = new[]
            {
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
            };
            await workspace.UpsertDeclaredRelationsAsync(declaredRelations, CancellationToken.None);
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

            var started = DateTimeOffset.UtcNow.AddMinutes(-1);
            var finished = DateTimeOffset.UtcNow;
            var scanManifest = new ScanManifest(
                captureId,
                "server/demo",
                started,
                finished,
                ScanMode.Full,
                "test-version",
                capabilities,
                coverages,
                Array.Empty<CollectorResult>());

            var outputPath = Path.Combine(root, "package.ikp.zip");
            var result = await new IkpPackageExporter().ExportAsync(
                workspace,
                scanManifest,
                new IkpOriginDescriptor("DEMO"),
                outputPath,
                CancellationToken.None);

            Assert.True(File.Exists(outputPath));
            Assert.Equal(captureId, result.CaptureId);
            Assert.NotEqual(Guid.Empty, Guid.Parse(result.PackageId));
            Assert.Equal(Sha256(await File.ReadAllBytesAsync(outputPath)), result.ZipSha256);

            using var archive = ZipFile.OpenRead(outputPath);
            var names = archive.Entries.Select(x => x.FullName).ToHashSet(StringComparer.Ordinal);
            Assert.Contains("manifest.json", names);
            Assert.Contains("capabilities.json", names);
            Assert.Contains("coverage.json", names);
            Assert.Contains("object-index.json", names);
            Assert.Contains("checksums.json", names);
            Assert.Contains("payload/phc.customizations/brule/S1.prg", names);
            Assert.Contains("payload/diagnostics/collection-issues.json", names);
            Assert.Contains("payload/relations/declared-relations.json", names);
            Assert.Contains("payload/relations/declared-relation-interpretations.json", names);
            Assert.DoesNotContain("payload/phc.customizations/old/obsolete.prg", names);

            using var manifestStream = archive.GetEntry("manifest.json")!.Open();
            using var manifestJson = await JsonDocument.ParseAsync(manifestStream);
            var manifestRoot = manifestJson.RootElement;
            Assert.Equal("0.1", manifestRoot.GetProperty("ikpContractVersion").GetString());
            Assert.Equal(result.PackageId, manifestRoot.GetProperty("packageId").GetString());
            Assert.Equal(captureId, manifestRoot.GetProperty("capture").GetProperty("captureId").GetString());
            Assert.Equal("DEMO", manifestRoot.GetProperty("origin").GetProperty("databaseName").GetString());
            Assert.Contains(manifestRoot.GetProperty("artifacts").EnumerateArray(),
                x => x.GetProperty("artifactId").GetString() == referencedArtifactId);

            using var checksumsStream = archive.GetEntry("checksums.json")!.Open();
            using var checksumsJson = await JsonDocument.ParseAsync(checksumsStream);
            var checksumRoot = checksumsJson.RootElement;
            Assert.Equal("SHA-256", checksumRoot.GetProperty("algorithm").GetString());
            Assert.Equal(result.LogicalContentDigest,
                checksumRoot.GetProperty("logicalContentDigest").GetProperty("value").GetString());

            using var coverageStream = archive.GetEntry("coverage.json")!.Open();
            using var coverageJson = await JsonDocument.ParseAsync(coverageStream);
            var procedureCoverage = coverageJson.RootElement.GetProperty("operations")
                .EnumerateArray()
                .Single(x => x.GetProperty("operationId").GetString() == KnownOperations.SqlProcedures);
            Assert.Equal("Partial", procedureCoverage.GetProperty("contentAvailability").GetString());
            Assert.Equal("SucceededWithWarnings", procedureCoverage.GetProperty("result").GetString());

            using var declaredRelationsStream = archive.GetEntry("payload/relations/declared-relations.json")!.Open();
            using var declaredRelationsJson = await JsonDocument.ParseAsync(declaredRelationsStream);
            Assert.Equal(2, declaredRelationsJson.RootElement.GetProperty("records").GetArrayLength());
            Assert.Contains(declaredRelationsJson.RootElement.GetProperty("records").EnumerateArray(),
                x => x.GetProperty("sourceType").GetString() == DeclaredRelationSourceTypes.PhcRel);
            Assert.Contains(declaredRelationsJson.RootElement.GetProperty("records").EnumerateArray(),
                x => x.GetProperty("sourceType").GetString() == DeclaredRelationSourceTypes.PhcDtl);

            using var interpretationsStream = archive.GetEntry("payload/relations/declared-relation-interpretations.json")!.Open();
            using var interpretationsJson = await JsonDocument.ParseAsync(interpretationsStream);
            Assert.Single(interpretationsJson.RootElement.GetProperty("records").EnumerateArray());

            var checksumFiles = checksumRoot.GetProperty("files").EnumerateArray().ToArray();
            Assert.DoesNotContain(checksumFiles, x => x.GetProperty("path").GetString() == "checksums.json");
            foreach (var checksum in checksumFiles)
            {
                var entryPath = checksum.GetProperty("path").GetString()!;
                var entry = archive.GetEntry(entryPath);
                Assert.NotNull(entry);
                await using var entryStream = entry!.Open();
                using var buffer = new MemoryStream();
                await entryStream.CopyToAsync(buffer);
                var bytes = buffer.ToArray();
                Assert.Equal(checksum.GetProperty("byteLength").GetInt64(), bytes.LongLength);
                Assert.Equal(checksum.GetProperty("sha256").GetString(), Sha256(bytes));
            }

            using var objectIndexStream = archive.GetEntry("object-index.json")!.Open();
            using var objectIndexJson = await JsonDocument.ParseAsync(objectIndexStream);
            var projected = objectIndexJson.RootElement.GetProperty("objects")
                .EnumerateArray()
                .Single(x => x.GetProperty("definition").GetProperty("artifactIds").GetArrayLength() == 1);
            Assert.Equal("Available", projected.GetProperty("definition").GetProperty("status").GetString());
            Assert.Equal(referencedArtifactId,
                Assert.Single(projected.GetProperty("definition").GetProperty("artifactIds").EnumerateArray()).GetString());

            var historicalOutput = Path.Combine(root, "historical-package.ikp.zip");
            await new IkpPackageExporter().ExportAsync(
                workspace,
                new IkpCaptureDescriptor(captureId, null, null, null, null),
                new IkpOriginDescriptor("DEMO"),
                historicalOutput,
                CancellationToken.None);

            using var historicalArchive = ZipFile.OpenRead(historicalOutput);
            using var historicalManifestStream = historicalArchive.GetEntry("manifest.json")!.Open();
            using var historicalManifest = await JsonDocument.ParseAsync(historicalManifestStream);
            var historicalCapture = historicalManifest.RootElement.GetProperty("capture");
            Assert.Equal("Unknown", historicalCapture.GetProperty("scannerVersion").GetString());
            Assert.Equal("Unknown", historicalCapture.GetProperty("mode").GetString());
            Assert.Equal("Partial", historicalCapture.GetProperty("metadataStatus").GetString());
            Assert.Equal(4, historicalCapture.GetProperty("unknownFields").GetArrayLength());
            Assert.False(historicalCapture.TryGetProperty("startedAtUtc", out _));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static string Sha256(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
