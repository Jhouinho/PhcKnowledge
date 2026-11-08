namespace PhcKnowledge.Import.Contracts;

public sealed record IkpPackageValidationResult(
    bool IsValid,
    string? PackageId,
    string? CaptureId,
    int FileCount,
    int ArtifactCount,
    int ObjectCount,
    int OperationCount,
    IReadOnlyList<string> Errors)
{
    public static IkpPackageValidationResult Invalid(params string[] errors) =>
        new(false, null, null, 0, 0, 0, 0, errors);
}
