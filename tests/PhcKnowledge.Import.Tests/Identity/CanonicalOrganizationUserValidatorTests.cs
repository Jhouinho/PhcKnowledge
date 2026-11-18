using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhcKnowledge.Host.Identity;
using PhcKnowledge.Host.Organizations;

namespace PhcKnowledge.Import.Tests.Identity;

public sealed class CanonicalOrganizationUserValidatorTests
{
    private static readonly Guid CanonicalId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public async Task Rejects_empty_organization_id()
    {
        var result = await ValidateAsync(organizationId: Guid.Empty, bind: false, canonicalExists: true);
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "OrganizationIdRequired");
    }

    [Fact]
    public async Task Rejects_non_canonical_organization_id()
    {
        var result = await ValidateAsync(organizationId: Guid.NewGuid(), bind: true, canonicalExists: true);
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "OrganizationIdNotCanonical");
    }

    [Fact]
    public async Task Rejects_when_canonical_organization_missing()
    {
        var result = await ValidateAsync(organizationId: CanonicalId, bind: true, canonicalExists: false);
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "CanonicalOrganizationMissing");
    }

    [Fact]
    public async Task Accepts_canonical_organization_id()
    {
        var result = await ValidateAsync(organizationId: CanonicalId, bind: true, canonicalExists: true);
        Assert.True(result.Succeeded);
    }

    private static async Task<IdentityResult> ValidateAsync(
        Guid organizationId,
        bool bind,
        bool canonicalExists)
    {
        CanonicalOrganization? org = canonicalExists
            ? new CanonicalOrganization(CanonicalId, "Empresa", DateTimeOffset.UtcNow, true)
            : null;

        var services = new ServiceCollection();
        services.AddSingleton<ICanonicalOrganizationService>(new FakeOrganizations(org));
        services.AddDbContext<ApplicationDbContext>(o =>
            o.UseInMemoryDatabase("validator-" + Guid.NewGuid().ToString("N")));
        services.AddScoped<CanonicalOrganizationUserValidator>();
        await using var sp = services.BuildServiceProvider();

        var validator = sp.GetRequiredService<CanonicalOrganizationUserValidator>();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "bob",
            Email = "bob@example.com",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        if (bind)
            user.BindOrganizationOnCreate(organizationId);

        return await validator.ValidateAsync(manager: null!, user);
    }

    private sealed class FakeOrganizations(CanonicalOrganization? org) : ICanonicalOrganizationService
    {
        public Task<CanonicalOrganization?> TryGetCanonicalAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(org);

        public Task<CanonicalOrganization> EnsureCanonicalAsync(
            string name, CancellationToken cancellationToken = default) =>
            Task.FromResult(org ?? throw new InvalidOperationException("missing"));
    }
}
