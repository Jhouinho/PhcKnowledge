using System.Security.Claims;
using PhcKnowledge.Host;

namespace PhcKnowledge.Import.Tests;

public sealed class OrganizationIdResolverTests
{
    private static OrganizationIdResolver Resolver(string claimType = "organization_id") =>
        new(new AuthenticationHostSettings
        {
            Authority = "https://idp.example.test",
            Issuer = "https://idp.example.test",
            Audience = "api",
            OrganizationClaimType = claimType
        });

    [Fact]
    public void ResolveRequired_returns_guid_from_configured_claim()
    {
        var organizationId = Guid.NewGuid();
        var principal = Principal(
            new Claim("organization_id", organizationId.ToString("D")),
            new Claim("scope", "ikp.upload ikp.read"));

        Assert.Equal(organizationId, Resolver().ResolveRequired(principal));
    }

    [Fact]
    public void ResolveRequired_uses_configured_claim_type()
    {
        var organizationId = Guid.NewGuid();
        var principal = Principal(new Claim("org", organizationId.ToString("D")));

        Assert.Equal(organizationId, Resolver("org").ResolveRequired(principal));
    }

    [Fact]
    public void ResolveRequired_rejects_unauthenticated_principal()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var ex = Assert.Throws<OrganizationIdResolutionException>(
            () => Resolver().ResolveRequired(principal));
        Assert.Equal(OrganizationIdResolutionFailure.Unauthenticated, ex.Failure);
    }

    [Fact]
    public void ResolveRequired_rejects_missing_claim()
    {
        var principal = Principal(new Claim("scope", "ikp.read"));

        var ex = Assert.Throws<OrganizationIdResolutionException>(
            () => Resolver().ResolveRequired(principal));
        Assert.Equal(OrganizationIdResolutionFailure.ClaimMissing, ex.Failure);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void ResolveRequired_rejects_invalid_guid(string value)
    {
        var principal = Principal(new Claim("organization_id", value));

        var ex = Assert.Throws<OrganizationIdResolutionException>(
            () => Resolver().ResolveRequired(principal));
        Assert.Equal(
            string.IsNullOrWhiteSpace(value)
                ? OrganizationIdResolutionFailure.ClaimMissing
                : OrganizationIdResolutionFailure.ClaimInvalid,
            ex.Failure);
    }

    [Fact]
    public void ResolveRequired_rejects_multiple_claim_values()
    {
        var principal = Principal(
            new Claim("organization_id", Guid.NewGuid().ToString("D")),
            new Claim("organization_id", Guid.NewGuid().ToString("D")));

        var ex = Assert.Throws<OrganizationIdResolutionException>(
            () => Resolver().ResolveRequired(principal));
        Assert.Equal(OrganizationIdResolutionFailure.ClaimInvalid, ex.Failure);
    }

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "Bearer"));
}
