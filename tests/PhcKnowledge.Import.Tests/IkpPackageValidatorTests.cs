using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Receipts;
using PhcKnowledge.Import.Storage;

namespace PhcKnowledge.Import.Tests;

public sealed class IkpPackageValidatorTests
{
    private static string FixtureDirectory => Path.Combine(AppContext.BaseDirectory, "Fixtures", "minimal-valid");
    private static string SchemaDirectory => Path.Combine(AppContext.BaseDirectory, "Schemas");

    [Fact]
    public async Task Duplicate_sourceObjectId_is_rejected_by_csharp_uniqueness_check()
    {
        await using var package = CreateMutatedFixtureZip(root =>
        {
            var objectIndexPath = Path.Combine(root, "object-index.json");
            var node = JsonNode.Parse(File.ReadAllText(objectIndexPath))!
                .AsObject();
            var objects = node["objects"]!.AsArray();
            var duplicate = JsonNode.Parse(objects[0]!.ToJsonString())!;
            objects.Add(duplicate);
            File.WriteAllText(objectIndexPath, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            RebuildChecksums(root);
        });

        var validator = new IkpPackageValidator(SchemaDirectory);
        var ex = await Assert.ThrowsAsync<IkpPackageValidationException>(
            () => validator.ValidateAsync(package));

        Assert.Contains("sourceObjectId duplicado", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Schema inválido", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Duplicate_definition_artifactIds_are_rejected_by_json_schema()
    {
        await using var package = CreateMutatedFixtureZip(root =>
        {
            var objectIndexPath = Path.Combine(root, "object-index.json");
            var node = JsonNode.Parse(File.ReadAllText(objectIndexPath))!
                .AsObject();
            var definition = node["objects"]![0]!["definition"]!.AsObject();
            var artifactIds = definition["artifactIds"]!.AsArray();
            var existing = artifactIds[0]!.GetValue<string>();
            artifactIds.Add(existing);
            File.WriteAllText(objectIndexPath, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            // Schema evaluation runs before checksum verification, so checksums need not be rebuilt.
        });

        var validator = new IkpPackageValidator(SchemaDirectory);
        var ex = await Assert.ThrowsAsync<IkpPackageValidationException>(
            () => validator.ValidateAsync(package));

        Assert.Contains("Schema inválido (object-index.json)", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("sourceObjectId duplicado", ex.Message, StringComparison.Ordinal);
    }

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

            byte[] packageBytes;
            await using (var fixture = CreateFixtureZip())
                packageBytes = fixture.ToArray();
            await using var firstPackage = new MemoryStream(packageBytes);
            var first = await ingestion.ReceiveValidateArchiveAsync(firstPackage);
            Assert.False(first.Archive.AlreadyExisted);

            var archivedPath = Path.Combine(archiveRoot, first.Archive.StorageKey.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(archivedPath));
            Assert.True((File.GetAttributes(archivedPath) & FileAttributes.ReadOnly) != 0);

            await using var secondPackage = new MemoryStream(packageBytes);
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
        try
        {
            var validator = new IkpPackageValidator(SchemaDirectory);
            var store = new PhcKnowledge.Import.Storage.FileSystemImmutablePackageStore(Path.Combine(root, "archive"));
            var attemptRepository = new RecordingIngestionAttemptRepository();
            var ingestion = new IkpPackageIngestionService(
                validator, store, Path.Combine(root, "staging"),
                attemptRepository: attemptRepository);

            byte[] packageBytes;
            await using (var fixture = CreateFixtureZip())
                packageBytes = fixture.ToArray();
            await using var package = new MemoryStream(packageBytes);
            var result = await ingestion.ReceiveValidateArchiveAsync(package, organizationId);

            Assert.NotNull(result.Receipt);
            Assert.Equal(organizationId, result.Receipt.OrganizationId);
            Assert.Equal(result.Validation.PackageId, result.Receipt.PackageId);
            Assert.Equal(result.Archive.StorageKey, result.Receipt.StorageKey);
            Assert.Equal(organizationId, attemptRepository.OrganizationId);

            await using var duplicatePackage = new MemoryStream(packageBytes);
            var duplicate = await ingestion.ReceiveValidateArchiveAsync(duplicatePackage, organizationId);
            Assert.Equal(result.Receipt.ReceiptId, duplicate.Receipt?.ReceiptId);
            Assert.True(duplicate.Archive.AlreadyExisted);
            Assert.Equal(2, attemptRepository.CreateCalls);
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

    private sealed class RecordingIngestionAttemptRepository : IPackageReceiptAttemptRepository
    {
        private PackageReceiptAttempt? _attempt;
        private Guid? _leaseToken;
        private PackageReceipt? _receipt;
        public int CreateCalls { get; private set; }
        public Guid? OrganizationId => _attempt?.OrganizationId;

        public Task<PackageReceiptAttemptRegistration> CreateOrGetPendingAsync(
            Guid organizationId, IkpPackageValidationResult validation, string storageKey,
            string stagingKey, CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            var now = DateTimeOffset.UtcNow;
            _attempt ??= new PackageReceiptAttempt(
                Guid.NewGuid(), organizationId, validation, storageKey, stagingKey,
                "PendingArchive", 0, now, null, null, null, null, now, now);
            return Task.FromResult(new PackageReceiptAttemptRegistration(_attempt, CreateCalls > 1));
        }

        public Task<PackageReceiptAttemptLease?> ClaimByIdAsync(
            Guid attemptId, TimeSpan leaseDuration, DateTimeOffset nowUtc,
            CancellationToken cancellationToken = default)
        {
            if (_attempt is null || _attempt.AttemptId != attemptId || _attempt.Status != "PendingArchive")
                return Task.FromResult<PackageReceiptAttemptLease?>(null);
            _leaseToken = Guid.NewGuid();
            _attempt = _attempt with { LeaseToken = _leaseToken, LeaseUntilUtc = nowUtc.Add(leaseDuration) };
            return Task.FromResult<PackageReceiptAttemptLease?>(
                new PackageReceiptAttemptLease(_attempt, _leaseToken.Value));
        }

        public Task<IReadOnlyList<PackageReceiptAttemptLease>> ClaimDueAsync(
            int batchSize, TimeSpan leaseDuration, DateTimeOffset nowUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PackageReceiptAttemptLease>>(Array.Empty<PackageReceiptAttemptLease>());

        public Task<PackageReceipt?> GetCompletedReceiptAsync(
            Guid attemptId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_attempt is { Status: "Completed" } completed && completed.AttemptId == attemptId ? _receipt : null);

        public Task<IReadOnlySet<string>> GetProtectedStagingKeysAsync(
            CancellationToken cancellationToken = default)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            if (_attempt is { Status: "PendingArchive" or "NeedsAttention" })
                keys.Add(_attempt.StagingKey);
            return Task.FromResult<IReadOnlySet<string>>(keys);
        }

        public Task<bool> RenewLeaseAsync(Guid attemptId, Guid leaseToken,
            DateTimeOffset leaseUntilUtc, CancellationToken cancellationToken = default)
        {
            if (_attempt is null || _attempt.AttemptId != attemptId || _leaseToken != leaseToken)
                return Task.FromResult(false);
            _attempt = _attempt with { LeaseUntilUtc = leaseUntilUtc };
            return Task.FromResult(true);
        }

        public Task<PackageReceiptRegistration> CompleteAsync(Guid attemptId, Guid leaseToken,
            ArchivedPackage archive, CancellationToken cancellationToken = default)
        {
            if (_attempt is null || _attempt.AttemptId != attemptId || _leaseToken != leaseToken)
                throw new InvalidOperationException("Lease inválida.");
            var validation = _attempt.Validation;
            var receipt = new PackageReceipt(Guid.NewGuid(), _attempt.OrganizationId,
                validation.PackageId, validation.CaptureId, validation.ContractVersion,
                validation.ProducerProductId, validation.ProducerProductVersion,
                validation.OriginDatabaseName, validation.ZipSha256, validation.ZipByteLength,
                archive.StorageKey, DateTimeOffset.UtcNow, "{}");
            _receipt = receipt;
            _attempt = _attempt with { Status = "Completed", LeaseToken = null, LeaseUntilUtc = null };
            return Task.FromResult(new PackageReceiptRegistration(receipt, false));
        }

        public Task<bool> ScheduleRetryAsync(Guid attemptId, Guid leaseToken,
            DateTimeOffset nextAttemptAtUtc, string errorCode, string errorMessage,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> MarkNeedsAttentionAsync(Guid attemptId, Guid leaseToken,
            string errorCode, string errorMessage, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
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

    private static MemoryStream CreateMutatedFixtureZip(Action<string> mutate)
    {
        var root = Path.Combine(Path.GetTempPath(), $"phc-ikp-fixture-{Guid.NewGuid():N}");
        try
        {
            CopyFixture(FixtureDirectory, root);
            mutate(root);
            return ZipDirectory(root);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    private static void CopyFixture(string source, string destination)
    {
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            if (Path.GetExtension(file).Equals(".md", StringComparison.OrdinalIgnoreCase))
                continue;
            var relative = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
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
