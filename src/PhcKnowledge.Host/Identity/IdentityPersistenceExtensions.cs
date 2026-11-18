using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace PhcKnowledge.Host.Identity;

public static class IdentityPersistenceExtensions
{
    /// <summary>
    /// Registers EF Core Identity stores and related services without enabling
    /// cookie authentication or changing the P2.B JWT scheme.
    /// Migrations are not applied automatically.
    /// </summary>
    public static IServiceCollection AddPhcKnowledgeIdentityPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // Required by Identity token providers; does not enable cookie auth.
        services.AddDataProtection();
        services.AddHttpContextAccessor();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 10;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager<PortalSignInManager>()
            .AddDefaultTokenProviders()
            .AddUserValidator<CanonicalOrganizationUserValidator>();

        services.AddScoped<ApplicationUserFactory>();
        services.AddScoped<IdentityRoleSeeder>();
        services.AddScoped<CanonicalActiveAdministratorCounter>();
        services.AddScoped<UserActivationService>();
        services.AddScoped<IAdministratorRoleAssigner, IdentityAdministratorRoleAssigner>();
        services.AddScoped<FirstAdministratorBootstrapService>();
        services.AddScoped<IPortalUserRoleAssigner, IdentityPortalUserRoleAssigner>();
        services.AddScoped<IPortalUserSecurityStampRotator, IdentityPortalUserSecurityStampRotator>();
        services.AddScoped<IdentityAdminInvariantGuard>();
        services.AddScoped<PortalUserAdministrationService>();
        services.AddScoped<IPortalUserSessionLookup, EfPortalUserSessionLookup>();
        services.AddScoped<IPortalCookiePrincipalValidator, PortalCookiePrincipalValidator>();
        services.AddSingleton<PortalCookieValidationDiagnostics>();
        services.AddScoped<IIdentityMigrator, EfIdentityMigrator>();

        return services;
    }
}

/// <summary>Explicit migration applicator for future CLI / install tooling.</summary>
public interface IIdentityMigrator
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
}

public sealed class EfIdentityMigrator(ApplicationDbContext dbContext) : IIdentityMigrator
{
    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        dbContext.Database.MigrateAsync(cancellationToken);
}
