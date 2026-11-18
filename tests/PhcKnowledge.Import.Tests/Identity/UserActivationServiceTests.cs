using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhcKnowledge.Host.Identity;
using PhcKnowledge.Host.Organizations;

namespace PhcKnowledge.Import.Tests.Identity;

/// <summary>
/// In-memory unit coverage for activation short-circuit behaviour.
/// Atomic SQL rollback is covered by SqlServerIdentityPersistenceTests.
/// </summary>
public sealed class UserActivationServiceTests
{
    private static readonly Guid CanonicalId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task SetActiveAsync_noop_when_already_in_desired_state()
    {
        await using var sp = await BuildAsync();
        using var scope = sp.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var activation = scope.ServiceProvider.GetRequiredService<UserActivationService>();
        var factory = scope.ServiceProvider.GetRequiredService<ApplicationUserFactory>();

        var user = await factory.CreateAsync("noop", "noop@example.com");
        Assert.True((await users.CreateAsync(user, "Str0ng!Passw0rd")).Succeeded);
        var stamp = (await users.FindByIdAsync(user.Id.ToString("D")))!.SecurityStamp;

        await activation.SetActiveAsync(user.Id, isActive: true);

        var loaded = await users.FindByIdAsync(user.Id.ToString("D"));
        Assert.NotNull(loaded);
        Assert.True(loaded!.IsActive);
        Assert.Equal(stamp, loaded.SecurityStamp);
    }

    private static async Task<ServiceProvider> BuildAsync()
    {
        var dbName = "activation-" + Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug());
        services.AddSingleton<ICanonicalOrganizationService>(new FakeOrganizations(
            new CanonicalOrganization(CanonicalId, "Empresa", DateTimeOffset.UtcNow, true)));
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
        services
            .AddIdentityCore<ApplicationUser>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = 10;
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireUppercase = true;
                o.Password.RequireNonAlphanumeric = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddUserValidator<CanonicalOrganizationUserValidator>();
        services.AddScoped<ApplicationUserFactory>();
        services.AddScoped<CanonicalActiveAdministratorCounter>();
        services.AddScoped<UserActivationService>();

        var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
        db.Organizations.Add(new OrganizationRow
        {
            OrganizationId = CanonicalId,
            Name = "Empresa",
            CreatedAtUtc = DateTime.UtcNow,
            IsCanonical = true
        });
        await db.SaveChangesAsync();
        return sp;
    }

    private sealed class FakeOrganizations(CanonicalOrganization org) : ICanonicalOrganizationService
    {
        public Task<CanonicalOrganization?> TryGetCanonicalAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<CanonicalOrganization?>(org);

        public Task<CanonicalOrganization> EnsureCanonicalAsync(
            string name, CancellationToken cancellationToken = default) =>
            Task.FromResult(org);
    }
}
