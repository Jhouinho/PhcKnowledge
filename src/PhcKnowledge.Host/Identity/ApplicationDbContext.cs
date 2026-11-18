using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace PhcKnowledge.Host.Identity;

public sealed class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<OrganizationRow> Organizations => Set<OrganizationRow>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<OrganizationRow>(entity =>
        {
            entity.ToTable("Organizations", t => t.ExcludeFromMigrations());
            entity.HasKey(x => x.OrganizationId);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnType("datetime2");
            entity.Property(x => x.IsCanonical).IsRequired();
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.DisplayName).HasMaxLength(200);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x => x.OrganizationId);

            // Inserts set OrganizationId; subsequent UPDATEs never rewrite it.
            entity.Property(x => x.OrganizationId)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            entity.HasOne(x => x.Organization)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });
    }
}
