using Microsoft.Extensions.Hosting;
using PhcKnowledge.Host;
using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Receipts;
using PhcKnowledge.Import.Storage;

var builder = Host.CreateApplicationBuilder(args);
var settings = ReceiptHostSettings.Load(builder.Configuration);

Directory.CreateDirectory(settings.StagingDirectory);
Directory.CreateDirectory(settings.ArchiveDirectory);

var attemptRepository = new SqlServerPackageReceiptAttemptRepository(settings.ConnectionString);
var packageStore = new FileSystemImmutablePackageStore(settings.ArchiveDirectory);

builder.Services.AddSingleton(settings);
builder.Services.AddSingleton<IPackageReceiptAttemptRepository>(attemptRepository);
builder.Services.AddSingleton<IImmutablePackageStore>(packageStore);
builder.Services.AddSingleton<IRecoverableImmutablePackageStore>(packageStore);
builder.Services.AddSingleton<IExpectedStorageKeyProvider>(packageStore);
builder.Services.AddSingleton<PackageReceiptRecoveryService>(services =>
    new PackageReceiptRecoveryService(
        services.GetRequiredService<IPackageReceiptAttemptRepository>(),
        services.GetRequiredService<IImmutablePackageStore>(),
        settings.StagingDirectory,
        settings.LeaseDuration));
builder.Services.AddSingleton<PackageStagingCleanupService>(services =>
    new PackageStagingCleanupService(
        services.GetRequiredService<IPackageReceiptAttemptRepository>(),
        settings.StagingDirectory,
        settings.StagingRetention));

builder.Services.AddHostedService<SqlServerSchemaPreflightService>();
builder.Services.AddHostedService<PackageReceiptRecoveryWorker>();
builder.Services.AddHostedService<PackageStagingCleanupWorker>();
builder.Services.AddHostedService<PackageReceiptOperationsMonitor>();

await builder.Build().RunAsync();
