using System.IO.Compression;
using PhcKnowledge.Import.Ikp;

namespace PhcKnowledge.Import.Tests;

public sealed class IkpPackageValidatorTests
{
    private static string FixtureDirectory => Path.Combine(AppContext.BaseDirectory, "Fixtures", "minimal-valid");
    private static string SchemaDirectory => Path.Combine(AppContext.BaseDirectory, "Schemas");

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

    private static MemoryStream CreateFixtureZip(bool includeTraversalEntry = false, bool tamperPayload = false)
    {
        var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in Directory.EnumerateFiles(FixtureDirectory, "*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(file).Equals(".md", StringComparison.OrdinalIgnoreCase))
                    continue; // Fixture documentation is not part of the IKP package.
                var relative = Path.GetRelativePath(FixtureDirectory, file).Replace(Path.DirectorySeparatorChar, '/');
                var entry = archive.CreateEntry(relative, CompressionLevel.Optimal);
                using var target = entry.Open();
                var bytes = File.ReadAllBytes(file);
                if (tamperPayload && relative == "payload/customizations/brule/7F3A.prg")
                    bytes[0] ^= 0x01;
                target.Write(bytes);
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
}