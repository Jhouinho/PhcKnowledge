namespace PhcKnowledge.Host;

/// <summary>
/// Shared readiness flag for health checks. Set by schema preflight after success.
/// </summary>
public sealed class HostReadinessState
{
    private int _schemaReady;

    public bool IsSchemaReady => Volatile.Read(ref _schemaReady) == 1;

    public void MarkSchemaReady() => Volatile.Write(ref _schemaReady, 1);
}
