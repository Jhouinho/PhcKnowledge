using PhcKnowledge.Host.Http;

namespace PhcKnowledge.Import.Tests.Http;

public sealed class IkpContentTypeTests
{
    [Theory]
    [InlineData("application/zip")]
    [InlineData("APPLICATION/ZIP")]
    [InlineData("application/zip; charset=binary")]
    [InlineData("application/zip;charset=utf-8")]
    public void IsApplicationZipContentType_accepts_zip_and_parameters(string contentType) =>
        Assert.True(IkpApiEndpoints.IsApplicationZipContentType(contentType));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("application/json")]
    [InlineData("application/zipx")]
    [InlineData("multipart/form-data")]
    [InlineData("multipart/form-data; boundary=abc")]
    [InlineData("not-a-media-type")]
    [InlineData("application/zipx; charset=binary")]
    public void IsApplicationZipContentType_rejects_non_zip_and_malformed(string? contentType) =>
        Assert.False(IkpApiEndpoints.IsApplicationZipContentType(contentType));
}
