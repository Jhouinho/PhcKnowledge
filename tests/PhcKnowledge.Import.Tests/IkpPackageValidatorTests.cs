using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Receipts;

namespace PhcKnowledge.Import.Tests;

public sealed class IkpPackageValidatorTests
{
    private static string FixtureDirectory => Path.Combine(AppContext.BaseDirectory, "Fixtures", "minimal-valid");
    private static string SchemaDirectory => Path.Combine(AppContext.BaseDirectory, "Schemas");
    private static string ObjectIndexSchemaPath => Path.Combine(SchemaDirectory, "object-index.schema.json");

    [Fact]
    public async Task Valid_minimal_fixture_is_accepted()
    {
        await using var package = CreateFixtureZip();
        var validator = new IkpPackageValidator(SchemaDirectory);

        var result = await validator.ValidateAsync(package);

        Assert.Equal(Guid.Parse("9d5e4f70-4a1d-4c9d-9a1b-1b7f9d4c0101"), result.PackageId);
        Assert.Equal(Guid.Parse("b1b2c3d4-1111-4222-8333-123456789abc"), result.CaptureId);
        Assert.Equal("0.1", result.ContractVersion);
        Assert.Equal(4, result.ArtifactCount);
        Assert.True(result.ObjectCount > 0);
        Assert.Equal(5, result.ContractFileCount);
        Assert.Equal(64, result.ZipSha256.Length);
    }

    [Fact]
    public async Task Valid_object_index_continues_to_be_accepted_after_objects_uniqueItems_removal()
    {
        await using var package = CreateFixtureZip();
        var validator = new IkpPackageValidator(SchemaDirectory);

        var result = await validator.ValidateAsync(package);

        Assert.Equal(3, result.ObjectCount);
        Assert.True(File.Exists(ObjectIndexSchemaPath));
        var schemaText = await File.ReadAllTextAsync(ObjectIndexSchemaPath);
        using var schemaDoc = JsonDocument.Parse(schemaText);
        Assert.False(
            schemaDoc.RootElement.GetProperty("properties").GetProperty("objects").TryGetProperty("uniqueItems", out _),
            "objects must not declare uniqueItems after the performance fix.");
        Assert.True(
            schemaDoc.RootElement
                .GetProperty("properties").GetProperty("objects")
                .GetProperty("items").GetProperty("properties")
                .GetProperty("definition").GetProperty("properties")
                .GetProperty("artifactIds").GetProperty("uniqueItems").GetBoolean());
    }

    [Fact]
    public async Task Duplicate_sourceObjectId_is_rejected_by_validator()
    {
        await using var package = CreateFixtureZip(mutateObjectIndex: index =>
        {
            var objects = index["objects"]!.AsArray();
            var duplicate = JsonNode.Parse(objects[0]!.ToJsonString())!.AsObject();
            duplicate["name"] = "Duplicated FT copy";
            objects.Add(duplicate);
        });
        var validator = new IkpPackageValidator(SchemaDirectory);

        var ex = await Assert.ThrowsAsync<IkpPackageValidationException>(
            () => validator.ValidateAsync(package));

        Assert.Contains("sourceObjectId duplicado", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Duplicate_artifactIds_are_rejected_by_schema_uniqueItems()
    {
        var document = JsonNode.Parse(File.ReadAllText(Path.Combine(FixtureDirectory, "object-index.json")))!;
        document["objects"]![0]!["definition"]!["artifactIds"] = new JsonArray("artifact-schema-ft", "artifact-schema-ft");

        var schema = JsonSchema.FromText(File.ReadAllText(ObjectIndexSchemaPath));
        var result = schema.Evaluate(JsonDocument.Parse(document.ToJsonString()).RootElement);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Valid_package_is_archived_immutably_and_duplicate_is_idempotent()
    {
        var root = Path.Combine(Path.GetTempPath(), $"phc-knowledge-import-{Guid.NewGuid():N}");
        var archiveRoot = Path.Combine(root, "archive");
        var stagingRoot = Path.Combine(root, "staging");
        Directory.CreateDirectory(root);
        try
        {
            var validator = new IkpPackageValidator(SchemaDirectory);
            var store = new PhcKnowledge.Import.Storage.FileSystemImmutablePackageStore(archiveRoot);
            var ingestion = new IkpPackageIngestionService(validator, store, stagingRoot);

            await using var firstPackage = CreateFixtureZip();
            var first = await ingestion.ReceiveValidateArchiveAsync(firstPackage);
            Assert.False(first.Archive.AlreadyExisted);

            var archivedPath = Path.Combine(archiveRoot, first.Archive.StorageKey.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(archivedPath));
            Assert.True((File.GetAttributes(archivedPath) & FileAttributes.ReadOnly) != 0);

            await using var secondPackage = CreateFixtureZip();
            var second = await ingestion.ReceiveValidateArchiveAsync(secondPackage);
            Assert.True(second.Archive.AlreadyExisted);
            Assert.Equal(first.Archive.StorageKey, second.Archive.StorageKey);
            Assert.Empty(Directory.EnumerateFiles(stagingRoot));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                    File.SetAttributes(file, FileAttributes.Normal);
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Valid_package_registers_receipt_for_the_authenticated_organization()
    {
        var root = Path.Combine(Path.GetTempPath(), $"phc-knowledge-receipt-{Guid.NewGuid():N}");
        var organizationId = Guid.NewGuid();
        var repository = new RecordingReceiptRepository();
        try
        {
            var validator = new IkpPackageValidator(SchemaDirectory);
            var store = new PhcKnowledge.Import.Storage.FileSystemImmutablePackageStore(Path.Combine(root, "archive"));
            var ingestion = new IkpPackageIngestionService(
                validator, store, Path.Combine(root, "staging"), receiptRepository: repository);

            await using var package = CreateFixtureZip();
            var result = await ingestion.ReceiveValidateArchiveAsync(package, organizationId);

            Assert.NotNull(result.Receipt);
            Assert.Equal(organizationId, result.Receipt.OrganizationId);
            Assert.Equal(result.Validation.PackageId, result.Receipt.PackageId);
            Assert.Equal(result.Archive.StorageKey, result.Receipt.StorageKey);
            Assert.Equal(organizationId, repository.OrganizationId);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                    File.SetAttributes(file, FileAttributes.Normal);
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Zip_path_traversal_is_rejected()
    {
        await using var package = CreateFixtureZip(includeTraversalEntry: true);
        var validator = new IkpPackageValidator(SchemaDirectory);

        await Assert.ThrowsAsync<IkpPackageValidationException>(
            () => validator.ValidateAsync(package));
    }

    [Fact]
    public async Task Tampered_payload_is_rejected_by_checksums()
    {
        await using var package = CreateFixtureZip(tamperPayload: true);
        var validator = new IkpPackageValidator(SchemaDirectory);

        await Assert.ThrowsAsync<IkpPackageValidationException>(
            () => validator.ValidateAsync(package));
    }

    private sealed class RecordingReceiptRepository : IPackageReceiptRepository
    {
        public Guid? OrganizationId { get; private set; }

        public Task<PackageReceiptRegistration> RegisterArchivedPackageAsync(
            Guid organizationId,
            IkpPackageValidationResult validation,
            PhcKnowledge.Import.Storage.ArchivedPackage archive,
            CancellationToken cancellationToken = default)
        {
            OrganizationId = organizationId;
            var receipt = new PackageReceipt(
                Guid.NewGuid(), organizationId, validation.PackageId, validation.CaptureId,
                validation.ContractVersion, validation.ProducerProductId, validation.ProducerProductVersion,
                validation.OriginDatabaseName, validation.ZipSha256, validation.ZipByteLength,
                archive.StorageKey, DateTimeOffset.UtcNow, "{}");
            return Task.FromResult(new PackageReceiptRegistration(receipt, false));
        }
    }

    private static MemoryStream CreateFixtureZip(
        bool includeTraversalEntry = false,
        bool tamperPayload = false,
        Action<JsonObject>? mutateObjectIndex = null)
    {
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(FixtureDirectory, "*", SearchOption.AllDirectories))
        {
            if (Path.GetExtension(file).Equals(".md", StringComparison.OrdinalIgnoreCase))
                continue; // Fixture documentation is not part of the IKP package.
            var relative = Path.GetRelativePath(FixtureDirectory, file).Replace(Path.DirectorySeparatorChar, '/');
            files[relative] = File.ReadAllBytes(file);
        }

        if (mutateObjectIndex is not null)
        {
            var index = JsonNode.Parse(files["object-index.json"])!.AsObject();
            mutateObjectIndex(index);
            files["object-index.json"] = Encoding.UTF8.GetBytes(index.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            files["checksums.json"] = Encoding.UTF8.GetBytes(RebuildChecksumsJson(files));
        }

        var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (relative, bytes) in files)
            {
                var content = bytes;
                if (tamperPayload && relative == "payload/customizations/brule/7F3A.prg")
                {
                    content = (byte[])bytes.Clone();
                    content[0] ^= 0x01;
                }

                var entry = archive.CreateEntry(relative, CompressionLevel.Optimal);
                using var target = entry.Open();
                target.Write(content);
            }

            if (includeTraversalEntry)
            {
                var entry = archive.CreateEntry("../escape.txt");
                using var writer = new StreamWriter(entry.Open());
                writer.Write("not allowed");
            }
        }
        output.Position = 0;
        return output;
    }

    private static string RebuildChecksumsJson(IReadOnlyDictionary<string, byte[]> files)
    {
        var entries = files
            .Where(x => x.Key != "checksums.json")
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => new
            {
                path = x.Key,
                byteLength = x.Value.LongLength,
                sha256 = Convert.ToHexString(SHA256.HashData(x.Value)).ToLowerInvariant()
            })
            .ToArray();

        var canonical = new StringBuilder();
        foreach (var entry in entries)
            canonical.Append(entry.path).Append('\n').Append(entry.byteLength).Append('\n').Append(entry.sha256).Append('\n');

        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()))).ToLowerInvariant();
        var checksums = new
        {
            schemaVersion = "1",
            algorithm = "SHA-256",
            files = entries,
            logicalContentDigest = new { algorithm = "SHA-256", value = digest }
        };
        return JsonSerializer.Serialize(checksums, new JsonSerializerOptions { WriteIndented = true });
    }
}
