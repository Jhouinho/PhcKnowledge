using PhcKnowledge.Host.Identity;
using PhcKnowledge.Host.Organizations;

namespace PhcKnowledge.Import.Tests.Identity;

public sealed class ApplicationUserFactoryTests
{
    [Fact]
    public async Task CreateAsync_binds_canonical_organization_and_defaults()
    {
        var orgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var orgs = new FakeOrganizations(new CanonicalOrganization(
            orgId, "Empresa", DateTimeOffset.UtcNow, true));
        var factory = new ApplicationUserFactory(orgs);

        var user = await factory.CreateAsync("alice", "alice@example.com", "Alice");

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(orgId, user.OrganizationId);
        Assert.True(user.IsActive);
        Assert.Equal("Alice", user.DisplayName);
        Assert.Equal("alice", user.UserName);
        Assert.Equal("alice@example.com", user.Email);
        Assert.True(user.CreatedAtUtc <= DateTimeOffset.UtcNow.AddMinutes(1));
        Assert.False(string.IsNullOrWhiteSpace(user.SecurityStamp));
    }

    [Fact]
    public async Task CreateAsync_throws_when_canonical_organization_missing()
    {
        var factory = new ApplicationUserFactory(new FakeOrganizations(null));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => factory.CreateAsync("alice", "alice@example.com"));
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
