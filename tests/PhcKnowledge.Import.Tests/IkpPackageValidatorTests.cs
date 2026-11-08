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
        Assert.Equal(0, result.ObjectCount);
        Assert.Equal(0, result.OperationCount);
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
        bool includeTraversalEntry = false)
    {
        var captureId = "a1f2fc33-7b0a-4c8f-b049-073a33ef7978";
        var documents = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["manifest.json"] = Encoding.UTF8.GetBytes($$"""
                {
                  "ikpContractVersion": "0.1",
                  "contentType": "phc.installation.knowledge.package",
                  "packageId": "5fc15128-b7ab-4a23-ad61-93757aedb0f7",
                  "producedAtUtc": "2026-11-08T15:00:00Z",
                  "producer": { "productId": "PhcKnowledgeScanner", "productVersion": "2.0.0" },
                  "capture": {
                    "captureId": "{{captureId}}",
                    "mode": "Full",
                    "scannerVersion": "2.0.0",
                    "metadataStatus": "Complete"
                  },
                  "origin": { "engine": "sqlserver" },
                  "artifacts": []
                }
                """),
            ["capabilities.json"] = Encoding.UTF8.GetBytes("""{"schemaVersion":"1","scannerVersion":"2.0.0","operations":[]}"""),
            ["coverage.json"] = Encoding.UTF8.GetBytes($$"""{"schemaVersion":"1","captureId":"{{captureId}}","operations":[]}"""),
            ["object-index.json"] = Encoding.UTF8.GetBytes($$"""{"schemaVersion":"1","captureId":"{{captureId}}","objects":[]}""")
        };

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
            documents["coverage.json"] = Encoding.UTF8.GetBytes($$"""{"schemaVersion":"1","captureId":"{{captureId}}","operations":[ ]} """);

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
