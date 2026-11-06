using Microsoft.Extensions.Logging;
using PhcKnowledgeScanner.Abstractions.Connection;
using PhcKnowledgeScanner.Collectors.Sql;

namespace PhcKnowledgeScanner.Application.Services;

public sealed class ConnectionAppService : IConnectionAppService
{
    private readonly SqlConnectionTester _tester;
    private readonly ILogger<ConnectionAppService> _logger;

    public ConnectionAppService(SqlConnectionTester tester, ILogger<ConnectionAppService> logger)
    {
        _tester = tester;
        _logger = logger;
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(
        SqlConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("A testar ligação SQL: {Target}", SqlConnectionStringBuilderService.ToLogSafe(request));
        return await _tester.TestAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
