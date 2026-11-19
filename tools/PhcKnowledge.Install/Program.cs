using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using PhcKnowledge.Host.Identity;
using PhcKnowledge.Host.Install;
using PhcKnowledge.Host.Organizations;
using PhcKnowledge.Install;

var configOption = new Option<string?>("--config")
{
    Description = "Caminho para pasta ou ficheiro JSON de configuração (ConnectionStrings:Knowledge)."
};

var root = new RootCommand("PhcKnowledge.Install — bootstrap de instalação (organização, roles, admin)");
root.Options.Add(configOption);

root.Subcommands.Add(BuildApplySqlScripts(configOption));
root.Subcommands.Add(BuildMigrateIdentity(configOption));
root.Subcommands.Add(BuildEnsureOrganization(configOption));
root.Subcommands.Add(BuildEnsureRoles(configOption));
root.Subcommands.Add(BuildBootstrapAdmin(configOption));

return await root.Parse(args).InvokeAsync();

static Command BuildApplySqlScripts(Option<string?> configOption)
{
    var cmd = new Command("apply-sql-scripts", "Aplica scripts SQL 001–005 (pré-requisito de schema).");
    cmd.Options.Add(configOption);
    cmd.SetAction(async (parseResult, ct) =>
    {
        await using var sp = CreateServices(parseResult.GetValue(configOption));
        var applicator = sp.GetRequiredService<SqlScriptApplicator>();
        await applicator.ApplyAsync(ct);
        Console.WriteLine("Scripts SQL 001–005 aplicados.");
    });
    return cmd;
}

static Command BuildMigrateIdentity(Option<string?> configOption)
{
    var cmd = new Command("migrate-identity", "Aplica migrations EF Core Identity (IIdentityMigrator).");
    cmd.Options.Add(configOption);
    cmd.SetAction(async (parseResult, ct) =>
    {
        await using var sp = CreateServices(parseResult.GetValue(configOption));
        using var scope = sp.CreateScope();
        var migrator = scope.ServiceProvider.GetRequiredService<IIdentityMigrator>();
        await migrator.MigrateAsync(ct);
        Console.WriteLine("Migrations Identity aplicadas.");
    });
    return cmd;
}

static Command BuildEnsureOrganization(Option<string?> configOption)
{
    var nameOption = new Option<string>("--name")
    {
        Description = "Nome da organização canónica (usado apenas na primeira criação).",
        Required = true
    };

    var cmd = new Command("ensure-organization", "Garante a organização canónica (idempotente; preserva GUID existente).");
    cmd.Options.Add(configOption);
    cmd.Options.Add(nameOption);
    cmd.SetAction(async (parseResult, ct) =>
    {
        var name = parseResult.GetValue(nameOption)!;
        await using var sp = CreateServices(parseResult.GetValue(configOption));
        var orgs = sp.GetRequiredService<ICanonicalOrganizationService>();
        var before = await orgs.TryGetCanonicalAsync(ct);
        var org = await orgs.EnsureCanonicalAsync(name, ct);
        if (before is null)
            Console.WriteLine($"Organização canónica criada: {org.OrganizationId} ({org.Name}).");
        else
            Console.WriteLine(
                $"Organização canónica existente preservada: {org.OrganizationId} ({org.Name}).");
    });
    return cmd;
}

static Command BuildEnsureRoles(Option<string?> configOption)
{
    var cmd = new Command("ensure-roles", "Garante roles Administrator e Collaborator (idempotente).");
    cmd.Options.Add(configOption);
    cmd.SetAction(async (parseResult, ct) =>
    {
        await using var sp = CreateServices(parseResult.GetValue(configOption));
        using var scope = sp.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IdentityRoleSeeder>();
        await seeder.EnsureRolesAsync(ct);
        Console.WriteLine("Roles Administrator e Collaborator garantidas.");
    });
    return cmd;
}

static Command BuildBootstrapAdmin(Option<string?> configOption)
{
    var emailOption = new Option<string>("--email")
    {
        Description = "Email do primeiro administrador.",
        Required = true
    };
    var displayOption = new Option<string?>("--display-name")
    {
        Description = "Nome de apresentação opcional."
    };

    var cmd = new Command(
        "bootstrap-admin",
        "Cria o primeiro administrador (password pedida interativamente; nunca via argumentos).");
    cmd.Options.Add(configOption);
    cmd.Options.Add(emailOption);
    cmd.Options.Add(displayOption);
    cmd.SetAction(async (parseResult, ct) =>
    {
        var email = parseResult.GetValue(emailOption)!;
        var displayName = parseResult.GetValue(displayOption);
        var password = ConsolePasswordReader.ReadConfirmedPassword();

        await using var sp = CreateServices(parseResult.GetValue(configOption));
        using var scope = sp.CreateScope();
        var bootstrap = scope.ServiceProvider.GetRequiredService<FirstAdministratorBootstrapService>();
        try
        {
            var result = await bootstrap.BootstrapAsync(email, password, displayName, ct);
            Console.WriteLine($"Administrador criado: {result.Email} (UserId={result.UserId}).");
        }
        catch (BootstrapAdminRejectedException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Environment.ExitCode = 2;
        }
    });
    return cmd;
}

static ServiceProvider CreateServices(string? configPath)
{
    var configuration = InstallConfiguration.Build(configPath);
    var connectionString = InstallConfiguration.RequireConnectionString(configuration);
    return InstallServiceProvider.Create(connectionString);
}
