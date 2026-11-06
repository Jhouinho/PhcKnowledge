using PhcKnowledgeScanner.Abstractions.Connection;

namespace PhcKnowledgeScanner.Application.Services;

public interface IConnectionAppService
{
    Task<ConnectionTestResult> TestConnectionAsync(SqlConnectionRequest request, CancellationToken cancellationToken = default);
}
