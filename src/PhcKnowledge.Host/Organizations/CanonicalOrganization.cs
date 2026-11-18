namespace PhcKnowledge.Host.Organizations;

public sealed record CanonicalOrganization(
    Guid OrganizationId,
    string Name,
    DateTimeOffset CreatedAtUtc,
    bool IsCanonical);

public interface ICanonicalOrganizationService
{
    /// <summary>
    /// Returns the canonical organization when present; otherwise null.
    /// Throws when more than one canonical row is found.
    /// </summary>
    Task<CanonicalOrganization?> TryGetCanonicalAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the existing canonical organization, or creates it once with <paramref name="name"/>.
    /// Concurrent callers serialize on an application lock and never replace an existing GUID.
    /// </summary>
    Task<CanonicalOrganization> EnsureCanonicalAsync(
        string name,
        CancellationToken cancellationToken = default);
}
