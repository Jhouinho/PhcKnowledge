using PhcKnowledge.Import.Receipts;

namespace PhcKnowledge.Host.Http;

/// <summary>
/// Public HTTP allowlist for ingestion status. Never serialize Import entities directly.
/// </summary>
public sealed record IkpIngestionStatusDto(
    Guid AttemptId,
    Guid PackageId,
    Guid CaptureId,
    string Status,
    int AttemptCount,
    int ObjectCount,
    int ArtifactCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed record IkpIngestionAcceptedDto(
    Guid AttemptId,
    Guid PackageId,
    Guid CaptureId,
    string Status,
    int AttemptCount,
    int ObjectCount,
    int ArtifactCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string StatusUrl);

/// <summary>Minimal 202 body when the full status row cannot be loaded.</summary>
public sealed record IkpIngestionPendingDto(
    Guid AttemptId,
    string Status,
    string StatusUrl);

public static class IkpPublicDtoMapper
{
    public static IkpIngestionStatusDto ToStatusDto(PackageIngestionStatus status) =>
        new(
            status.AttemptId,
            status.PackageId,
            status.CaptureId,
            status.Status,
            status.AttemptCount,
            status.ObjectCount,
            status.ArtifactCount,
            status.CreatedAtUtc,
            status.UpdatedAtUtc,
            status.CompletedAtUtc);

    public static IkpIngestionAcceptedDto ToAcceptedDto(PackageIngestionStatus status, string statusUrl) =>
        new(
            status.AttemptId,
            status.PackageId,
            status.CaptureId,
            status.Status,
            status.AttemptCount,
            status.ObjectCount,
            status.ArtifactCount,
            status.CreatedAtUtc,
            status.UpdatedAtUtc,
            status.CompletedAtUtc,
            statusUrl);
}
