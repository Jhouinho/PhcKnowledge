using System.Collections.Concurrent;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using PhcKnowledge.Import.Contracts;

namespace PhcKnowledge.Import.Validation;

/// <summary>
/// Validates an IKP 0.1 archive without extracting it or mutating persistent state.
/// The validator intentionally fails closed: an invalid package never reaches staging.
/// </summary>
public sealed class IkpPackageValidator
{
    private static readonly string[] RequiredDocuments =
    [
        "manifest.json",
        "capabilities.json",
        "coverage.json",
        "object-index.json",
        "checksums.json"
    ];

    private static readonly ConcurrentDictionary<string, JsonSchema> SchemaCache = new(StringComparer.Ordinal);
    private static readonly EvaluationOptions SchemaEvaluationOptions = new()
    {
        OutputFormat = OutputFormat.List,
        RequireFormatValidation = true
    };

    private const int MaxArchiveEntries = 100_000;
    private const long MaxEntryBytes = 256L * 1024 * 1024;
    private const long MaxExpandedBytes = 1024L * 1024 * 1024;

    public async Task<IkpPackageValidationResult> ValidateAsync(
        Stream packageStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(packageStream);

        var errors = new List<string>();
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        try
        {
            using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read, leaveOpen: true);
            if (archive.Entries.Count > MaxArchiveEntries)
                return IkpPackageValidationResult.Invalid($"O pacote excede o limite de {MaxArchiveEntries} entradas.");

            long expandedBytes = 0;
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
                {
                    var directoryPath = ValidateArchivePath(entry.FullName[..^1]);
                    if (directoryPath != "payload" &&
                        !directoryPath.StartsWith("payload/", StringComparison.Ordinal))
                        errors.Add($"Diretório fora da estrutura IKP permitida: {directoryPath}");
                    continue;
                }

                var path = ValidateArchivePath(entry.FullName);
                if (!IsAllowedPackagePath(path))
                    errors.Add($"Ficheiro fora da estrutura IKP permitida: {path}");

                if (!files.TryAdd(path, []))
                {
                    errors.Add($"Entrada ZIP duplicada: {path}");
                    continue;
                }

                if (entry.Length > MaxEntryBytes)
                {
                    errors.Add($"Ficheiro excede o limite permitido: {path}");
                    continue;
                }

                await using var input = entry.Open();
                await using var output = new MemoryStream(entry.Length > int.MaxValue ? 0 : (int)entry.Length);
                var buffer = new byte[81920];
                int read;
                while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    if (output.Length + read > MaxEntryBytes)
                        throw new InvalidDataException($"Ficheiro excede o limite permitido: {path}");
                    if (expandedBytes + output.Length + read > MaxExpandedBytes)
                        throw new InvalidDataException("O conteúdo expandido do pacote excede o limite permitido.");
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }

                expandedBytes = checked(expandedBytes + output.Length);
                files[path] = output.ToArray();
            }
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or ArgumentException or OverflowException)
        {
            return IkpPackageValidationResult.Invalid($"ZIP inválido: {ex.Message}");
        }

        foreach (var required in RequiredDocuments)
        {
            if (!files.ContainsKey(required))
                errors.Add($"Documento obrigatório em falta: {required}");
        }

        if (errors.Count > 0)
            return new(false, null, null, files.Count, 0, 0, 0, errors);

        try
        {
            using var manifestDoc = Parse(files["manifest.json"], "manifest.json");
            using var capabilitiesDoc = Parse(files["capabilities.json"], "capabilities.json");
            using var coverageDoc = Parse(files["coverage.json"], "coverage.json");
            using var objectIndexDoc = Parse(files["object-index.json"], "object-index.json");
            using var checksumsDoc = Parse(files["checksums.json"], "checksums.json");

            var manifest = manifestDoc.RootElement;
            var capabilities = capabilitiesDoc.RootElement;
            var coverage = coverageDoc.RootElement;
            var objectIndex = objectIndexDoc.RootElement;
            var checksums = checksumsDoc.RootElement;

            ValidateSchema("manifest.schema.json", manifest, errors);
            ValidateSchema("capabilities.schema.json", capabilities, errors);
            ValidateSchema("coverage.schema.json", coverage, errors);
            ValidateSchema("object-index.schema.json", objectIndex, errors);
            ValidateSchema("checksums.schema.json", checksums, errors);

            RequireString(manifest, "ikpContractVersion", "0.1");
            RequireString(manifest, "contentType", "phc.installation.knowledge.package");
            var packageId = RequiredString(manifest, "packageId");
            if (!Guid.TryParse(packageId, out _))
                errors.Add("manifest.packageId não é um UUID válido.");

            var capture = RequiredObject(manifest, "capture");
            var captureId = RequiredString(capture, "captureId");
            if (!Guid.TryParse(captureId, out _))
                errors.Add("manifest.capture.captureId não é um UUID válido.");
            RequiredString(capture, "mode");
            RequiredString(capture, "scannerVersion");
            RequiredString(capture, "metadataStatus");

            RequireString(checksums, "schemaVersion", "1");
            RequireString(checksums, "algorithm", "SHA-256");

            if (RequiredString(coverage, "captureId") != captureId)
                errors.Add("coverage: CaptureId divergente do manifesto.");
            if (RequiredString(objectIndex, "captureId") != captureId)
                errors.Add("object-index: CaptureId divergente do manifesto.");

            var checksumEntries = ReadChecksumEntries(checksums, errors);
            ValidateFileSet(files, checksumEntries, errors);
            ValidateChecksums(files, checksumEntries, checksums, errors);

            var artifacts = ReadArtifacts(manifest, errors);
            ValidateArtifacts(files, checksumEntries, artifacts, errors);

            var supportedOperations = ReadCapabilities(capabilities, errors);
            var issueIds = ReadIssueIds(files, errors);
            var coverageByOperation = ReadCoverage(coverage, supportedOperations, issueIds, errors);
            var objectCount = ValidateObjects(objectIndex, supportedOperations, coverageByOperation, artifacts, errors);
            ValidateDeclaredRelations(files, captureId, issueIds, errors);

            return new(
                errors.Count == 0,
                packageId,
                captureId,
                files.Count,
                artifacts.Count,
                objectCount,
                coverageByOperation.Count,
                errors);
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or KeyNotFoundException or InvalidOperationException or FormatException or OverflowException)
        {
            errors.Add($"Contrato IKP inválido: {ex.Message}");
            return new(false, null, null, files.Count, 0, 0, 0, errors);
        }
    }

    private static void ValidateSchema(string schemaFileName, JsonElement instance, List<string> errors)
    {
        var schema = SchemaCache.GetOrAdd(schemaFileName, static fileName =>
        {
            var resourceName = $"PhcKnowledge.Import.Schemas.{fileName}";
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Schema IKP embebido não encontrado: {resourceName}");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var schemaNode = JsonNode.Parse(reader.ReadToEnd())
                ?? throw new InvalidOperationException($"Schema IKP vazio: {resourceName}");

            // Large object-index arrays make generic uniqueItems comparisons expensive.
            // The contract's unique arrays are checked below by their stable identifiers/paths.
            RemoveUniqueItems(schemaNode);
            return JsonSchema.FromText(schemaNode.ToJsonString());
        });

        var result = schema.Evaluate(instance, SchemaEvaluationOptions);
        if (result.IsValid)
            return;

        foreach (var failure in FlattenSchemaErrors(result))
            errors.Add($"{schemaFileName} {failure.InstanceLocation}: {failure.Message}");
    }

    private static void RemoveUniqueItems(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            obj.Remove("uniqueItems");
            foreach (var key in obj.Select(property => property.Key).ToArray())
                RemoveUniqueItems(obj[key]);
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
                RemoveUniqueItems(item);
        }
    }

    private static IEnumerable<(string InstanceLocation, string Message)> FlattenSchemaErrors(EvaluationResults result)
    {
        if (result.Errors is { Count: > 0 })
        {
            foreach (var (keyword, message) in result.Errors)
                yield return (result.InstanceLocation.ToString(), $"{keyword}: {message}");
        }

        if (result.Details is null)
            yield break;

        foreach (var detail in result.Details)
        {
            if (!detail.IsValid)
            {
                foreach (var failure in FlattenSchemaErrors(detail))
                    yield return failure;
            }
        }
    }

    private static JsonDocument Parse(byte[] bytes, string name)
    {
        try
        {
            return JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 128 });
        }
        catch (JsonException ex)
        {
            throw new JsonException($"{name}: JSON inválido: {ex.Message}", ex);
        }
    }

    private static string ValidateArchivePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Contains('\\') || path.StartsWith("/", StringComparison.Ordinal))
            throw new InvalidDataException($"Caminho ZIP inseguro: {path}");

        var normalized = path.Normalize(NormalizationForm.FormC);
        if (!string.Equals(path, normalized, StringComparison.Ordinal) ||
            (path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':'))
            throw new InvalidDataException($"Caminho ZIP não canónico: {path}");

        var segments = path.Split('/');
        if (segments.Any(segment => segment.Length == 0 || segment is "." or ".."))
            throw new InvalidDataException($"Caminho ZIP com traversal: {path}");

        return path;
    }

    private static bool IsAllowedPackagePath(string path) =>
        RequiredDocuments.Contains(path, StringComparer.Ordinal) ||
        path.StartsWith("payload/", StringComparison.Ordinal);

    private static string RequiredString(JsonElement parent, string property)
    {
        if (!parent.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String)
            throw new InvalidDataException($"Campo obrigatório ausente ou inválido: {property}");
        var result = value.GetString();
        if (string.IsNullOrWhiteSpace(result))
            throw new InvalidDataException($"Campo obrigatório vazio: {property}");
        return result;
    }

    private static void RequireString(JsonElement parent, string property, string expected)
    {
        var actual = RequiredString(parent, property);
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
            throw new InvalidDataException($"{property}: valor não suportado '{actual}'.");
    }

    private static JsonElement RequiredObject(JsonElement parent, string property)
    {
        if (!parent.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"Objeto obrigatório ausente ou inválido: {property}");
        return value;
    }

    private static JsonElement RequiredArray(JsonElement parent, string property)
    {
        if (!parent.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException($"Array obrigatório ausente ou inválido: {property}");
        return value;
    }

    private static Dictionary<string, JsonElement> ReadChecksumEntries(JsonElement checksums, List<string> errors)
    {
        var result = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var entry in RequiredArray(checksums, "files").EnumerateArray())
        {
            var path = ValidateArchivePath(RequiredString(entry, "path"));
            if (!result.TryAdd(path, entry))
                errors.Add($"checksums: caminho duplicado: {path}");
            if (!entry.TryGetProperty("byteLength", out var length) || !length.TryGetInt64(out _) || length.GetInt64() < 0)
                errors.Add($"checksums: byteLength inválido: {path}");
            var hash = RequiredString(entry, "sha256");
            if (!IsSha256(hash))
                errors.Add($"checksums: SHA-256 inválido: {path}");
        }
        return result;
    }

    private static void ValidateFileSet(
        Dictionary<string, byte[]> files,
        Dictionary<string, JsonElement> checksums,
        List<string> errors)
    {
        var actual = files.Keys.Where(path => path != "checksums.json").ToHashSet(StringComparer.Ordinal);
        var expected = checksums.Keys.ToHashSet(StringComparer.Ordinal);
        foreach (var missing in expected.Except(actual))
            errors.Add($"checksums: ficheiro em falta: {missing}");
        foreach (var unlisted in actual.Except(expected))
            errors.Add($"checksums: ficheiro não declarado: {unlisted}");
    }

    private static void ValidateChecksums(
        Dictionary<string, byte[]> files,
        Dictionary<string, JsonElement> checksums,
        JsonElement checksumsDocument,
        List<string> errors)
    {
        foreach (var (path, entry) in checksums)
        {
            if (!files.TryGetValue(path, out var bytes))
                continue;
            if (entry.GetProperty("byteLength").GetInt64() != bytes.LongLength)
                errors.Add($"checksums: byteLength divergente: {path}");
            var expectedHash = RequiredString(entry, "sha256");
            var actualHash = Convert.ToHexStringLower(SHA256.HashData(bytes));
            if (!string.Equals(expectedHash, actualHash, StringComparison.Ordinal))
                errors.Add($"checksums: SHA-256 divergente: {path}");
        }

        var digestObject = RequiredObject(checksumsDocument, "logicalContentDigest");
        RequireString(digestObject, "algorithm", "SHA-256");
        var expectedDigest = RequiredString(digestObject, "value");
        var canonical = new StringBuilder();
        foreach (var path in checksums.Keys.OrderBy(value => value, StringComparer.Ordinal))
        {
            var entry = checksums[path];
            canonical.Append(path).Append('\n')
                .Append(entry.GetProperty("byteLength").GetInt64()).Append('\n')
                .Append(RequiredString(entry, "sha256")).Append('\n');
        }
        var actualDigest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
        if (!string.Equals(expectedDigest, actualDigest, StringComparison.Ordinal))
            errors.Add("checksums: logicalContentDigest divergente.");
    }

    private sealed record Artifact(string Id, string Path, long ByteLength, string Sha256);

    private static Dictionary<string, Artifact> ReadArtifacts(JsonElement manifest, List<string> errors)
    {
        var result = new Dictionary<string, Artifact>(StringComparer.Ordinal);
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in RequiredArray(manifest, "artifacts").EnumerateArray())
        {
            var id = RequiredString(item, "artifactId");
            var path = ValidateArchivePath(RequiredString(item, "path"));
            var length = item.GetProperty("byteLength").GetInt64();
            var hash = RequiredString(item, "sha256");
            if (!result.TryAdd(id, new(id, path, length, hash)))
                errors.Add($"manifest: artifactId duplicado: {id}");
            if (!paths.Add(path))
                errors.Add($"manifest: path de artefacto duplicado: {path}");
        }
        return result;
    }

    private static void ValidateArtifacts(
        Dictionary<string, byte[]> files,
        Dictionary<string, JsonElement> checksums,
        Dictionary<string, Artifact> artifacts,
        List<string> errors)
    {
        var payloadFiles = files.Keys.Where(path => path.StartsWith("payload/", StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);
        var artifactPaths = artifacts.Values.Select(item => item.Path).ToHashSet(StringComparer.Ordinal);
        foreach (var path in payloadFiles.Except(artifactPaths))
            errors.Add($"manifest: payload sem artefacto correspondente: {path}");
        foreach (var path in artifactPaths.Except(payloadFiles))
            errors.Add($"manifest: artefacto sem ficheiro payload: {path}");

        foreach (var artifact in artifacts.Values)
        {
            if (!files.TryGetValue(artifact.Path, out var bytes))
                continue;
            if (!checksums.ContainsKey(artifact.Path))
                errors.Add($"manifest: artefacto não consta em checksums: {artifact.Path}");
            if (artifact.ByteLength != bytes.LongLength ||
                !string.Equals(artifact.Sha256, Convert.ToHexStringLower(SHA256.HashData(bytes)), StringComparison.Ordinal))
                errors.Add($"manifest: integridade do artefacto divergente: {artifact.Path}");
        }
    }

    private static Dictionary<string, string> ReadCapabilities(JsonElement capabilities, List<string> errors)
    {
        var allOperationIds = new HashSet<string>(StringComparer.Ordinal);
        var supported = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var operation in RequiredArray(capabilities, "operations").EnumerateArray())
        {
            var id = RequiredString(operation, "operationId");
            if (!allOperationIds.Add(id))
                errors.Add($"capabilities: operationId duplicado: {id}");
            if (operation.GetProperty("supported").GetBoolean())
                supported[id] = RequiredString(operation, "capabilityVersion");
        }
        return supported;
    }

    private static HashSet<string> ReadIssueIds(
        Dictionary<string, byte[]> files,
        List<string> errors)
    {
        var issues = new HashSet<string>(StringComparer.Ordinal);
        const string currentPath = "payload/diagnostics/collection-issues.json";
        const string legacyPath = "payload/diagnostics/issues.json";
        var path = files.ContainsKey(currentPath) ? currentPath : legacyPath;
        if (!files.TryGetValue(path, out var bytes))
            return issues;

        using var document = Parse(bytes, path);
        var issueArray = document.RootElement.ValueKind == JsonValueKind.Array
            ? document.RootElement
            : RequiredArray(document.RootElement, "issues");
        foreach (var issue in issueArray.EnumerateArray())
        {
            if (!issue.TryGetProperty("issueId", out var idElement) ||
                idElement.ValueKind != JsonValueKind.String)
                continue;
            var id = idElement.GetString();
            if (!string.IsNullOrWhiteSpace(id) && !issues.Add(id))
                errors.Add($"diagnostics: issueId duplicado: {id}");
        }
        return issues;
    }

    private static Dictionary<string, JsonElement> ReadCoverage(
        JsonElement coverage,
        Dictionary<string, string> supportedOperations,
        HashSet<string> issues,
        List<string> errors)
    {
        var result = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var operation in RequiredArray(coverage, "operations").EnumerateArray())
        {
            var id = RequiredString(operation, "operationId");
            if (!result.TryAdd(id, operation))
                errors.Add($"coverage: operationId duplicado: {id}");
            if (!supportedOperations.TryGetValue(id, out var capabilityVersion))
                errors.Add($"coverage: operação não suportada ou não declarada: {id}");
            else if (!string.Equals(RequiredString(operation, "capabilityVersion"), capabilityVersion, StringComparison.Ordinal))
                errors.Add($"coverage: capabilityVersion divergente: {id}");
            var requested = operation.GetProperty("requested").GetBoolean();
            var operationResult = RequiredString(operation, "result");
            if (!requested && operationResult != "Skipped")
                errors.Add($"coverage: operação não solicitada deve ter resultado Skipped: {id}");
            var seenIssueIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var issueId in RequiredArray(operation, "issueIds").EnumerateArray())
            {
                var value = issueId.GetString() ?? "";
                if (!seenIssueIds.Add(value))
                    errors.Add($"coverage: issueId duplicado na operação {id}: {value}");
                if (!issues.Contains(value))
                    errors.Add($"coverage: issueId não resolvido: {value}");
            }
        }

        if (!result.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(supportedOperations))
            errors.Add("coverage: cada capability deve ter exatamente uma entrada de cobertura.");
        return result;
    }

    private static int ValidateObjects(
        JsonElement objectIndex,
        Dictionary<string, string> supportedOperations,
        Dictionary<string, JsonElement> coverage,
        Dictionary<string, Artifact> artifacts,
        List<string> errors)
    {
        var objectIds = new HashSet<string>(StringComparer.Ordinal);
        var count = 0;
        foreach (var obj in RequiredArray(objectIndex, "objects").EnumerateArray())
        {
            count++;
            var id = RequiredString(obj, "sourceObjectId");
            if (!objectIds.Add(id))
                errors.Add($"object-index: sourceObjectId duplicado: {id}");
            var operationId = RequiredString(obj, "operationId");
            if (!supportedOperations.ContainsKey(operationId))
                errors.Add($"object-index: operationId não suportado: {operationId}");
            if (!coverage.TryGetValue(operationId, out var operation))
            {
                errors.Add($"object-index: operação sem coverage: {operationId}");
                continue;
            }

            var result = RequiredString(operation, "result");
            if (!operation.GetProperty("requested").GetBoolean() ||
                result is "Failed" or "Skipped" or "NotAttempted")
                errors.Add($"object-index: objeto ligado a operação sem observação válida: {id}");

            var definition = RequiredObject(obj, "definition");
            var artifactIds = RequiredArray(definition, "artifactIds");
            var seenArtifactIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var artifactIdElement in artifactIds.EnumerateArray())
            {
                var artifactId = artifactIdElement.GetString() ?? "";
                if (!seenArtifactIds.Add(artifactId))
                    errors.Add($"object-index: artifactId duplicado em {id}: {artifactId}");
                if (!artifacts.ContainsKey(artifactId))
                    errors.Add($"object-index: artifactId não resolvido: {artifactId}");
            }
            if (RequiredString(definition, "status") == "Available" && artifactIds.GetArrayLength() == 0)
                errors.Add($"object-index: definição Available sem artefacto: {id}");
        }
        return count;
    }

    private static void ValidateDeclaredRelations(
        Dictionary<string, byte[]> files,
        string captureId,
        HashSet<string> issueIds,
        List<string> errors)
    {
        const string relationsPath = "payload/relations/declared-relations.json";
        const string interpretationsPath = "payload/relations/declared-relation-interpretations.json";
        if (!files.TryGetValue(relationsPath, out var relationBytes))
            return;

        using var relationDocument = Parse(relationBytes, relationsPath);
        var relationRoot = relationDocument.RootElement;
        if (RequiredString(relationRoot, "captureId") != captureId)
            errors.Add("relations: CaptureId divergente.");

        // Current Scanner exporter uses "records"; accept the original fixture's
        // "declaredRelations" shape as a compatible 0.1 representation.
        var relations = relationRoot.TryGetProperty("records", out var currentRecords)
            ? currentRecords
            : RequiredArray(relationRoot, "declaredRelations");

        var relationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var relation in relations.EnumerateArray())
        {
            var id = relation.TryGetProperty("relationId", out var relationId)
                ? relationId.GetString() ?? ""
                : RequiredString(relation, "sourceObjectId");
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidDataException("relations: RelationId vazio.");
            if (!relationIds.Add(id))
                errors.Add($"relations: RelationId duplicado: {id}");
        }

        JsonDocument? interpretationDocument = null;
        try
        {
            JsonElement interpretations;
            if (files.TryGetValue(interpretationsPath, out var interpretationBytes))
            {
                interpretationDocument = Parse(interpretationBytes, interpretationsPath);
                var interpretationRoot = interpretationDocument.RootElement;
                if (RequiredString(interpretationRoot, "captureId") != captureId)
                    errors.Add("relation interpretations: CaptureId divergente.");
                interpretations = RequiredArray(interpretationRoot, "records");
            }
            else if (relationRoot.TryGetProperty("interpretations", out var legacyInterpretations)
                     && legacyInterpretations.ValueKind == JsonValueKind.Array)
            {
                interpretations = legacyInterpretations;
            }
            else
            {
                return;
            }

            foreach (var interpretation in interpretations.EnumerateArray())
            {
                var id = interpretation.TryGetProperty("relationId", out var relationId)
                    ? relationId.GetString() ?? ""
                    : RequiredString(interpretation, "sourceObjectId");
                if (string.IsNullOrWhiteSpace(id))
                    throw new InvalidDataException("relation interpretations: RelationId vazio.");
                if (!relationIds.Contains(id))
                    errors.Add($"relations: interpretação sem relação declarada: {id}");

                if (interpretation.TryGetProperty("issueIds", out var referencedIssues)
                    && referencedIssues.ValueKind == JsonValueKind.Array)
                {
                    foreach (var issueIdElement in referencedIssues.EnumerateArray())
                    {
                        var issueId = issueIdElement.GetString() ?? "";
                        if (!issueIds.Contains(issueId))
                            errors.Add($"relations: issueId não resolvido: {issueId}");
                    }
                }
            }
        }
        finally
        {
            interpretationDocument?.Dispose();
        }
    }

    private static bool IsSha256(string value) =>
        value.Length == 64 && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
}
