using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Json.Schema;

namespace PhcKnowledge.Import.Ikp;

/// <summary>
/// Shared IKP 0.1 extract/validate/project pipeline used by the Validator and Reader.
/// Performs a single parse pass over the ZIP contents.
/// </summary>
public sealed class IkpPackageInspectionEngine
{
    private static readonly string[] ContractFiles =
    [
        "manifest.json", "capabilities.json", "coverage.json",
        "object-index.json", "checksums.json"
    ];

    private static readonly IReadOnlyDictionary<string, string> SchemaFiles =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["manifest.json"] = "manifest.schema.json",
            ["capabilities.json"] = "capabilities.schema.json",
            ["coverage.json"] = "coverage.schema.json",
            ["object-index.json"] = "object-index.schema.json",
            ["checksums.json"] = "checksums.schema.json"
        };

    private readonly string _schemaDirectory;
    private readonly long _maxZipBytes;
    private readonly long _maxExpandedBytes;
    private readonly long _maxEntryBytes;
    private readonly int _maxEntries;

    public IkpPackageInspectionEngine(
        string? schemaDirectory = null,
        long maxZipBytes = 250L * 1024 * 1024,
        long maxExpandedBytes = 500L * 1024 * 1024,
        long maxEntryBytes = 100L * 1024 * 1024,
        int maxEntries = 10_000)
    {
        _schemaDirectory = schemaDirectory ?? Path.Combine(AppContext.BaseDirectory, "Schemas");
        _maxZipBytes = maxZipBytes;
        _maxExpandedBytes = maxExpandedBytes;
        _maxEntryBytes = maxEntryBytes;
        _maxEntries = maxEntries;
    }

    public async Task<IkpPackageInspection> InspectAsync(
        Stream packageStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(packageStream);
        var zipPath = Path.Combine(Path.GetTempPath(), $"ikp-upload-{Guid.NewGuid():N}.zip");
        var extractionPath = Path.Combine(Path.GetTempPath(), $"ikp-validate-{Guid.NewGuid():N}");
        try
        {
            var zipHash = await CopyAndHashAsync(packageStream, zipPath, _maxZipBytes, cancellationToken);
            Directory.CreateDirectory(extractionPath);
            await ExtractSafelyAsync(zipPath, extractionPath, cancellationToken);
            var inspection = await InspectExtractedPackageAsync(extractionPath, cancellationToken);
            return inspection with
            {
                Validation = inspection.Validation with
                {
                    ZipSha256 = zipHash.Hash,
                    ZipByteLength = zipHash.Length
                }
            };
        }
        catch (IkpPackageValidationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidDataException or JsonException or IOException or ArgumentException or FormatException)
        {
            throw new IkpPackageValidationException($"Pacote IKP inválido: {ex.Message}", ex);
        }
        finally
        {
            TryDeleteFile(zipPath);
            TryDeleteDirectory(extractionPath);
        }
    }

    private async Task<(string Hash, long Length)> CopyAndHashAsync(
        Stream source, string destination, long maxBytes, CancellationToken cancellationToken)
    {
        await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long total = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0) break;
            total += read;
            if (total > maxBytes) throw new IkpPackageValidationException("O ZIP excede o limite de tamanho permitido.");
            hash.AppendData(buffer, 0, read);
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        if (total == 0) throw new IkpPackageValidationException("O pacote recebido está vazio.");
        await output.FlushAsync(cancellationToken);
        return (Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant(), total);
    }

    private async Task ExtractSafelyAsync(string zipPath, string root, CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        if (archive.Entries.Count == 0 || archive.Entries.Count > _maxEntries)
            throw new IkpPackageValidationException("Número de entradas ZIP inválido.");

        var names = new HashSet<string>(StringComparer.Ordinal);
        var folded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long expanded = 0;

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var isDirectory = entry.FullName.EndsWith("/", StringComparison.Ordinal);
            var rawName = isDirectory ? entry.FullName[..^1] : entry.FullName;
            var name = NormalizeSafePath(rawName);
            if (isDirectory)
            {
                var directoryPath = Path.GetFullPath(Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar)));
                var directoryPrefix = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
                if (!directoryPath.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase))
                    throw new IkpPackageValidationException($"Directório ZIP fora da pasta temporária: {name}");
                Directory.CreateDirectory(directoryPath);
                continue;
            }
            if (!names.Add(name) || !folded.Add(name))
                throw new IkpPackageValidationException($"Entrada ZIP duplicada ou com colisão de maiúsculas/minúsculas: {name}");
            if ((entry.ExternalAttributes >> 16 & 0xF000) == 0xA000)
                throw new IkpPackageValidationException($"Links simbólicos não são permitidos no ZIP: {name}");
            if (entry.Length < 0 || entry.Length > _maxEntryBytes)
                throw new IkpPackageValidationException($"Entrada ZIP excede o limite permitido: {name}");
            expanded += entry.Length;
            if (expanded > _maxExpandedBytes)
                throw new IkpPackageValidationException("O conteúdo descomprimido excede o limite permitido.");
            if (entry.Length > 0 && entry.CompressedLength == 0 ||
                entry.CompressedLength > 0 && entry.Length / Math.Max(1, entry.CompressedLength) > 200)
                throw new IkpPackageValidationException($"Rácio de compressão suspeito: {name}");

            var destination = Path.GetFullPath(Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar)));
            var rootPrefix = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
            if (!destination.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                throw new IkpPackageValidationException($"Entrada ZIP fora da pasta temporária: {name}");

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            await using var input = entry.Open();
            await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            await input.CopyToAsync(output, 81920, cancellationToken);
        }
    }

    private async Task<IkpPackageInspection> InspectExtractedPackageAsync(
        string root, CancellationToken cancellationToken)
    {
        var documents = new Dictionary<string, JsonDocument>(StringComparer.Ordinal);
        try
        {
            foreach (var name in ContractFiles)
            {
                var path = Path.Combine(root, name);
                if (!File.Exists(path)) throw new IkpPackageValidationException($"Ficheiro obrigatório em falta: {name}");
                var json = await File.ReadAllBytesAsync(path, cancellationToken);
                documents[name] = JsonDocument.Parse(json);
                ValidateSchema(name, json);
            }

            var manifest = documents["manifest.json"].RootElement;
            var capabilities = documents["capabilities.json"].RootElement;
            var coverage = documents["coverage.json"].RootElement;
            var objectIndex = documents["object-index.json"].RootElement;
            var checksums = documents["checksums.json"].RootElement;

            var packageId = ReadGuid(manifest, "packageId");
            var captureId = ReadGuid(manifest.GetProperty("capture"), "captureId");
            var producer = manifest.GetProperty("producer");
            var capture = manifest.GetProperty("capture");
            var origin = manifest.GetProperty("origin");

            if (manifest.GetProperty("ikpContractVersion").GetString() != "0.1")
                throw new IkpPackageValidationException("Versão IKP não suportada.");
            var contentType = manifest.GetProperty("contentType").GetString()!;
            if (contentType != "phc.installation.knowledge.package")
                throw new IkpPackageValidationException("contentType não reconhecido.");
            var captureMode = capture.GetProperty("mode").GetString()!;
            if (captureMode is not ("Full" or "Selective" or "ProbeOnly" or "Unknown"))
                throw new IkpPackageValidationException("Modo de captura inválido.");
            var metadataStatus = capture.GetProperty("metadataStatus").GetString()!;
            if (metadataStatus is not ("Complete" or "Partial"))
                throw new IkpPackageValidationException("metadataStatus inválido.");
            var originEngine = origin.GetProperty("engine").GetString()!;
            if (originEngine != "sqlserver")
                throw new IkpPackageValidationException("Motor de origem não suportado.");
            if (ReadGuid(coverage, "captureId") != captureId || ReadGuid(objectIndex, "captureId") != captureId)
                throw new IkpPackageValidationException("CaptureId divergente entre os ficheiros de contrato.");

            var checksummed = await ValidateChecksumsAsync(root, checksums, cancellationToken);
            var artifacts = ValidateArtifacts(manifest.GetProperty("artifacts"), checksummed);
            var capabilityOps = ProjectCapabilities(capabilities, out var operationIds);
            var issues = await ProjectIssuesAsync(root, cancellationToken);
            var issueIds = issues.Select(x => x.IssueId).ToHashSet(StringComparer.Ordinal);
            if (issueIds.Count != issues.Count)
                throw new IkpPackageValidationException("issueId duplicado.");
            var coverageOps = ProjectCoverage(coverage, operationIds, issueIds);
            var coverageById = coverageOps.ToDictionary(x => x.OperationId, StringComparer.Ordinal);
            var objects = ProjectObjectIndex(objectIndex, operationIds, coverageById, artifacts);
            var relations = await ProjectDeclaredRelationsAsync(root, captureId, issueIds, cancellationToken);

            ValidateRootContents(root);

            var validation = new IkpPackageValidationResult(
                packageId, captureId, "0.1",
                producer.GetProperty("productId").GetString()!,
                producer.GetProperty("productVersion").GetString()!,
                origin.TryGetProperty("databaseName", out var db) ? db.GetString() : null,
                string.Empty, 0,
                ContractFiles.Length, checksummed.Count, artifacts.Count,
                objects.Count, coverageById.Count, issues.Count,
                checksums.GetProperty("logicalContentDigest").GetProperty("value").GetString()!);

            return new IkpPackageInspection(
                validation,
                InternalChecksumsVerified: true,
                contentType,
                captureMode,
                metadataStatus,
                originEngine,
                origin.TryGetProperty("serverLabel", out var server) ? server.GetString() : null,
                origin.TryGetProperty("phcVersion", out var phcVersion) ? phcVersion.GetString() : null,
                capture.TryGetProperty("scannerVersion", out var scannerVersion) ? scannerVersion.GetString() : null,
                capabilityOps,
                coverageOps,
                objects,
                artifacts.Values.OrderBy(x => x.ArtifactId, StringComparer.Ordinal).ToArray(),
                issues,
                relations);
        }
        finally
        {
            foreach (var document in documents.Values) document.Dispose();
        }
    }

    private void ValidateSchema(string fileName, byte[] json)
    {
        var schemaPath = Path.Combine(_schemaDirectory, SchemaFiles[fileName]);
        if (!File.Exists(schemaPath))
            throw new IkpPackageValidationException($"Schema IKP em falta: {SchemaFiles[fileName]}");
        var schema = JsonSchema.FromText(File.ReadAllText(schemaPath));
        using var instance = JsonDocument.Parse(json);
        var result = schema.Evaluate(instance.RootElement);
        if (!result.IsValid)
            throw new IkpPackageValidationException($"Schema inválido ({fileName}): {result}");
    }

    private static async Task<Dictionary<string, (long Length, string Hash)>> ValidateChecksumsAsync(
        string root, JsonElement checksums, CancellationToken cancellationToken)
    {
        if (checksums.GetProperty("schemaVersion").GetString() != "1" ||
            checksums.GetProperty("algorithm").GetString() != "SHA-256")
            throw new IkpPackageValidationException("Formato de checksums não suportado.");

        var result = new Dictionary<string, (long Length, string Hash)>(StringComparer.Ordinal);
        foreach (var entry in checksums.GetProperty("files").EnumerateArray())
        {
            var relative = NormalizeSafePath(entry.GetProperty("path").GetString()!);
            if (!result.TryAdd(relative, (entry.GetProperty("byteLength").GetInt64(), entry.GetProperty("sha256").GetString()!)))
                throw new IkpPackageValidationException($"Path duplicado em checksums: {relative}");
        }

        var actual = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(path => path != "checksums.json")
            .ToHashSet(StringComparer.Ordinal);
        if (!actual.SetEquals(result.Keys))
            throw new IkpPackageValidationException("Os ficheiros do pacote não correspondem à lista checksums.");

        foreach (var item in result)
        {
            var path = Path.Combine(root, item.Key.Replace('/', Path.DirectorySeparatorChar));
            await using var stream = File.OpenRead(path);
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken)).ToLowerInvariant();
            var length = new FileInfo(path).Length;
            if (length != item.Value.Length || !string.Equals(hash, item.Value.Hash, StringComparison.Ordinal))
                throw new IkpPackageValidationException($"Tamanho ou SHA-256 divergente: {item.Key}");
        }

        var canonical = new StringBuilder();
        foreach (var item in result.OrderBy(x => x.Key, StringComparer.Ordinal))
            canonical.Append(item.Key).Append('\n').Append(item.Value.Length).Append('\n').Append(item.Value.Hash).Append('\n');
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()))).ToLowerInvariant();
        var declaredDigest = checksums.GetProperty("logicalContentDigest").GetProperty("value").GetString();
        if (!string.Equals(digest, declaredDigest, StringComparison.Ordinal))
            throw new IkpPackageValidationException("logicalContentDigest divergente.");
        return result;
    }

    private static Dictionary<string, IkpInspectedArtifact> ValidateArtifacts(
        JsonElement artifactsElement, Dictionary<string, (long Length, string Hash)> checksums)
    {
        var artifacts = new Dictionary<string, IkpInspectedArtifact>(StringComparer.Ordinal);
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var artifact in artifactsElement.EnumerateArray())
        {
            var id = artifact.GetProperty("artifactId").GetString()!;
            var path = NormalizeSafePath(artifact.GetProperty("path").GetString()!);
            var projected = new IkpInspectedArtifact(
                id,
                path,
                artifact.GetProperty("category").GetString()!,
                artifact.GetProperty("format").GetString()!,
                artifact.TryGetProperty("role", out var role) ? role.GetString() : null,
                artifact.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.True,
                artifact.GetProperty("byteLength").GetInt64(),
                artifact.GetProperty("sha256").GetString()!);
            if (!artifacts.TryAdd(id, projected) || !paths.Add(path))
                throw new IkpPackageValidationException("artifactId ou path de artefacto duplicado.");
            if (!checksums.TryGetValue(path, out var checksum) ||
                checksum.Length != projected.ByteLength ||
                !string.Equals(checksum.Hash, projected.Sha256, StringComparison.Ordinal))
                throw new IkpPackageValidationException($"Artefacto não corresponde aos checksums: {path}");
        }
        var payloadPaths = checksums.Keys.Where(x => x.StartsWith("payload/", StringComparison.Ordinal)).ToHashSet(StringComparer.Ordinal);
        if (!payloadPaths.SetEquals(paths))
            throw new IkpPackageValidationException("A lista de artefactos não corresponde aos ficheiros payload.");
        return artifacts;
    }

    private static IReadOnlyList<IkpInspectedCapabilityOperation> ProjectCapabilities(
        JsonElement capabilities, out HashSet<string> operationIds)
    {
        if (capabilities.GetProperty("schemaVersion").GetString() != "1")
            throw new IkpPackageValidationException("Versão de capabilities não suportada.");
        operationIds = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<IkpInspectedCapabilityOperation>();
        foreach (var operation in capabilities.GetProperty("operations").EnumerateArray())
        {
            var id = operation.GetProperty("operationId").GetString()!;
            if (!operationIds.Add(id))
                throw new IkpPackageValidationException("operationId duplicado em capabilities.");
            if (!operation.TryGetProperty("supported", out var supported) || supported.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new IkpPackageValidationException("Capability sem indicador supported válido.");
            list.Add(new IkpInspectedCapabilityOperation(
                id,
                operation.GetProperty("categoryId").GetString()!,
                operation.GetProperty("capabilityVersion").GetString()!,
                operation.GetProperty("collectorId").GetString()!,
                supported.GetBoolean(),
                operation.TryGetProperty("description", out var description) ? description.GetString() : null));
        }
        return list;
    }

    private static async Task<IReadOnlyList<IkpInspectedIssue>> ProjectIssuesAsync(
        string root, CancellationToken cancellationToken)
    {
        var path = Path.Combine(root, "payload", "diagnostics", "issues.json");
        if (!File.Exists(path)) return Array.Empty<IkpInspectedIssue>();
        using var doc = JsonDocument.Parse(await File.ReadAllBytesAsync(path, cancellationToken));
        if (!doc.RootElement.TryGetProperty("issues", out var issues))
            return Array.Empty<IkpInspectedIssue>();

        var list = new List<IkpInspectedIssue>();
        foreach (var issue in issues.EnumerateArray())
        {
            list.Add(new IkpInspectedIssue(
                issue.GetProperty("issueId").GetString()!,
                issue.TryGetProperty("operationId", out var operationId) ? operationId.GetString() : null,
                issue.GetProperty("severity").GetString()!,
                issue.GetProperty("code").GetString()!,
                issue.GetProperty("message").GetString()!,
                issue.TryGetProperty("sourceObjectId", out var sourceObjectId) ? sourceObjectId.GetString() : null,
                issue.TryGetProperty("details", out var details) ? details.GetRawText() : null));
        }
        return list;
    }

    private static IReadOnlyList<IkpInspectedCoverageOperation> ProjectCoverage(
        JsonElement coverage, HashSet<string> operations, HashSet<string> issueIds)
    {
        if (coverage.GetProperty("schemaVersion").GetString() != "1")
            throw new IkpPackageValidationException("Versão de coverage não suportada.");
        var entries = new List<IkpInspectedCoverageOperation>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in coverage.GetProperty("operations").EnumerateArray())
        {
            var id = item.GetProperty("operationId").GetString()!;
            if (!operations.Contains(id) || !ids.Add(id))
                throw new IkpPackageValidationException($"Operação de coverage inválida ou duplicada: {id}");
            if (!item.GetProperty("requested").GetBoolean() && item.GetProperty("result").GetString() != "Skipped")
                throw new IkpPackageValidationException($"Operação não solicitada sem resultado Skipped: {id}");
            var linkedIssueIds = item.GetProperty("issueIds").EnumerateArray().Select(x => x.GetString()!).ToArray();
            foreach (var issueId in linkedIssueIds)
                if (!issueIds.Contains(issueId))
                    throw new IkpPackageValidationException($"issueId não resolvido na coverage: {issueId}");

            IReadOnlyDictionary<string, string>? scope = null;
            if (item.TryGetProperty("scope", out var scopeElement) && scopeElement.ValueKind == JsonValueKind.Object)
            {
                var map = new SortedDictionary<string, string>(StringComparer.Ordinal);
                foreach (var property in scopeElement.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                        map[property.Name] = property.Value.GetString()!;
                }
                scope = map;
            }

            entries.Add(new IkpInspectedCoverageOperation(
                id,
                item.GetProperty("capabilityVersion").GetString()!,
                item.GetProperty("requested").GetBoolean(),
                item.GetProperty("result").GetString()!,
                item.GetProperty("sourceAvailability").GetString()!,
                item.GetProperty("contentAvailability").GetString()!,
                item.TryGetProperty("observedCount", out var observed) && observed.TryGetInt32(out var count) ? count : null,
                linkedIssueIds,
                scope));
        }
        if (!ids.SetEquals(operations))
            throw new IkpPackageValidationException("Coverage não contém exactamente todas as operações declaradas.");
        return entries;
    }

    private static IReadOnlyList<IkpInspectedObject> ProjectObjectIndex(
        JsonElement objectIndex, HashSet<string> operations,
        Dictionary<string, IkpInspectedCoverageOperation> coverage,
        Dictionary<string, IkpInspectedArtifact> artifacts)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<IkpInspectedObject>();
        foreach (var item in objectIndex.GetProperty("objects").EnumerateArray())
        {
            var id = item.GetProperty("sourceObjectId").GetString()!;
            var operationId = item.GetProperty("operationId").GetString()!;
            if (!ids.Add(id)) throw new IkpPackageValidationException($"sourceObjectId duplicado: {id}");
            if (!operations.Contains(operationId) || !coverage.TryGetValue(operationId, out var operation))
                throw new IkpPackageValidationException($"operationId não declarado: {operationId}");
            if (!operation.Requested ||
                operation.Result is "Failed" or "Skipped" or "NotAttempted")
                throw new IkpPackageValidationException($"Objecto ligado a operação sem observação válida: {id}");
            var definition = item.GetProperty("definition");
            var status = definition.GetProperty("status").GetString()!;
            var artifactIds = definition.GetProperty("artifactIds").EnumerateArray().Select(x => x.GetString()!).ToArray();
            if (artifactIds.Any(artifactId => !artifacts.ContainsKey(artifactId)))
                throw new IkpPackageValidationException($"artifactId não resolvido para objecto: {id}");
            if (status == "Available" && artifactIds.Length == 0)
                throw new IkpPackageValidationException($"Definição Available sem artefacto: {id}");

            list.Add(new IkpInspectedObject(
                id,
                item.GetProperty("objectKind").GetString()!,
                operationId,
                status,
                artifactIds,
                artifactIds.Length > 0,
                item.TryGetProperty("sourceIdentity", out _),
                item.TryGetProperty("metadataFingerprint", out _),
                definition.TryGetProperty("definitionFingerprint", out _),
                item.TryGetProperty("name", out var name) ? name.GetString() : null,
                item.TryGetProperty("schemaName", out var schemaName) ? schemaName.GetString() : null));
        }
        return list;
    }

    private static async Task<IkpInspectedRelations?> ProjectDeclaredRelationsAsync(
        string root, Guid captureId, HashSet<string> issueIds, CancellationToken cancellationToken)
    {
        var path = Path.Combine(root, "payload", "relations", "declared-relations.json");
        if (!File.Exists(path)) return null;
        using var doc = JsonDocument.Parse(await File.ReadAllBytesAsync(path, cancellationToken));
        if (ReadGuid(doc.RootElement, "captureId") != captureId)
            throw new IkpPackageValidationException("CaptureId divergente nas relações declaradas.");

        var declared = new List<IkpInspectedDeclaredRelation>();
        var relationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var relation in doc.RootElement.GetProperty("declaredRelations").EnumerateArray())
        {
            var sourceObjectId = relation.GetProperty("sourceObjectId").GetString()!;
            relationIds.Add(sourceObjectId);
            declared.Add(new IkpInspectedDeclaredRelation(
                sourceObjectId,
                relation.TryGetProperty("sourceType", out var sourceType) ? sourceType.GetString() : null,
                relation.TryGetProperty("sourceStamp", out var sourceStamp) ? sourceStamp.GetString() : null));
        }

        var interpretations = new List<IkpInspectedRelationInterpretation>();
        var orphanCount = 0;
        foreach (var interpretation in doc.RootElement.GetProperty("interpretations").EnumerateArray())
        {
            var sourceObjectId = interpretation.GetProperty("sourceObjectId").GetString()!;
            if (!relationIds.Contains(sourceObjectId))
            {
                orphanCount++;
                throw new IkpPackageValidationException("Interpretação sem relação declarada correspondente.");
            }
            if (interpretation.TryGetProperty("issueIds", out var ids))
                foreach (var issueId in ids.EnumerateArray().Select(x => x.GetString()!))
                    if (!issueIds.Contains(issueId))
                        throw new IkpPackageValidationException($"issueId não resolvido nas relações: {issueId}");

            interpretations.Add(new IkpInspectedRelationInterpretation(
                interpretation.GetProperty("interpretationId").GetString()!,
                sourceObjectId,
                interpretation.TryGetProperty("status", out var status) ? status.GetString() : null));
        }

        return new IkpInspectedRelations(
            doc.RootElement.GetProperty("schemaVersion").GetString()!,
            captureId,
            declared,
            interpretations,
            orphanCount);
    }

    private static void ValidateRootContents(string root)
    {
        foreach (var item in Directory.EnumerateFileSystemEntries(root))
        {
            var name = Path.GetFileName(item);
            if (File.Exists(item) && !ContractFiles.Contains(name, StringComparer.Ordinal))
                throw new IkpPackageValidationException($"Ficheiro extra na raiz do pacote: {name}");
            if (Directory.Exists(item) && name != "payload")
                throw new IkpPackageValidationException($"Directório extra na raiz do pacote: {name}");
        }
    }

    private static string NormalizeSafePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains('\\') || value.Contains('\0'))
            throw new IkpPackageValidationException($"Path inválido: {value}");
        var normalized = value.Normalize(NormalizationForm.FormC);
        if (normalized.StartsWith("/", StringComparison.Ordinal) ||
            Regex.IsMatch(normalized, "^[A-Za-z]:") ||
            normalized.Split('/').Any(part => part is "" or "." or ".."))
            throw new IkpPackageValidationException($"Path absoluto, não canónico ou traversal: {value}");
        return normalized;
    }

    private static Guid ReadGuid(JsonElement element, string property)
    {
        if (!Guid.TryParse(element.GetProperty(property).GetString(), out var value))
            throw new IkpPackageValidationException($"UUID inválido: {property}");
        return value;
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    private static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); } catch { }
    }
}
