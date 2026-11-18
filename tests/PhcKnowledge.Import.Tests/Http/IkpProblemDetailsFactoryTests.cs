using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using PhcKnowledge.Host.Http;

namespace PhcKnowledge.Import.Tests.Http;

public sealed class IkpProblemDetailsFactoryTests
{
    [Fact]
    public void FromException_client_cancellation_returns_empty_without_logging()
    {
        using var aborted = new CancellationTokenSource();
        aborted.Cancel();

        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "client-abort-trace",
            RequestAborted = aborted.Token
        };
        var logger = new CollectingLogger();

        var result = IkpProblemDetailsFactory.FromException(
            httpContext,
            new OperationCanceledException("client aborted"),
            logger);

        Assert.IsType<EmptyHttpResult>(result);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void FromException_internal_cancellation_returns_500_and_logs_error_with_traceId()
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "internal-cancel-trace"
        };
        // RequestAborted remains non-cancelled: cancellation is not client-driven.
        var logger = new CollectingLogger();
        var exception = new OperationCanceledException("internal timeout");

        var result = IkpProblemDetailsFactory.FromException(httpContext, exception, logger);

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.StatusCode);
        Assert.Equal(
            IkpPublicErrorCodes.InternalError,
            problem.ProblemDetails.Extensions["code"]?.ToString());
        Assert.Null(problem.ProblemDetails.Detail);

        Assert.Single(logger.Entries);
        var entry = logger.Entries[0];
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(exception, entry.Exception);
        Assert.Contains("internal-cancel-trace", entry.Message, StringComparison.Ordinal);
        Assert.Contains("TraceId", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("internal timeout", problem.ProblemDetails.Title ?? string.Empty, StringComparison.Ordinal);
    }

    private sealed class CollectingLogger : ILogger
    {
        public List<(LogLevel Level, Exception? Exception, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, exception, formatter(state, exception)));
        }
    }
}
