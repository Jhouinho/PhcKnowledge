using PhcKnowledgeScanner.Abstractions.Workspace;

namespace PhcKnowledgeScanner.Export.ObjectIndex;

/// <summary>
/// Application-facing orchestration for projecting an existing workspace.
/// Workspace access stays in the reader; the actual projection remains pure.
/// </summary>
public sealed class ObjectIndexWorkspaceProjector
{
    private readonly ObjectIndexProjectionInputReader _reader;
    private readonly ObjectIndexProjector _projector;

    public ObjectIndexWorkspaceProjector(
        ObjectIndexProjectionInputReader? reader = null,
        ObjectIndexProjector? projector = null)
    {
        _reader = reader ?? new ObjectIndexProjectionInputReader();
        _projector = projector ?? new ObjectIndexProjector();
    }

    public async Task<ObjectIndexProjectionResult> ProjectAsync(
        IScanWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var input = await _reader.ReadAsync(workspace, cancellationToken).ConfigureAwait(false);
        return _projector.Project(input);
    }
}
