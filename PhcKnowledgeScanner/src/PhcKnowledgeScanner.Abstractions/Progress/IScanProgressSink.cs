namespace PhcKnowledgeScanner.Abstractions.Progress;

public interface IScanProgressSink
{
    void Report(ScanProgressEvent progress);
}
