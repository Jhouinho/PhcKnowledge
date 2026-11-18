using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using PhcKnowledge.Host;

namespace PhcKnowledge.Import.Tests;

public sealed class ScopeAuthorizationHandlerTests
{
    [Fact]
    public async Task Handler_succeeds_when_space_delimited_scope_contains_required_value()
    {
        var handler = new ScopeAuthorizationHandler();
        var requirement = new ScopeAuthorizationRequirement(AuthenticationHostSettings.UploadScope);
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ScopeAuthorizationHandler.ScopeClaimType, "openid ikp.upload offline_access")],
            authenticationType: "Bearer"));
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handler_fails_when_required_scope_is_absent()
    {
        var handler = new ScopeAuthorizationHandler();
        var requirement = new ScopeAuthorizationRequirement(AuthenticationHostSettings.ReadScope);
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ScopeAuthorizationHandler.ScopeClaimType, "ikp.upload")],
            authenticationType: "Bearer"));
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public void HasScope_is_ordinal_and_does_not_treat_substring_as_match()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ScopeAuthorizationHandler.ScopeClaimType, "ikp.upload.extra")],
            authenticationType: "Bearer"));

        Assert.False(ScopeAuthorizationHandler.HasScope(user, "ikp.upload"));
    }
}
