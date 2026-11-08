using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PhcKnowledge.Import.Validation;

namespace PhcKnowledge.Import.Tests;

public sealed class IkpPackageValidatorTests
{
    private readonly IkpPackageValidator _validator = new();

    [Fact]
    public async Task Accepts_a_minimal_valid_package()
    {
        await using var package = CreatePackage();
        var result = await _validator.ValidateAsync(package);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.Equal(0, result.ArtifactCount);
        Assert.Equal(1, result.ObjectCount);
        Assert.Equal(1, result.OperationCount);
    }

    [Fact]
    public async Task Accepts_current_exporter_relation_and_issue_payload_shapes()
    {
        await using var package = CreatePackage(includeCurrentRelationPayload: true);
        var result = await _validator.ValidateAsync(package);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
        Assert.Equal(3, result.ArtifactCount);
    }

    [Fact]
    public async Task Rejects_an_orphaned_relation_interpretation()
    {
        await using var package = CreatePackage(
            includeCurrentRelationPayload: true,
            orphanRelationInterpretation: true);
        var result = await _validator.ValidateAsync(package);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("interpretação sem relação declarada", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Rejects_a_document_that_violates_the_published_json_schema()
    {
        await using var package = CreatePackage(invalidManifestSchema: true);
        var result = await _validator.ValidateAsync(package);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("manifest.schema.json", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Rejects_a_package_with_a_tampered_document()
    {
        await using var package = CreatePackage(tamperCoverageAfterChecksums: true);
        var result = await _validator.ValidateAsync(package);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("SHA-256 divergente", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Rejects_path_traversal_entries()
    {
        await using var package = CreatePackage(includeTraversalEntry: true);
        var result = await _validator.ValidateAsync(package);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("traversal", StringComparison.OrdinalIgnoreCase));
    }

    private static MemoryStream CreatePackage(
        bool tamperCoverageAfterChecksums = false,
        bool includeTraversalEntry = false,
        bool invalidManifestSchema = false,
        bool includeCurrentRelationPayload = false,
        bool orphanRelationInterpretation = false)
    {
        const string captureId = "a1f2fc33-7b0a-4c8f-b049-073a33ef7978";
        const string operationId = "sample.operation";
        const string relationId = "phc.rel:REL001";

        var payloadFiles = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        if (includeCurrentRelationPayload)
        {
            payloadFiles["payload/diagnostics/collection-issues.json"] =
                JsonSerializer.SerializeToUtf8Bytes(Array.Empty<object>());
            payloadFiles["payload/relations/declared-relations.json"] =
                JsonSerializer.SerializeToUtf8Bytes(new
                {
                    schemaVersion = "1",
                    captureId,
                    records = new[] { new { relationId } }
                });
            payloadFiles["payload/relations/declared-relation-interpretations.json"] =
                JsonSerializer.SerializeToUtf8Bytes(new
                {
                    schemaVersion = "1",
                    captureId,
                    records = new[] { new { relationId = orphanRelationInterpretation ? "phc.rel:ORPHAN" : relationId } }
                });
        }

        var artifacts = payloadFiles.Select((pair, index) => new
        {
            artifactId = $"artifact-{index + 1}",
            path = pair.Key,
            category = pair.Key.Contains("/relations/", StringComparison.Ordinal) ? "relations" : "diagnostics",
            format = "application/json",
            byteLength = pair.Value.LongLength,
            sha256 = Convert.ToHexStringLower(SHA256.HashData(pair.Value)),
            required = true,
            role = "test-fixture"
        }).ToArray();

        var documents = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["manifest.json"] = JsonSerializer.SerializeToUtf8Bytes(new
            {
                ikpContractVersion = "0.1",
                contentType = "phc.installation.knowledge.package",
                packageId = "5fc15128-b7ab-4a23-ad61-93757aedb0f7",
                producedAtUtc = DateTimeOffset.Parse("2026-11-08T15:00:00Z"),
                producer = new { productId = "PhcKnowledgeScanner", productVersion = "2.0.0" },
                capture = new
                {
                    captureId,
                    mode = invalidManifestSchema ? "Unsupported" : "Full",
                    scannerVersion = "2.0.0",
                    metadataStatus = "Complete"
                },
                origin = new { engine = "sqlserver" },
                artifacts
            }),
            ["capabilities.json"] = JsonSerializer.SerializeToUtf8Bytes(new
            {
                schemaVersion = "1",
                scannerVersion = "2.0.0",
                operations = new[]
                {
                    new
                    {
                        categoryId = "sample",
                        operationId,
                        capabilityVersion = "1.0",
                        collectorId = "sample",
                        supported = true
                    }
                }
            }),
            ["coverage.json"] = JsonSerializer.SerializeToUtf8Bytes(new
            {
                schemaVersion = "1",
                captureId,
                operations = new[]
                {
                    new
                    {
                        operationId,
                        capabilityVersion = "1.0",
                        requested = true,
                        result = "Succeeded",
                        sourceAvailability = "Present",
                        contentAvailability = "NotCollected",
                        observedCount = 1,
                        issueIds = Array.Empty<string>()
                    }
                }
            }),
            ["object-index.json"] = JsonSerializer.SerializeToUtf8Bytes(new
            {
                schemaVersion = "1",
                captureId,
                objects = new[]
                {
                    new
                    {
                        sourceObjectId = "sample:1",
                        objectKind = "sample",
                        metadata = new { },
                        definition = new { status = "NotCollected", artifactIds = Array.Empty<string>() },
                        operationId
                    }
                }
            })
        };

        foreach (var (path, bytes) in payloadFiles)
            documents.Add(path, bytes);

        var checksumEntries = documents.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new
            {
                path = pair.Key,
                byteLength = pair.Value.LongLength,
                sha256 = Convert.ToHexStringLower(SHA256.HashData(pair.Value))
            })
            .ToArray();
        var canonical = new StringBuilder();
        foreach (var item in checksumEntries)
            canonical.Append(item.path).Append('\n').Append(item.byteLength).Append('\n').Append(item.sha256).Append('\n');
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
        documents["checksums.json"] = JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "1",
            algorithm = "SHA-256",
            files = checksumEntries,
            logicalContentDigest = new { algorithm = "SHA-256", value = digest }
        });

        if (tamperCoverageAfterChecksums)
            documents["coverage.json"] = Encoding.UTF8.GetBytes("""{"tampered":true}""");

        var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, bytes) in documents)
            {
                var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
                using var stream = entry.Open();
                stream.Write(bytes);
            }

            if (includeTraversalEntry)
            {
                var entry = archive.CreateEntry("payload/../../outside.txt");
                using var stream = entry.Open();
                stream.Write("unsafe"u8);
            }
        }
        output.Position = 0;
        return output;
    }
}
