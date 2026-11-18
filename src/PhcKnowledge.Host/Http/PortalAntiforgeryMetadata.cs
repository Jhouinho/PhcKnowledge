using Microsoft.AspNetCore.Antiforgery;

namespace PhcKnowledge.Host.Http;

/// <summary>Marks minimal API endpoints that must pass antiforgery validation.</summary>
internal sealed class PortalAntiforgeryMetadata : IAntiforgeryMetadata
{
    public static PortalAntiforgeryMetadata Instance { get; } = new();
    public bool RequiresValidation => true;
}
