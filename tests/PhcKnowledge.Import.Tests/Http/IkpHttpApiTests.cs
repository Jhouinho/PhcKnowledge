using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhcKnowledge.Host;
using PhcKnowledge.Host.Http;
using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Ikp.Analysis;
using PhcKnowledge.Import.Receipts;
using PhcKnowledge.Import.Storage;

namespace PhcKnowledge.Import.Tests.Http;

public sealed class IkpHttpApiTests : IClassFixture<IkpHttpApiFactory>
{
    private readonly IkpHttpApiFactory _factory;

    public IkpHttpApiTests(IkpHttpApiFactory factory) => _factory = factory;

    private HttpClient CreateClient(
        Guid? organizationId = null,
        string? scopes = "ikp.upload ikp.read",
        bool authenticate = true)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        if (!authenticate)
            return client;

        client.DefaultRequestHeaders.Add(TestAuthHandler.OrganizationHeader,
            (organizationId ?? _factory.OrganizationA).ToString("D"));
        if (scopes is not null)
            client.DefaultRequestHeaders.Add(TestAuthHandler.ScopeHeader, scopes);
        return client;
    }

    [Fact]
    public async Task Upload_with_valid_auth_returns_200_and_public_dto_only()
    {
        _factory.Gateway.Reset();
        _factory.Gateway.OnReceive = (_, org, _) =>
        {
            var status = _factory.Repo.SeedCompleted(org);
            return Task.FromResult(CompletedResult(status));
        };

        var client = CreateClient();
        using var content = new ByteArrayContent([0x50, 0x4B]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");

        var response = await client.PostAsync("/api/v1/ikp/packages", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement;
        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.True(root.TryGetProperty("attemptId", out _));
        Assert.True(root.TryGetProperty("packageId", out _));
        Assert.True(root.TryGetProperty("status", out _));
        Assert.False(root.TryGetProperty("organizationId", out _));
        Assert.False(root.TryGetProperty("storageKey", out _));
        Assert.False(root.TryGetProperty("lastErrorCode", out _));
        Assert.False(root.TryGetProperty("lastErrorMessage", out _));
        Assert.False(root.TryGetProperty("zipSha256", out _));
        Assert.False(root.TryGetProperty("receiptId", out _));
        Assert.False(root.TryGetProperty("stagingKey", out _));
    }

    [Fact]
    public async Task Upload_without_authentication_returns_401()
    {
        var client = CreateClient(authenticate: false);
        using var content = new ByteArrayContent([0x50, 0x4B]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");

        var response = await client.PostAsync("/api/v1/ikp/packages", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Upload_with_insufficient_scope_returns_403()
    {
        var client = CreateClient(scopes: "ikp.read");
        using var content = new ByteArrayContent([0x50, 0x4B]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");

        var response = await client.PostAsync("/api/v1/ikp/packages", content);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Upload_with_invalid_organization_claim_returns_403(string? org)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.ScopeHeader, "ikp.upload");
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "1");
        if (!string.IsNullOrEmpty(org))
            client.DefaultRequestHeaders.Add(TestAuthHandler.OrganizationHeader, org);

        using var content = new ByteArrayContent([0x50, 0x4B]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        var response = await client.PostAsync("/api/v1/ikp/packages", content);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertPublicProblemAsync(response, "forbidden");
    }

    [Fact]
    public async Task Get_endpoints_isolate_organizations()
    {
        var statusA = _factory.Repo.SeedCompleted(_factory.OrganizationA);
        var statusB = _factory.Repo.SeedCompleted(_factory.OrganizationB);

        var clientA = CreateClient(_factory.OrganizationA, scopes: "ikp.read");
        var clientB = CreateClient(_factory.OrganizationB, scopes: "ikp.read");

        Assert.Equal(HttpStatusCode.OK,
            (await clientA.GetAsync($"/api/v1/ikp/attempts/{statusA.AttemptId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await clientA.GetAsync($"/api/v1/ikp/attempts/{statusB.AttemptId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await clientA.GetAsync($"/api/v1/ikp/packages/{statusB.PackageId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await clientB.GetAsync($"/api/v1/ikp/packages/{statusB.PackageId}")).StatusCode);
    }

    [Fact]
    public async Task Upload_invalid_zip_maps_to_422_without_internal_fields()
    {
        _factory.Gateway.OnReceive = (_, _, _) =>
            throw new IkpPackageValidationException("Pacote IKP inválido: path interno");

        var client = CreateClient();
        using var content = new ByteArrayContent([0x00]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        var response = await client.PostAsync("/api/v1/ikp/packages", content);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var json = await AssertPublicProblemAsync(response, "invalid_package");
        Assert.DoesNotContain("path interno", json, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/zipx")]
    [InlineData("not-a-media-type")]
    [InlineData("")]
    public async Task Upload_rejects_invalid_content_types(string contentType)
    {
        var client = CreateClient();
        using var content = new ByteArrayContent([0x50, 0x4B]);
        if (!string.IsNullOrEmpty(contentType))
            content.Headers.TryAddWithoutValidation("Content-Type", contentType);

        var response = await client.PostAsync("/api/v1/ikp/packages", content);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        await AssertPublicProblemAsync(response, "unsupported_media_type");
    }

    [Theory]
    [InlineData("application/zip")]
    [InlineData("APPLICATION/ZIP")]
    [InlineData("application/zip; charset=binary")]
    public async Task Upload_accepts_application_zip_with_optional_parameters(string contentType)
    {
        _factory.Gateway.Reset();
        _factory.Gateway.OnReceive = (_, org, _) =>
        {
            var status = _factory.Repo.SeedCompleted(org);
            return Task.FromResult(CompletedResult(status));
        };

        var client = CreateClient();
        using var content = new ByteArrayContent([0x50, 0x4B]);
        content.Headers.TryAddWithoutValidation("Content-Type", contentType);

        var response = await client.PostAsync("/api/v1/ikp/packages", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Upload_rejects_multipart()
    {
        var client = CreateClient();
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([0x50, 0x4B]), "file", "a.zip");
        var response = await client.PostAsync("/api/v1/ikp/packages", content);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        await AssertPublicProblemAsync(response, "unsupported_media_type");
    }

    [Fact]
    public async Task Upload_with_duplicate_organization_claims_returns_403()
    {
        var org = _factory.OrganizationA.ToString("D");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthenticatedHeader, "1");
        client.DefaultRequestHeaders.Add(TestAuthHandler.ScopeHeader, "ikp.upload");
        client.DefaultRequestHeaders.Add(TestAuthHandler.OrganizationHeader, org);
        client.DefaultRequestHeaders.Add(TestAuthHandler.OrganizationHeader, org);

        using var content = new ByteArrayContent([0x50, 0x4B]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        var response = await client.PostAsync("/api/v1/ikp/packages", content);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertPublicProblemAsync(response, "forbidden");
    }

    [Fact]
    public async Task Get_attempt_repository_failure_returns_500_without_internal_detail()
    {
        var status = _factory.Repo.SeedCompleted(_factory.OrganizationA);
        var client = CreateClient(scopes: "ikp.read");
        _factory.Repo.ReadFault = new InvalidOperationException(
            "SqlException Server=db;Database=secret path=C:\\temp\\x");
        try
        {
            var response = await client.GetAsync($"/api/v1/ikp/attempts/{status.AttemptId}");
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var json = await AssertPublicProblemAsync(response, "internal_error");
            Assert.DoesNotContain("SqlException", json, StringComparison.Ordinal);
            Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("C:\\temp", json, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            _factory.Repo.ReadFault = null;
        }
    }

    [Fact]
    public async Task Get_package_repository_failure_returns_500_without_internal_detail()
    {
        var status = _factory.Repo.SeedCompleted(_factory.OrganizationA);
        var client = CreateClient(scopes: "ikp.read");
        _factory.Repo.ReadFault = new InvalidOperationException("connection string leaked");
        try
        {
            var response = await client.GetAsync($"/api/v1/ikp/packages/{status.PackageId}");
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            await AssertPublicProblemAsync(response, "internal_error");
        }
        finally
        {
            _factory.Repo.ReadFault = null;
        }
    }

    [Fact]
    public async Task Upload_post_ingestion_status_lookup_failure_returns_500()
    {
        _factory.Gateway.OnReceive = (_, org, _) =>
        {
            var status = _factory.Repo.SeedCompleted(org);
            return Task.FromResult(CompletedResult(status));
        };
        var client = CreateClient();
        _factory.Repo.ReadFault = new InvalidOperationException("lookup failed after ingest");
        try
        {
            using var content = new ByteArrayContent([0x50, 0x4B]);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            var response = await client.PostAsync("/api/v1/ikp/packages", content);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            await AssertPublicProblemAsync(response, "internal_error");
        }
        finally
        {
            _factory.Repo.ReadFault = null;
        }
    }

    [Fact]
    public async Task Upload_pending_returns_202_with_statusUrl()
    {
        var attemptId = Guid.NewGuid();
        _factory.Repo.SeedPending(_factory.OrganizationA, attemptId);
        _factory.Gateway.OnReceive = (_, _, _) =>
            throw new PackageReceiptPendingException(attemptId);

        var client = CreateClient();
        using var content = new ByteArrayContent([0x50, 0x4B]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        var response = await client.PostAsync("/api/v1/ikp/packages", content);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<IkpIngestionAcceptedDto>();
        Assert.NotNull(body);
        Assert.Equal(attemptId, body!.AttemptId);
        Assert.Equal($"/api/v1/ikp/attempts/{attemptId:D}", body.StatusUrl);
    }

    [Fact]
    public async Task Upload_identity_conflict_returns_409()
    {
        _factory.Gateway.OnReceive = (_, _, _) =>
            throw new PackageReceiptConflictException(
                "conflict", PackageReceiptConflictKind.IdentityMismatch);

        var client = CreateClient();
        using var content = new ByteArrayContent([0x50, 0x4B]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        var response = await client.PostAsync("/api/v1/ikp/packages", content);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertPublicProblemAsync(response, "package_conflict");
    }

    [Fact]
    public async Task Upload_needs_attention_returns_409_with_attemptId()
    {
        var attemptId = Guid.NewGuid();
        _factory.Gateway.OnReceive = (_, _, _) =>
            throw new PackageReceiptNeedsAttentionException(attemptId);

        var client = CreateClient();
        using var content = new ByteArrayContent([0x50, 0x4B]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        var response = await client.PostAsync("/api/v1/ikp/packages", content);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ingestion_needs_attention", doc.RootElement.GetProperty("code").GetString());
        Assert.Equal(attemptId.ToString(), doc.RootElement.GetProperty("attemptId").GetString());
    }

    [Fact]
    public async Task Upload_internal_inconsistency_returns_500_not_client_conflict()
    {
        _factory.Gateway.OnReceive = (_, _, _) =>
            throw new PackageReceiptConflictException(
                "missing receipt", PackageReceiptConflictKind.InternalInconsistency);

        var client = CreateClient();
        using var content = new ByteArrayContent([0x50, 0x4B]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        var response = await client.PostAsync("/api/v1/ikp/packages", content);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await AssertPublicProblemAsync(response, "internal_error");
    }

    [Fact]
    public async Task Upload_concurrency_releases_on_success_failure_and_cancel()
    {
        var gate = _factory.Services.GetRequiredService<UploadConcurrencyGate>();
        Assert.Equal(1, gate.CurrentCount);

        var hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _factory.Gateway.OnReceive = async (_, _, ct) =>
        {
            await hold.Task.WaitAsync(ct);
            var status = _factory.Repo.SeedCompleted(_factory.OrganizationA);
            return CompletedResult(status);
        };

        var client = CreateClient();
        using var content1 = new ByteArrayContent([0x50, 0x4B]);
        content1.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        var first = client.PostAsync("/api/v1/ikp/packages", content1);

        await WaitUntilAsync(() => gate.CurrentCount == 0);

        using var content2 = new ByteArrayContent([0x50, 0x4B]);
        content2.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        var blocked = await client.PostAsync("/api/v1/ikp/packages", content2);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, blocked.StatusCode);
        Assert.True(blocked.Headers.Contains("Retry-After"));
        await AssertPublicProblemAsync(blocked, "upload_capacity_exceeded");

        hold.SetResult();
        Assert.Equal(HttpStatusCode.OK, (await first).StatusCode);
        Assert.Equal(1, gate.CurrentCount);

        // Failure path releases
        _factory.Gateway.OnReceive = (_, _, _) => throw new IkpPackageValidationException("bad");
        using var content3 = new ByteArrayContent([0x50, 0x4B]);
        content3.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await client.PostAsync("/api/v1/ikp/packages", content3)).StatusCode);
        Assert.Equal(1, gate.CurrentCount);

        // Cancellation path releases
        var cancelHold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _factory.Gateway.OnReceive = async (_, _, ct) =>
        {
            await cancelHold.Task.WaitAsync(ct);
            throw new InvalidOperationException("unreachable");
        };
        using var cts = new CancellationTokenSource();
        using var content4 = new ByteArrayContent([0x50, 0x4B]);
        content4.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        var cancelled = client.PostAsync("/api/v1/ikp/packages", content4, cts.Token);
        await WaitUntilAsync(() => gate.CurrentCount == 0);
        cts.Cancel();
        try { await cancelled; } catch (OperationCanceledException) { }
        await WaitUntilAsync(() => gate.CurrentCount == 1);
    }

    [Fact]
    public async Task Health_live_and_ready()
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        var ready = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        var json = await ready.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Server=", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ArchiveDirectory", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionString", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Idempotent_completed_repeat_returns_200()
    {
        var status = _factory.Repo.SeedCompleted(_factory.OrganizationA);
        _factory.Gateway.OnReceive = (_, _, _) => Task.FromResult(CompletedResult(status));

        var client = CreateClient();
        using var content = new ByteArrayContent([0x50, 0x4B]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        var first = await client.PostAsync("/api/v1/ikp/packages", content);
        using var content2 = new ByteArrayContent([0x50, 0x4B]);
        content2.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        var second = await client.PostAsync("/api/v1/ikp/packages", content2);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task Upload_over_configured_limit_returns_413_without_starting_ingestion()
    {
        await using var factory = new IkpHttp413KestrelFactory();
        var ingestionStarted = 0;
        factory.Gateway.OnReceive = async (stream, organizationId, ct) =>
        {
            _ = organizationId;
            // Kestrel applies MaxRequestBodySize when the body is consumed. A throw here
            // means the upload was rejected before any ingestion work could proceed.
            _ = await stream.ReadAsync(new byte[4096], ct);
            Interlocked.Increment(ref ingestionStarted);
            throw new InvalidOperationException("Ingestion must not complete for oversized uploads.");
        };

        // Touch Services to start the real Kestrel host; use HttpClientHandler (not TestServer).
        _ = factory.Services;
        using var client = new HttpClient { BaseAddress = factory.ClientOptions.BaseAddress };
        client.DefaultRequestHeaders.Add(TestAuthHandler.OrganizationHeader, factory.OrganizationA.ToString("D"));
        client.DefaultRequestHeaders.Add(TestAuthHandler.ScopeHeader, "ikp.upload");

        var oversized = new byte[IkpHttp413KestrelFactory.MaxUploadBytesForTest + 1];
        using var content = new ByteArrayContent(oversized);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");

        var response = await client.PostAsync("/api/v1/ikp/packages", content);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        await AssertPublicProblemAsync(response, "upload_too_large");
        Assert.Equal(0, Volatile.Read(ref ingestionStarted));
    }

    [Fact]
    public async Task Get_package_analysis_returns_404_when_receipt_missing_or_foreign_org()
    {
        var status = _factory.Repo.SeedCompleted(_factory.OrganizationA);
        var clientA = CreateClient(scopes: "ikp.read");
        var clientB = CreateClient(organizationId: _factory.OrganizationB, scopes: "ikp.read");

        Assert.Equal(HttpStatusCode.NotFound,
            (await clientA.GetAsync($"/api/v1/ikp/packages/{Guid.NewGuid():D}/analysis")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await clientB.GetAsync($"/api/v1/ikp/packages/{status.PackageId:D}/analysis")).StatusCode);
    }

    [Fact]
    public async Task Get_package_analysis_is_read_only_404_until_post_persists()
    {
        var status = _factory.Repo.SeedCompleted(_factory.OrganizationA);
        var uploadOnly = CreateClient(scopes: "ikp.upload");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await uploadOnly.GetAsync($"/api/v1/ikp/packages/{status.PackageId:D}/analysis")).StatusCode);

        var client = CreateClient(scopes: "ikp.read");
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/v1/ikp/packages/{status.PackageId:D}/analysis")).StatusCode);

        var post = await client.PostAsync($"/api/v1/ikp/packages/{status.PackageId:D}/analysis", null);
        Assert.Equal(HttpStatusCode.OK, post.StatusCode);
        await using var postStream = await post.Content.ReadAsStreamAsync();
        using var postDoc = await JsonDocument.ParseAsync(postStream);
        Assert.Equal(status.PackageId.ToString(), postDoc.RootElement.GetProperty("packageId").GetString());
        Assert.Equal("Blocked", postDoc.RootElement.GetProperty("readiness").GetProperty("status").GetString());
        Assert.True(Guid.TryParse(postDoc.RootElement.GetProperty("analysisId").GetString(), out var postedAnalysisId));
        Assert.NotEqual(Guid.Empty, postedAnalysisId);

        var get = await client.GetAsync($"/api/v1/ikp/packages/{status.PackageId:D}/analysis");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        await using var getStream = await get.Content.ReadAsStreamAsync();
        using var getDoc = await JsonDocument.ParseAsync(getStream);
        Assert.Equal(status.ReceiptId!.Value.ToString(), getDoc.RootElement.GetProperty("receiptId").GetString());
        Assert.Equal(postedAnalysisId.ToString("D"), getDoc.RootElement.GetProperty("analysisId").GetString());
        Assert.False(getDoc.RootElement.TryGetProperty("organizationId", out _));
        Assert.False(getDoc.RootElement.TryGetProperty("storageKey", out _));

        var postAgain = await client.PostAsync($"/api/v1/ikp/packages/{status.PackageId:D}/analysis", null);
        Assert.Equal(HttpStatusCode.OK, postAgain.StatusCode);
        Assert.Equal(1, _factory.Analyses.Count);
        await using var postAgainStream = await postAgain.Content.ReadAsStreamAsync();
        using var postAgainDoc = await JsonDocument.ParseAsync(postAgainStream);
        Assert.Equal(postedAnalysisId.ToString("D"), postAgainDoc.RootElement.GetProperty("analysisId").GetString());
    }

    private static IkpIngestionResult CompletedResult(PackageIngestionStatus status)
    {
        var validation = new IkpPackageValidationResult(
            status.PackageId, status.CaptureId, "0.1", "prod", "1",
            null, "a".PadLeft(64, 'a'), 10, 1, 1, status.ArtifactCount, status.ObjectCount, 1, 0, "a".PadLeft(64, 'a'));
        var receipt = new PackageReceipt(
            Guid.NewGuid(), status.OrganizationId, status.PackageId, status.CaptureId,
            "0.1", "prod", "1", null, validation.ZipSha256, validation.ZipByteLength,
            status.StorageKey, DateTimeOffset.UtcNow, "{}");
        return new IkpIngestionResult(validation, new ArchivedPackage(status.StorageKey, true), receipt);
    }

    private static async Task<string> AssertPublicProblemAsync(HttpResponseMessage response, string code)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(code, doc.RootElement.GetProperty("code").GetString());
        Assert.True(doc.RootElement.TryGetProperty("traceId", out _));
        Assert.False(doc.RootElement.TryGetProperty("detail", out var detail) &&
                     detail.ValueKind == JsonValueKind.String &&
                     detail.GetString() is { Length: > 0 } text &&
                     (text.Contains('\\') || text.Contains("SqlException", StringComparison.Ordinal)));
        Assert.DoesNotContain("StorageKey", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LastErrorMessage", json, StringComparison.OrdinalIgnoreCase);
        return json;
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 5000)
    {
        var start = Environment.TickCount64;
        while (!condition())
        {
            if (Environment.TickCount64 - start > timeoutMs)
                throw new TimeoutException("Condition not met.");
            await Task.Delay(10);
        }
    }
}

public sealed class IkpHttpApiFactory : WebApplicationFactory<Program>
{
    public Guid OrganizationA { get; } = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public Guid OrganizationB { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public ControllableIngestionGateway Gateway { get; } = new();
    public ControllableAttemptRepository Repo { get; } = new();
    public CountingInMemoryPackageImportAnalysisRepository Analyses { get; } = new();

    private readonly string _archive = Path.Combine(Path.GetTempPath(), "ikp-http-tests", Guid.NewGuid().ToString("N"), "archive");
    private readonly string _staging = Path.Combine(Path.GetTempPath(), "ikp-http-tests", Guid.NewGuid().ToString("N"), "staging");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_archive);
        Directory.CreateDirectory(_staging);

        // Testing: Host skips real JwtBearer; this factory registers TestAuthHandler as Bearer.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Knowledge",
            "Server=(local);Database=PhcKnowledgeHttpTests;Integrated Security=true;TrustServerCertificate=true");
        builder.UseSetting("Storage:ArchiveDirectory", _archive);
        builder.UseSetting("Storage:StagingDirectory", _staging);
        builder.UseSetting("Authentication:Jwt:Enabled", "true");
        builder.UseSetting("Authentication:Authority", "https://idp.example.test");
        builder.UseSetting("Authentication:Audience", "phcknowledge-ikp-api");
        builder.UseSetting("Authentication:RequireHttpsMetadata", "false");
        builder.UseSetting("Upload:MaxConcurrentUploads", "1");
        builder.UseSetting("Upload:MaxUploadBytes", UploadHostSettings.DefaultMaxUploadBytes.ToString());

        builder.ConfigureTestServices(services =>
        {
            foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IHostedService)).ToArray())
                services.Remove(descriptor);

            services.RemoveAll<IIkpIngestionGateway>();
            services.RemoveAll<IPackageReceiptAttemptRepository>();
            services.RemoveAll<IPackageImportAnalysisRepository>();
            services.RemoveAll<IkpPackageAnalysisService>();
            services.RemoveAll<UploadConcurrencyGate>();
            services.RemoveAll<HostReadinessState>();

            var readiness = new HostReadinessState();
            readiness.MarkSchemaReady();
            services.AddSingleton(readiness);
            services.AddSingleton(Gateway);
            services.AddSingleton<IIkpIngestionGateway>(Gateway);
            services.AddSingleton(Repo);
            services.AddSingleton<IPackageReceiptAttemptRepository>(Repo);
            services.AddSingleton<IPackageImportAnalysisRepository>(Analyses);
            services.AddSingleton<IkpPackageAnalysisService>();
            services.AddSingleton(new UploadHostSettings { MaxConcurrentUploads = 1, MaxUploadBytes = UploadHostSettings.DefaultMaxUploadBytes });
            services.AddSingleton<UploadConcurrencyGate>();

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            if (Directory.Exists(_archive)) Directory.Delete(Path.GetDirectoryName(_archive)!, recursive: true);
        }
        catch { /* best effort */ }
    }
}

/// <summary>
/// Real Kestrel host with a tiny MaxRequestBodySize so HTTP 413 can be asserted
/// without uploading hundreds of MiB. TestServer alone does not enforce Kestrel limits.
/// </summary>
public sealed class IkpHttp413KestrelFactory : WebApplicationFactory<Program>
{
    public const long MaxUploadBytesForTest = 64;

    public Guid OrganizationA { get; } = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public ControllableIngestionGateway Gateway { get; } = new();
    public ControllableAttemptRepository Repo { get; } = new();

    private readonly string _archive = Path.Combine(Path.GetTempPath(), "ikp-http-413", Guid.NewGuid().ToString("N"), "archive");
    private readonly string _staging = Path.Combine(Path.GetTempPath(), "ikp-http-413", Guid.NewGuid().ToString("N"), "staging");
    private IHost? _kestrelHost;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_archive);
        Directory.CreateDirectory(_staging);

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Knowledge",
            "Server=(local);Database=PhcKnowledgeHttp413Tests;Integrated Security=true;TrustServerCertificate=true");
        builder.UseSetting("Storage:ArchiveDirectory", _archive);
        builder.UseSetting("Storage:StagingDirectory", _staging);
        builder.UseSetting("Authentication:Jwt:Enabled", "true");
        builder.UseSetting("Authentication:Authority", "https://idp.example.test");
        builder.UseSetting("Authentication:Audience", "phcknowledge-ikp-api");
        builder.UseSetting("Authentication:RequireHttpsMetadata", "false");
        builder.UseSetting("Upload:MaxConcurrentUploads", "1");
        builder.UseSetting("Upload:MaxUploadBytes", MaxUploadBytesForTest.ToString());

        builder.ConfigureTestServices(services =>
        {
            foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IHostedService)).ToArray())
                services.Remove(descriptor);

            services.RemoveAll<IIkpIngestionGateway>();
            services.RemoveAll<IPackageReceiptAttemptRepository>();
            services.RemoveAll<UploadConcurrencyGate>();
            services.RemoveAll<HostReadinessState>();

            var readiness = new HostReadinessState();
            readiness.MarkSchemaReady();
            services.AddSingleton(readiness);
            services.AddSingleton(Gateway);
            services.AddSingleton<IIkpIngestionGateway>(Gateway);
            services.AddSingleton(Repo);
            services.AddSingleton<IPackageReceiptAttemptRepository>(Repo);
            services.AddSingleton(new UploadHostSettings
            {
                MaxConcurrentUploads = 1,
                MaxUploadBytes = MaxUploadBytesForTest
            });
            services.AddSingleton<UploadConcurrencyGate>();

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // DeferredHostBuilder allows a second Build for a real Kestrel listener.
        var testHost = builder.Build();

        builder.ConfigureWebHost(webBuilder =>
        {
            webBuilder.UseKestrel();
            webBuilder.UseUrls("http://127.0.0.1:0");
        });

        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        var addresses = _kestrelHost.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()
            ?.Addresses
            ?? throw new InvalidOperationException("Kestrel did not publish server addresses.");
        ClientOptions.BaseAddress = new Uri(addresses.Last());

        testHost.Start();
        return testHost;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _kestrelHost?.Dispose();
            _kestrelHost = null;
            try
            {
                if (Directory.Exists(_archive))
                    Directory.Delete(Path.GetDirectoryName(_archive)!, recursive: true);
            }
            catch { /* best effort */ }
        }

        base.Dispose(disposing);
    }
}

public sealed class ControllableIngestionGateway : IIkpIngestionGateway
{
    public Func<Stream, Guid, CancellationToken, Task<IkpIngestionResult>> OnReceive { get; set; } =
        (_, _, _) => throw new InvalidOperationException("OnReceive not configured.");

    public void Reset() =>
        OnReceive = (_, _, _) => throw new InvalidOperationException("OnReceive not configured.");

    public Task<IkpIngestionResult> ReceiveAsync(
        Stream upload, Guid organizationId, CancellationToken cancellationToken = default) =>
        OnReceive(upload, organizationId, cancellationToken);
}

public sealed class CountingInMemoryPackageImportAnalysisRepository : IPackageImportAnalysisRepository
{
    private readonly InMemoryPackageImportAnalysisRepository _inner = new();
    public int Count { get; private set; }

    public Task<PersistedPackageImportAnalysis?> GetAsync(
        Guid organizationId, Guid packageId, string zipSha256, string analysisSchemaVersion,
        CancellationToken cancellationToken = default) =>
        _inner.GetAsync(organizationId, packageId, zipSha256, analysisSchemaVersion, cancellationToken);

    public Task<PersistedPackageImportAnalysis?> GetByAnalysisIdAsync(
        Guid organizationId, Guid analysisId, CancellationToken cancellationToken = default) =>
        _inner.GetByAnalysisIdAsync(organizationId, analysisId, cancellationToken);

    public async Task<PersistedPackageImportAnalysis> InsertOrGetExistingAsync(
        PersistedPackageImportAnalysis analysis, CancellationToken cancellationToken = default)
    {
        var existing = await _inner.GetAsync(
            analysis.OrganizationId, analysis.PackageId, analysis.ZipSha256,
            analysis.AnalysisSchemaVersion, cancellationToken);
        var stored = await _inner.InsertOrGetExistingAsync(analysis, cancellationToken);
        if (existing is null)
            Count++;
        return stored;
    }
}

public sealed class ControllableAttemptRepository : IPackageReceiptAttemptRepository
{
    private readonly Dictionary<(Guid Org, Guid Attempt), PackageIngestionStatus> _byAttempt = new();
    private readonly Dictionary<(Guid Org, Guid Package), PackageIngestionStatus> _byPackage = new();

    /// <summary>When set, read methods throw this exception (for HTTP 500 mapping tests).</summary>
    public Exception? ReadFault { get; set; }

    public PackageIngestionStatus SeedCompleted(Guid organizationId)
    {
        var status = new PackageIngestionStatus(
            Guid.NewGuid(), organizationId, Guid.NewGuid(), Guid.NewGuid(),
            "Completed", "a".PadLeft(64, 'a'), 10, "sha256/aa/a.ikp.zip", 1, 2, 1,
            DateTimeOffset.UtcNow.AddMinutes(-2), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            Guid.NewGuid(), null);
        _byAttempt[(organizationId, status.AttemptId)] = status;
        _byPackage[(organizationId, status.PackageId)] = status;
        return status;
    }

    public PackageIngestionStatus SeedPending(Guid organizationId, Guid attemptId)
    {
        var status = new PackageIngestionStatus(
            attemptId, organizationId, Guid.NewGuid(), Guid.NewGuid(),
            "PendingArchive", "b".PadLeft(64, 'b'), 10, "sha256/bb/b.ikp.zip", 0, 0, 0,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, null);
        _byAttempt[(organizationId, attemptId)] = status;
        _byPackage[(organizationId, status.PackageId)] = status;
        return status;
    }

    public Task<PackageIngestionStatus?> GetAttemptAsync(
        Guid organizationId, Guid attemptId, CancellationToken cancellationToken = default)
    {
        if (ReadFault is not null)
            throw ReadFault;
        return Task.FromResult(_byAttempt.TryGetValue((organizationId, attemptId), out var s) ? s : null);
    }

    public Task<PackageIngestionStatus?> GetByPackageIdAsync(
        Guid organizationId, Guid packageId, CancellationToken cancellationToken = default)
    {
        if (ReadFault is not null)
            throw ReadFault;
        return Task.FromResult(_byPackage.TryGetValue((organizationId, packageId), out var s) ? s : null);
    }

    public Task<PackageReceiptAttemptRegistration> CreateOrGetPendingAsync(
        Guid organizationId, IkpPackageValidationResult validation, string storageKey, string stagingKey,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<PackageReceiptAttemptLease?> ClaimByIdAsync(Guid attemptId, TimeSpan leaseDuration, DateTimeOffset nowUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<IReadOnlyList<PackageReceiptAttemptLease>> ClaimDueAsync(int batchSize, TimeSpan leaseDuration, DateTimeOffset nowUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<PackageReceipt?> GetCompletedReceiptAsync(Guid organizationId, Guid attemptId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<PackageReceipt?> GetCompletedReceiptByPackageIdAsync(
        Guid organizationId, Guid packageId, CancellationToken cancellationToken = default)
    {
        if (ReadFault is not null)
            throw ReadFault;
        if (!_byPackage.TryGetValue((organizationId, packageId), out var status) ||
            status.Status != "Completed" ||
            status.ReceiptId is null)
        {
            return Task.FromResult<PackageReceipt?>(null);
        }

        return Task.FromResult<PackageReceipt?>(new PackageReceipt(
            status.ReceiptId.Value, status.OrganizationId, status.PackageId, status.CaptureId,
            "0.1", "test", "1.0", null, status.ZipSha256, status.ZipByteLength, status.StorageKey,
            status.CompletedAtUtc ?? status.UpdatedAtUtc, "{}"));
    }
    public Task<IReadOnlySet<string>> GetProtectedStagingKeysAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> RenewLeaseAsync(Guid attemptId, Guid leaseToken, DateTimeOffset leaseUntilUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<PackageReceiptRegistration> CompleteAsync(Guid attemptId, Guid leaseToken, ArchivedPackage archive, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> ScheduleRetryAsync(Guid attemptId, Guid leaseToken, DateTimeOffset nextAttemptAtUtc, string errorCode, string errorMessage, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> MarkNeedsAttentionAsync(Guid attemptId, Guid leaseToken, string errorCode, string errorMessage, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>Must match IKP policy authentication schemes (JwtBearer).</summary>
    public new const string Scheme = JwtBearerDefaults.AuthenticationScheme;
    public const string OrganizationHeader = "X-Test-OrganizationId";
    public const string ScopeHeader = "X-Test-Scope";
    public const string AuthenticatedHeader = "X-Test-Authenticated";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(OrganizationHeader) &&
            !Request.Headers.ContainsKey(AuthenticatedHeader))
            return Task.FromResult(AuthenticateResult.Fail("No test credentials."));

        var claims = new List<Claim> { new(ClaimTypes.Name, "test-client") };
        foreach (var org in Request.Headers[OrganizationHeader])
        {
            if (!string.IsNullOrWhiteSpace(org))
                claims.Add(new Claim("organization_id", org));
        }

        if (Request.Headers.TryGetValue(ScopeHeader, out var scopes) &&
            !string.IsNullOrWhiteSpace(scopes))
            claims.Add(new Claim(ScopeAuthorizationHandler.ScopeClaimType, scopes.ToString()));

        var identity = new ClaimsIdentity(claims, Scheme);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme)));
    }
}
