using PhcKnowledge.Import.Receipts;

namespace PhcKnowledge.Import.Tests;

public sealed class PackageIngestionStatusTests
{
    [Fact]
    public void Read_model_exposes_stable_fields_without_internal_error_text()
    {
        var type = typeof(PackageIngestionStatus);
        Assert.NotNull(type.GetProperty(nameof(PackageIngestionStatus.AttemptId)));
        Assert.NotNull(type.GetProperty(nameof(PackageIngestionStatus.OrganizationId)));
        Assert.NotNull(type.GetProperty(nameof(PackageIngestionStatus.PackageId)));
        Assert.NotNull(type.GetProperty(nameof(PackageIngestionStatus.Status)));
        Assert.NotNull(type.GetProperty(nameof(PackageIngestionStatus.LastErrorCode)));
        Assert.NotNull(type.GetProperty(nameof(PackageIngestionStatus.ReceiptId)));
        Assert.NotNull(type.GetProperty(nameof(PackageIngestionStatus.ObjectCount)));
        Assert.NotNull(type.GetProperty(nameof(PackageIngestionStatus.ArtifactCount)));

        Assert.Null(type.GetProperty("LastErrorMessage"));
        Assert.Null(type.GetProperty("StagingKey"));
        Assert.Null(type.GetProperty("ValidationJson"));
        Assert.Null(type.GetProperty("LeaseToken"));
        Assert.Null(type.GetProperty("LeaseUntilUtc"));
    }
}
