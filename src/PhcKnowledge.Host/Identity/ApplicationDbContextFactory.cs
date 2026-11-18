using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PhcKnowledge.Host.Identity;

/// <summary>
/// Design-time factory for <c>dotnet ef migrations</c>. Uses a placeholder
/// connection string; runtime uses ConnectionStrings:Knowledge.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(
                "Server=(local);Database=PhcKnowledgeDesignTime;Integrated Security=true;TrustServerCertificate=true")
            .Options;
        return new ApplicationDbContext(options);
    }
}
