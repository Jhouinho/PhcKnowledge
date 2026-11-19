using Microsoft.AspNetCore.Authorization;
using PhcKnowledge.Host;
using PhcKnowledge.Host.Authentication;
using PhcKnowledge.Host.Http;
using PhcKnowledge.Host.Identity;
using PhcKnowledge.Host.Organizations;
using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Ikp.Analysis;
using PhcKnowledge.Import.Receipts;
using PhcKnowledge.Import.Storage;

var builder = WebApplication.CreateBuilder(args);

var receiptSettings = ReceiptHostSettings.Load(builder.Configuration);
var authenticationSettings = AuthenticationHostSettings.Load(builder.Configuration);
var uploadSettings = UploadHostSettings.Load(builder.Configuration);

Directory.CreateDirectory(receiptSettings.StagingDirectory);
Directory.CreateDirectory(receiptSettings.ArchiveDirectory);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = uploadSettings.MaxUploadBytes;
});

var attemptRepository = new SqlServerPackageReceiptAttemptRepository(receiptSettings.ConnectionString);
var packageStore = new FileSystemImmutablePackageStore(receiptSettings.ArchiveDirectory);
var schemaDirectory = Path.Combine(AppContext.BaseDirectory, "Schemas");
var packageInspectionEngine = new IkpPackageInspectionEngine(schemaDirectory);
var packageValidator = new IkpPackageValidator(packageInspectionEngine);
var packageReader = new IkpPackageReader(packageInspectionEngine);

builder.Services.AddSingleton(receiptSettings);
builder.Services.AddSingleton(authenticationSettings);
builder.Services.AddSingleton(uploadSettings);
builder.Services.AddSingleton<HostReadinessState>();
builder.Services.AddSingleton<IPackageReceiptAttemptRepository>(attemptRepository);
builder.Services.AddSingleton<IImmutablePackageStore>(packageStore);
builder.Services.AddSingleton<IRecoverableImmutablePackageStore>(packageStore);
builder.Services.AddSingleton<IExpectedStorageKeyProvider>(packageStore);
builder.Services.AddSingleton(packageInspectionEngine);
builder.Services.AddSingleton(packageValidator);
builder.Services.AddSingleton(packageReader);
builder.Services.AddSingleton<IkpPackageAnalysisBuilder>();
builder.Services.AddSingleton<IPackageImportAnalysisRepository>(
    _ => new SqlServerPackageImportAnalysisRepository(receiptSettings.ConnectionString));
builder.Services.AddSingleton<IkpPackageAnalysisService>();
builder.Services.AddSingleton(services =>
    new IkpPackageIngestionService(
        services.GetRequiredService<IkpPackageValidator>(),
        services.GetRequiredService<IImmutablePackageStore>(),
        receiptSettings.StagingDirectory,
        uploadSettings.MaxUploadBytes,
        services.GetRequiredService<IPackageReceiptAttemptRepository>()));
builder.Services.AddSingleton<IIkpIngestionGateway, IkpIngestionGateway>();
builder.Services.AddSingleton<OrganizationIdResolver>();
builder.Services.AddSingleton<UploadConcurrencyGate>();
builder.Services.AddSingleton<IAuthorizationHandler, ScopeAuthorizationHandler>();
builder.Services.AddSingleton<ICanonicalOrganizationService>(
    _ => new SqlServerCanonicalOrganizationService(receiptSettings.ConnectionString));
builder.Services.AddPhcKnowledgeIdentityPersistence(receiptSettings.ConnectionString);
builder.Services.AddPhcKnowledgeAuthentication(authenticationSettings, builder.Environment);
builder.Services.AddSingleton<PackageReceiptRecoveryService>(services =>
    new PackageReceiptRecoveryService(
        services.GetRequiredService<IPackageReceiptAttemptRepository>(),
        services.GetRequiredService<IImmutablePackageStore>(),
        receiptSettings.StagingDirectory,
        receiptSettings.LeaseDuration));
builder.Services.AddSingleton<PackageStagingCleanupService>(services =>
    new PackageStagingCleanupService(
        services.GetRequiredService<IPackageReceiptAttemptRepository>(),
        receiptSettings.StagingDirectory,
        receiptSettings.StagingRetention));

builder.Services.AddHostedService<SqlServerSchemaPreflightService>();
builder.Services.AddHostedService<PackageReceiptRecoveryWorker>();
builder.Services.AddHostedService<PackageStagingCleanupWorker>();
builder.Services.AddHostedService<PackageReceiptOperationsMonitor>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapIkpApi(jwtEnabled: authenticationSettings.JwtEnabled);
app.MapPortalApi(app.Environment);

await app.RunAsync();

public partial class Program;
