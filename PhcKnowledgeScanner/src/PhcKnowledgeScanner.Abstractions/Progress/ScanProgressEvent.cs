namespace PhcKnowledgeScanner.Abstractions.Progress;

public sealed record ScanProgressEvent(
    string Stage,
    string? CollectorId,
    string? CollectorDisplayName,
    string Message,
    int ObjectsProcessed,
    int SuccessCount,
    int WarningCount,
    int ErrorCount,
    double? PercentComplete,
    TimeSpan Elapsed);
