using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PhcKnowledgeScanner.Abstractions.Coverage;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Inventory.Expression;
using PhcKnowledgeScanner.Abstractions.Manifest;
using PhcKnowledgeScanner.Abstractions.Workspace;
using PhcKnowledgeScanner.Export.ObjectIndex;

namespace PhcKnowledgeScanner.Export.Ikp;

/// <summary>
/// Produces an immutable IKP 0.1 package from an analyzed Scanner workspace.
/// It reads only artifacts referenced by the current capture's objects.
/// </summary>
public sealed class IkpPackageExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public Task<IkpPackageExportResult> ExportAsync(
        IScanWorkspace workspace,
        ScanManifest scanManifest,
        IkpOriginDescriptor origin,
        string zipPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scanManifest);
        return ExportAsync(workspace, new IkpCaptureDescriptor(
            scanManifest.CaptureId,
            scanManifest.ScannerVersion,
            scanManifest.StartedAtUtc,
            scanManifest.FinishedAtUtc,
            scanManifest.Mode.ToString()),
            origin, zipPath, cancellationToken);
    }

    /// <summary>Exports a prior workspace when its original execution manifest was not retained.</summary>
    public async Task<IkpPackageExportResult> ExportAsync(
        IScanWorkspace workspace,
        IkpCaptureDescriptor captureDescriptor,
        IkpOriginDescriptor origin,
        string zipPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(captureDescriptor);
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentException.ThrowIfNullOrWhiteSpace(zipPath);

        if (!Guid.TryParse(captureDescriptor.CaptureId, out var captureGuid))
            throw new InvalidDataException("CaptureId inválido: IKP 0.1 exige UUID.");
        if (!Guid.TryParse(workspace.CaptureId, out var workspaceCaptureGuid)
            || workspaceCaptureGuid != captureGuid)
            throw new InvalidDataException("CaptureId do workspace não corresponde ao descriptor da captura.");

        if (string.IsNullOrWhiteSpace(origin.DatabaseName))
            throw new InvalidDataException("O nome da base de dados de origem é obrigatório para exportar IKP.");
        if (captureDescriptor.StartedAtUtc.HasValue && captureDescriptor.CompletedAtUtc.HasValue
            && captureDescriptor.CompletedAtUtc < captureDescriptor.StartedAtUtc)
            throw new InvalidDataException("O fim da captura é anterior ao seu início.");

        var integrity = await workspace.CheckIntegrityAsync(cancellationToken).ConfigureAwait(false);
        if (!integrity.IsValid)
            throw new InvalidDataException("Workspace inválido: " + string.Join("; ", integrity.Messages));

        var capture = await workspace.GetCaptureMetadataAsync(cancellationToken).ConfigureAwait(false);
        if (!capture.HasCompleteCoverageMetadata
            || capture.Capabilities is null
            || capture.Coverage is null)
            throw new InvalidDataException("O workspace não contém CaptureId, capabilities e coverage completos.");

        var capabilities = capture.Capabilities;
        var coverage = capture.Coverage;
        ValidateCapabilityCoverage(capabilities, coverage);
        if (!string.Equals(capture.CaptureId, captureDescriptor.CaptureId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("CaptureId do metadata SQLite não corresponde ao descriptor da captura.");

        var input = await new ObjectIndexProjectionInputReader()
            .ReadAsync(workspace, cancellationToken).ConfigureAwait(false);
        var projection = new ObjectIndexProjector().Project(input);
        if (!projection.IsSuccess || projection.JsonBytes is null)
        {
            var errors = string.Join(Environment.NewLine, projection.Diagnostics.Select(x => $"{x.Code}: {x.Message}"));
            throw new InvalidDataException("Não foi possível projetar o Object Index:" + Environment.NewLine + errors);
        }

        var artifactMetadata = input.Artifacts.ToDictionary(x => x.ArtifactId, StringComparer.Ordinal);
        var referencedIds = input.Objects
            .Select(x => x.DefinitionArtifactId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var files = new List<PackageFile>();
        foreach (var artifactId in referencedIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!artifactMetadata.TryGetValue(artifactId, out var metadata))
                throw new InvalidDataException($"O objeto referencia o artefacto '{artifactId}', mas não existe metadata no workspace.");

            var snapshot = input.DefinitionArtifacts.SingleOrDefault(x => string.Equals(x.ArtifactId, artifactId, StringComparison.Ordinal));
            if (snapshot?.Content is null)
                throw new InvalidDataException($"O artefacto '{artifactId}' não tem bytes legíveis.");

            var digest = Sha256(snapshot.Content);
            if (snapshot.ExpectedByteLength != snapshot.Content.LongLength)
                throw new InvalidDataException($"ByteLength divergente no artefacto '{artifactId}'.");
            if (string.IsNullOrWhiteSpace(snapshot.ExpectedSha256)
                || !string.Equals(digest, snapshot.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"SHA-256 divergente no artefacto '{artifactId}'.");

            if (string.IsNullOrWhiteSpace(metadata.ContentType))
                throw new InvalidDataException($"ContentType em falta no artefacto '{artifactId}'.");

            var path = BuildArtifactPath(metadata);
            files.Add(new PackageFile(
                artifactId,
                path,
                metadata.CollectorId,
                metadata.ContentType,
                snapshot.Content,
                true,
                "source-definition"));
        }

        var issues = await workspace.GetIssuesAsync(cancellationToken).ConfigureAwait(false);
        AddGeneratedFile(
            files,
            "ikp:diagnostics:collection-issues",
            "payload/diagnostics/collection-issues.json",
            "diagnostics",
            "application/json",
            JsonSerializer.SerializeToUtf8Bytes(issues, JsonOptions),
            "collection-issues");

        // PHC REL/DTL are declared facts, not derived relations or SQL foreign keys.
        var declaredRelations = await workspace.GetDeclaredRelationsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var declaredRelationInterpretations = await workspace.GetDeclaredRelationInterpretationsAsync(
            cancellationToken: cancellationToken).ConfigureAwait(false);
        ValidateDeclaredRelationCounts(coverage, declaredRelations);
        ValidateDeclaredRelationInterpretations(declaredRelations, declaredRelationInterpretations);

        var hasDeclaredRelationCapture = declaredRelations.Count > 0 || coverage.Any(x =>
            x.Requested
            && (x.OperationId == KnownOperations.PhcRel || x.OperationId == KnownOperations.PhcDtl)
            && x.ContentAvailability is ContentAvailability.Available
                or ContentAvailability.Partial
                or ContentAvailability.Empty);

        if (hasDeclaredRelationCapture)
        {
            AddGeneratedFile(
                files,
                "ikp:relations:declared",
                "payload/relations/declared-relations.json",
                "relations",
                "application/json",
                JsonSerializer.SerializeToUtf8Bytes(new
                {
                    schemaVersion = "1",
                    captureId = captureGuid.ToString("D"),
                    records = declaredRelations
                }, JsonOptions),
                "declared-relations");

            if (declaredRelationInterpretations.Count > 0)
            {
                AddGeneratedFile(
                    files,
                    "ikp:relations:declared-interpretations",
                    "payload/relations/declared-relation-interpretations.json",
                    "relations",
                    "application/json",
                    JsonSerializer.SerializeToUtf8Bytes(new
                    {
                        schemaVersion = "1",
                        captureId = captureGuid.ToString("D"),
                        records = declaredRelationInterpretations
                    }, JsonOptions),
                    "declared-relation-interpretations");
            }
        }

        if (projection.Diagnostics.Count > 0)
        {
            AddGeneratedFile(
                files,
                "ikp:diagnostics:object-index-projection",
                "payload/diagnostics/object-index-projection.json",
                "diagnostics",
                "application/json",
                JsonSerializer.SerializeToUtf8Bytes(projection.Diagnostics, JsonOptions),
                "object-index-projection-diagnostics");
        }

        EnsureUniqueArtifacts(files);

        var effectiveCoverage = ReconcileDefinitionCoverage(coverage, input);

        var packageId = Guid.NewGuid().ToString("D");
        var producedAt = DateTimeOffset.UtcNow;
        var capabilitiesBytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "1",
            scannerVersion = captureDescriptor.ScannerVersion ?? "Unknown",
            operations = capabilities
        }, JsonOptions);
        var coverageBytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "1",
            captureId = captureGuid.ToString("D"),
            operations = effectiveCoverage
        }, JsonOptions);

        var manifestBytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            ikpContractVersion = "0.1",
            contentType = "phc.installation.knowledge.package",
            packageId,
            producedAtUtc = producedAt,
            producer = new
            {
                productId = "PhcKnowledgeScanner",
                productVersion = typeof(IkpPackageExporter).Assembly.GetName().Version?.ToString(3) ?? "0.1.0",
                exportTool = "PhcKnowledgeScanner.Export"
            },
            capture = new
            {
                captureId = captureGuid.ToString("D"),
                startedAtUtc = captureDescriptor.StartedAtUtc,
                completedAtUtc = captureDescriptor.CompletedAtUtc,
                mode = captureDescriptor.Mode ?? "Unknown",
                scannerVersion = captureDescriptor.ScannerVersion ?? "Unknown",
                metadataStatus = captureDescriptor.ScannerVersion is null
                    || captureDescriptor.StartedAtUtc is null
                    || captureDescriptor.CompletedAtUtc is null
                    || captureDescriptor.Mode is null ? "Partial" : "Complete",
                unknownFields = new[]
                {
                    captureDescriptor.ScannerVersion is null ? "scannerVersion" : null,
                    captureDescriptor.StartedAtUtc is null ? "startedAtUtc" : null,
                    captureDescriptor.CompletedAtUtc is null ? "completedAtUtc" : null,
                    captureDescriptor.Mode is null ? "mode" : null
                }.Where(x => x is not null).ToArray()
            },
            origin = new
            {
                engine = "sqlserver",
                databaseName = origin.DatabaseName,
                serverLabel = origin.ServerLabel,
                originFingerprint = origin.OriginFingerprint,
                phcVersion = origin.PhcVersion,
                phcSignatureDetected = origin.PhcSignatureDetected
            },
            artifacts = files.OrderBy(x => x.Path, StringComparer.Ordinal).Select(x => new
            {
                artifactId = x.ArtifactId,
                path = x.Path,
                category = x.Category,
                format = x.Format,
                byteLength = x.Content.LongLength,
                sha256 = Sha256(x.Content),
                required = x.Required,
                role = x.Role
            }).ToArray()
        }, JsonOptions);

        var packageEntries = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["manifest.json"] = manifestBytes,
            ["capabilities.json"] = capabilitiesBytes,
            ["coverage.json"] = coverageBytes,
            ["object-index.json"] = projection.JsonBytes
        };
        foreach (var file in files)
            packageEntries.Add(file.Path, file.Content);

        var checksumRows = packageEntries
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => new ChecksumFile(x.Key, x.Value.LongLength, Sha256(x.Value)))
            .ToArray();
        var logicalDigest = ComputeLogicalDigest(checksumRows);
        var checksumsBytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "1",
            algorithm = "SHA-256",
            files = checksumRows,
            logicalContentDigest = new { algorithm = "SHA-256", value = logicalDigest }
        }, JsonOptions);

        packageEntries.Add("checksums.json", checksumsBytes);
        await WriteZipAtomicallyAsync(packageEntries, zipPath, cancellationToken).ConfigureAwait(false);

        var zipInfo = new FileInfo(zipPath);
        string zipSha256;
        await using (var zipStream = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            zipSha256 = Convert.ToHexString(await SHA256.HashDataAsync(zipStream, cancellationToken).ConfigureAwait(false)).ToLowerInvariant();
        }

        return new IkpPackageExportResult(
            packageId,
            captureGuid.ToString("D"),
            zipInfo.FullName,
            zipInfo.Length,
            zipSha256,
            files.Count,
            packageEntries.Count,
            logicalDigest,
            projection.Diagnostics);
    }

    private static IReadOnlyList<OperationCoverage> ReconcileDefinitionCoverage(
        IReadOnlyList<OperationCoverage> coverage,
        ObjectIndexProjectionInput input)
    {
        var definitions = DefinitionProjectionResolver.ResolveAll(
            input.Objects,
            input.DefinitionArtifacts,
            input.Issues);

        var unavailableByOperation = input.Objects
            .Select(record => new
            {
                Record = record,
                Operation = ObjectOperationResolver.Resolve(record)
            })
            .Where(x => x.Operation.IsResolved)
            .Where(x => definitions.TryGetValue(x.Record.ObjectId, out var definition)
                && definition.Status == DefinitionStatus.Unavailable)
            .GroupBy(x => x.Operation.OperationId!, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);

        return coverage.Select(item =>
        {
            if (item.ContentAvailability != ContentAvailability.Available
                || !unavailableByOperation.TryGetValue(item.OperationId, out var unavailableCount)
                || unavailableCount == 0)
            {
                return item;
            }

            return item with
            {
                ContentAvailability = ContentAvailability.Partial,
                Result = item.Result == OperationExecutionResult.Succeeded
                    ? OperationExecutionResult.SucceededWithWarnings
                    : item.Result
            };
        }).ToArray();
    }

    private static void ValidateDeclaredRelationCounts(
        IReadOnlyList<OperationCoverage> coverage,
        IReadOnlyList<DeclaredRelationRecord> declaredRelations)
    {
        var expected = new[]
        {
            (OperationId: KnownOperations.PhcRel, SourceType: DeclaredRelationSourceTypes.PhcRel),
            (OperationId: KnownOperations.PhcDtl, SourceType: DeclaredRelationSourceTypes.PhcDtl)
        };

        foreach (var item in expected)
        {
            var operation = coverage.SingleOrDefault(x => x.OperationId == item.OperationId);
            if (operation is null
                || !operation.Requested
                || operation.Result is OperationExecutionResult.Failed
                    or OperationExecutionResult.Skipped
                    or OperationExecutionResult.NotAttempted
                || operation.ContentAvailability is not (ContentAvailability.Available
                    or ContentAvailability.Partial
                    or ContentAvailability.Empty))
            {
                continue;
            }

            var actualCount = declaredRelations.Count(x => x.SourceType == item.SourceType);
            if (actualCount != operation.ObservedCount)
            {
                throw new InvalidDataException(
                    $"Declared relation payload count mismatch for '{item.OperationId}': " +
                    $"coverage observed {operation.ObservedCount}, workspace contains {actualCount}.");
            }
        }
    }

    private static void ValidateDeclaredRelationInterpretations(
        IReadOnlyList<DeclaredRelationRecord> declaredRelations,
        IReadOnlyList<DeclaredRelationInterpretationRecord> interpretations)
    {
        var declaredIds = declaredRelations
            .Select(x => x.RelationId)
            .ToHashSet(StringComparer.Ordinal);

        var orphaned = interpretations
            .Where(x => !declaredIds.Contains(x.RelationId))
            .Select(x => x.RelationId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        if (orphaned.Length > 0)
        {
            throw new InvalidDataException(
                "Existem interpretações REL/DTL sem relação declarada correspondente: " +
                string.Join(", ", orphaned));
        }
    }

    private static void ValidateCapabilityCoverage(
        IReadOnlyList<PhcKnowledgeScanner.Abstractions.Coverage.DeclaredCapability> capabilities,
        IReadOnlyList<PhcKnowledgeScanner.Abstractions.Coverage.OperationCoverage> coverage)
    {
        var capabilityGroups = capabilities
            .GroupBy(x => x.OperationId, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.Ordinal);
        if (capabilityGroups.Any(x => x.Value.Length != 1))
            throw new InvalidDataException("O catálogo de capabilities contém operationId duplicado.");

        var coverageGroups = coverage
            .GroupBy(x => x.OperationId, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.Ordinal);
        if (coverageGroups.Any(x => x.Value.Length != 1))
            throw new InvalidDataException("A coverage contém operationId duplicado.");

        foreach (var item in coverage)
        {
            if (!capabilityGroups.TryGetValue(item.OperationId, out var declared)
                || declared.Length != 1
                || !declared[0].Supported
                || !string.Equals(declared[0].CapabilityVersion, item.CapabilityVersion, StringComparison.Ordinal))
                throw new InvalidDataException($"Coverage sem capability suportada compatível: '{item.OperationId}'.");
        }

        foreach (var capability in capabilities.Where(x => x.Supported))
        {
            if (!coverageGroups.ContainsKey(capability.OperationId))
                throw new InvalidDataException($"Não existe coverage para a capability suportada '{capability.OperationId}'.");
        }
    }

    private static string BuildArtifactPath(WorkspaceArtifactMetadata artifact)
    {
        var collector = NormalizeSegment(artifact.CollectorId);
        var relative = NormalizeRelativePath(artifact.RelativePath);
        var path = $"payload/{collector}/{relative}";
        return NormalizeRelativePath(path);
    }

    private static string NormalizeSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value is "." or ".."
            || value.Contains('/') || value.Contains('\\')
            || Path.IsPathRooted(value))
            throw new InvalidDataException($"Segmento de caminho inválido: '{value}'.");
        return value.Normalize(NormalizationForm.FormC);
    }

    private static string NormalizeRelativePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidDataException("Caminho de artefacto vazio.");

        var normalized = value.Replace('\\', '/').Normalize(NormalizationForm.FormC);
        if (normalized.StartsWith('/') || Path.IsPathRooted(normalized)
            || (normalized.Length >= 2 && char.IsLetter(normalized[0]) && normalized[1] == ':'))
            throw new InvalidDataException($"Caminho absoluto não permitido: '{value}'.");

        var segments = normalized.Split('/');
        if (segments.Any(x => string.IsNullOrWhiteSpace(x) || x is "." or ".."))
            throw new InvalidDataException($"Caminho inválido ou com traversal: '{value}'.");

        foreach (var segment in segments)
        {
            if (segment.Any(c => char.IsControl(c) || "<>:\"|?*".Contains(c))
                || segment.EndsWith(' ') || segment.EndsWith('.'))
                throw new InvalidDataException($"O segmento de caminho não é portável entre sistemas: '{segment}'.");

            var deviceName = segment.Split('.')[0];
            if (deviceName.Equals("CON", StringComparison.OrdinalIgnoreCase)
                || deviceName.Equals("PRN", StringComparison.OrdinalIgnoreCase)
                || deviceName.Equals("AUX", StringComparison.OrdinalIgnoreCase)
                || deviceName.Equals("NUL", StringComparison.OrdinalIgnoreCase)
                || (deviceName.Length == 4
                    && (deviceName.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
                        || deviceName.StartsWith("LPT", StringComparison.OrdinalIgnoreCase))
                    && deviceName[3] is >= '1' and <= '9'))
                throw new InvalidDataException($"Nome reservado pelo Windows no caminho do artefacto: '{segment}'.");
        }

        return string.Join('/', segments);
    }

    private static void AddGeneratedFile(
        ICollection<PackageFile> files,
        string artifactId,
        string path,
        string category,
        string format,
        byte[] content,
        string role) =>
        files.Add(new PackageFile(artifactId, path, category, format, content, true, role));

    private static void EnsureUniqueArtifacts(IReadOnlyCollection<PackageFile> files)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            if (!ids.Add(file.ArtifactId))
                throw new InvalidDataException($"ArtifactId duplicado: '{file.ArtifactId}'.");
            if (!paths.Add(file.Path))
                throw new InvalidDataException($"Caminho de artefacto duplicado: '{file.Path}'.");
        }
    }

    private static string ComputeLogicalDigest(IEnumerable<ChecksumFile> files)
    {
        var builder = new StringBuilder();
        foreach (var file in files.OrderBy(x => x.Path, StringComparer.Ordinal))
        {
            var path = NormalizeRelativePath(file.Path);
            builder.Append(path).Append('\n')
                .Append(file.ByteLength.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\n')
                .Append(file.Sha256).Append('\n');
        }

        return Sha256(Encoding.UTF8.GetBytes(builder.ToString()));
    }

    private static string Sha256(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static async Task WriteZipAtomicallyAsync(
        IReadOnlyDictionary<string, byte[]> entries,
        string zipPath,
        CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(zipPath);
        var directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                81920, FileOptions.Asynchronous | FileOptions.WriteThrough))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false))
            {
                foreach (var entry in entries.OrderBy(x => x.Key, StringComparer.Ordinal))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var zipEntry = archive.CreateEntry(entry.Key, CompressionLevel.Optimal);
                    await using var entryStream = zipEntry.Open();
                    await entryStream.WriteAsync(entry.Value, cancellationToken).ConfigureAwait(false);
                }
            }

            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private sealed record PackageFile(
        string ArtifactId,
        string Path,
        string Category,
        string Format,
        byte[] Content,
        bool Required,
        string Role);

    private sealed record ChecksumFile(string Path, long ByteLength, string Sha256);
}

public sealed record IkpCaptureDescriptor(
    string CaptureId,
    string? ScannerVersion,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? Mode);

public sealed record IkpOriginDescriptor(
    string DatabaseName,
    string? ServerLabel = null,
    string? OriginFingerprint = null,
    string? PhcVersion = null,
    bool? PhcSignatureDetected = null);

public sealed record IkpPackageExportResult(
    string PackageId,
    string CaptureId,
    string ZipPath,
    long ByteLength,
    string ZipSha256,
    int PayloadArtifactCount,
    int PackageFileCount,
    string LogicalContentDigest,
    IReadOnlyList<ObjectIndexProjectionDiagnostic> Diagnostics);
