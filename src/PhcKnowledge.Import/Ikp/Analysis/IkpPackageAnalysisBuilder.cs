namespace PhcKnowledge.Import.Ikp.Analysis;

public sealed class IkpPackageAnalysisBuilder
{
    private readonly MaterializationReadinessEvaluator _readinessEvaluator = new();

    public PackageImportAnalysis Build(
        Guid organizationId,
        Guid receiptId,
        IkpPackageInspection inspection,
        bool archiveSha256Verified,
        DateTimeOffset analyzedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(inspection);

        var validation = inspection.Validation;
        var incompatibilities = new List<string>();
        if (!string.Equals(validation.ContractVersion, "0.1", StringComparison.Ordinal))
            incompatibilities.Add($"Contrato IKP incompatível: {validation.ContractVersion}.");

        var integrity = new PackageImportIntegrityAnalysis(
            ValidationSucceeded: true,
            ArchiveSha256Verified: archiveSha256Verified,
            InternalChecksumsVerified: inspection.InternalChecksumsVerified,
            incompatibilities);

        var objectIndex = BuildObjectIndex(inspection.Objects);
        var artifacts = BuildArtifacts(inspection.Artifacts);
        var relations = BuildRelations(inspection.Relations);
        var limitations = BuildLimitations(inspection);
        var unavailableObjects = BuildUnavailableObjects(inspection.Objects, inspection.Issues);
        var readiness = _readinessEvaluator.Evaluate(
            integrity,
            validation.ContractVersion,
            inspection.Issues,
            inspection.Coverage);

        return new PackageImportAnalysis(
            organizationId,
            receiptId,
            validation.PackageId,
            validation.CaptureId,
            validation.ContractVersion,
            validation.ProducerProductId,
            validation.ProducerProductVersion,
            validation.OriginDatabaseName,
            inspection.OriginEngine,
            inspection.OriginServerLabel,
            inspection.OriginPhcVersion,
            inspection.CaptureMode,
            inspection.MetadataStatus,
            inspection.ScannerVersion,
            validation.ZipSha256,
            validation.LogicalContentDigest,
            PackageImportAnalysisSchema.Version,
            analyzedAtUtc,
            integrity,
            inspection.Capabilities,
            inspection.Coverage,
            objectIndex,
            artifacts,
            relations,
            inspection.Issues,
            limitations,
            readiness,
            unavailableObjects);
    }

    public PackageImportAnalysis BuildBlocked(
        Guid organizationId,
        Guid receiptId,
        Guid packageId,
        Guid captureId,
        string contractVersion,
        string producerProductId,
        string producerProductVersion,
        string? originDatabaseName,
        string zipSha256,
        string? logicalContentDigest,
        bool archiveSha256Verified,
        IReadOnlyList<string> incompatibilities,
        DateTimeOffset analyzedAtUtc)
    {
        var integrity = new PackageImportIntegrityAnalysis(
            ValidationSucceeded: false,
            ArchiveSha256Verified: archiveSha256Verified,
            InternalChecksumsVerified: false,
            incompatibilities);

        var readiness = _readinessEvaluator.Evaluate(
            integrity,
            contractVersion,
            Array.Empty<IkpInspectedIssue>(),
            Array.Empty<IkpInspectedCoverageOperation>());

        return new PackageImportAnalysis(
            organizationId,
            receiptId,
            packageId,
            captureId,
            contractVersion,
            producerProductId,
            producerProductVersion,
            originDatabaseName,
            OriginEngine: "unavailable",
            OriginServerLabel: null,
            OriginPhcVersion: null,
            CaptureMode: null,
            MetadataStatus: null,
            ScannerVersion: null,
            zipSha256,
            logicalContentDigest ?? string.Empty,
            PackageImportAnalysisSchema.Version,
            analyzedAtUtc,
            integrity,
            Array.Empty<IkpInspectedCapabilityOperation>(),
            Array.Empty<IkpInspectedCoverageOperation>(),
            new PackageImportObjectIndexAnalysis(
                0,
                new SortedDictionary<string, int>(StringComparer.Ordinal),
                new SortedDictionary<string, int>(StringComparer.Ordinal),
                0, 0, 0, 0, 0, 0, 0, 0, 0),
            new PackageImportArtifactsAnalysis(
                0, false, Array.Empty<IkpInspectedArtifact>(),
                new SortedDictionary<string, int>(StringComparer.Ordinal)),
            new PackageImportRelationsAnalysis(
                false, 0, 0, 0, new SortedDictionary<string, int>(StringComparer.Ordinal)),
            Array.Empty<IkpInspectedIssue>(),
            Array.Empty<string>(),
            readiness,
            Array.Empty<PackageImportUnavailableObject>());
    }

    internal static IReadOnlyList<PackageImportUnavailableObject> BuildUnavailableObjects(
        IReadOnlyList<IkpInspectedObject> objects,
        IReadOnlyList<IkpInspectedIssue> issues)
    {
        var issuesByOperation = issues
            .Where(x => !string.IsNullOrWhiteSpace(x.OperationId))
            .GroupBy(x => x.OperationId!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);

        var list = new List<PackageImportUnavailableObject>();
        foreach (var item in objects.Where(x =>
                     string.Equals(x.DefinitionStatus, "Unavailable", StringComparison.Ordinal)))
        {
            IkpInspectedIssue? matched = null;
            if (issuesByOperation.TryGetValue(item.OperationId, out var candidates))
            {
                matched = candidates.FirstOrDefault(x =>
                    string.Equals(x.SourceObjectId, item.SourceObjectId, StringComparison.Ordinal));
                // Preserve missing sourceObjectId on issues: do not invent a match by inventing ids.
                // Only attach an issue when it explicitly references this object, or when a single
                // operation-scoped issue exists without sourceObjectId (informational, not identity).
                if (matched is null &&
                    candidates.Length == 1 &&
                    string.IsNullOrWhiteSpace(candidates[0].SourceObjectId))
                {
                    // Still do not bind a single unscoped issue to every Unavailable object —
                    // that would invent relationships. Leave association null.
                    matched = null;
                }
            }

            list.Add(new PackageImportUnavailableObject(
                item.SourceObjectId,
                item.ObjectKind,
                item.DefinitionStatus,
                item.OperationId,
                item.Name,
                matched?.IssueId,
                matched?.Code,
                matched?.Message));
        }

        return list
            .OrderBy(x => x.SourceObjectId, StringComparer.Ordinal)
            .ToArray();
    }

    private static PackageImportObjectIndexAnalysis BuildObjectIndex(
        IReadOnlyList<IkpInspectedObject> objects)
    {
        var byKind = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var byStatus = new SortedDictionary<string, int>(StringComparer.Ordinal);
        var withDefinition = 0;
        var withoutDefinition = 0;
        var available = 0;
        var empty = 0;
        var unavailable = 0;
        var notCollected = 0;
        var sourceIdentity = 0;
        var metadataFingerprint = 0;
        var definitionFingerprintAvailable = 0;
        var definitionFingerprintUnavailable = 0;

        foreach (var item in objects)
        {
            byKind[item.ObjectKind] = byKind.TryGetValue(item.ObjectKind, out var kindCount) ? kindCount + 1 : 1;
            byStatus[item.DefinitionStatus] = byStatus.TryGetValue(item.DefinitionStatus, out var statusCount)
                ? statusCount + 1
                : 1;

            if (item.HasDefinitionArtifacts) withDefinition++;
            else withoutDefinition++;

            switch (item.DefinitionStatus)
            {
                case "Available": available++; break;
                case "Empty": empty++; break;
                case "Unavailable": unavailable++; break;
                case "NotCollected": notCollected++; break;
            }

            if (item.HasSourceIdentity) sourceIdentity++;
            if (item.HasMetadataFingerprint) metadataFingerprint++;
            if (item.DefinitionFingerprintStatus == "Available") definitionFingerprintAvailable++;
            if (item.DefinitionFingerprintStatus == "Unavailable") definitionFingerprintUnavailable++;
        }

        return new PackageImportObjectIndexAnalysis(
            objects.Count,
            byKind,
            byStatus,
            withDefinition,
            withoutDefinition,
            available,
            empty,
            unavailable,
            notCollected,
            sourceIdentity,
            metadataFingerprint,
            definitionFingerprintAvailable,
            definitionFingerprintUnavailable);
    }

    private static PackageImportArtifactsAnalysis BuildArtifacts(
        IReadOnlyList<IkpInspectedArtifact> artifacts)
    {
        var byCategory = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var artifact in artifacts)
            byCategory[artifact.Category] = byCategory.TryGetValue(artifact.Category, out var count) ? count + 1 : 1;

        return new PackageImportArtifactsAnalysis(
            artifacts.Count,
            ReferencesValidated: true,
            artifacts,
            byCategory);
    }

    private static PackageImportRelationsAnalysis BuildRelations(IkpInspectedRelations? relations)
    {
        if (relations is null)
        {
            return new PackageImportRelationsAnalysis(
                false, 0, 0, 0, new SortedDictionary<string, int>(StringComparer.Ordinal));
        }

        var bySourceType = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var relation in relations.DeclaredRelations)
        {
            var key = string.IsNullOrWhiteSpace(relation.SourceType) ? "unknown" : relation.SourceType;
            bySourceType[key] = bySourceType.TryGetValue(key, out var count) ? count + 1 : 1;
        }

        return new PackageImportRelationsAnalysis(
            true,
            relations.DeclaredRelations.Count,
            relations.Interpretations.Count,
            relations.OrphanInterpretationCount,
            bySourceType);
    }

    private static IReadOnlyList<string> BuildLimitations(IkpPackageInspection inspection)
    {
        var limitations = new List<string>();

        if (string.Equals(inspection.CaptureMode, "Selective", StringComparison.Ordinal) ||
            string.Equals(inspection.CaptureMode, "ProbeOnly", StringComparison.Ordinal))
        {
            limitations.Add($"Capture mode {inspection.CaptureMode}: a ausência de objectos não implica remoção.");
        }

        if (string.Equals(inspection.MetadataStatus, "Partial", StringComparison.Ordinal))
            limitations.Add("metadataStatus Partial: a captura pode omitir metadados.");

        foreach (var capability in inspection.Capabilities.Where(x => !x.Supported))
            limitations.Add($"Capability não suportada: {capability.OperationId}.");

        foreach (var operation in inspection.Coverage)
        {
            if (!operation.Requested)
                limitations.Add($"Operação não solicitada: {operation.OperationId} (result={operation.Result}).");
            if (operation.Result is "Skipped" or "NotAttempted" or "Failed")
                limitations.Add($"Coverage {operation.Result}: {operation.OperationId}.");
            if (string.Equals(operation.ContentAvailability, "Partial", StringComparison.Ordinal) ||
                string.Equals(operation.ContentAvailability, "Unavailable", StringComparison.Ordinal) ||
                string.Equals(operation.ContentAvailability, "Empty", StringComparison.Ordinal))
            {
                limitations.Add(
                    $"contentAvailability {operation.ContentAvailability} em {operation.OperationId}.");
            }
        }

        if (inspection.Relations is null)
            limitations.Add("Ficheiro de relações declaradas ausente no pacote.");

        return limitations;
    }
}
