using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using PhcKnowledge.Host;
using PhcKnowledge.Host.Http;
using PhcKnowledge.Import.Ikp;
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
var packageValidator = new IkpPackageValidator(schemaDirectory);

builder.Services.AddSingleton(receiptSettings);
builder.Services.AddSingleton(authenticationSettings);
builder.Services.AddSingleton(uploadSettings);
builder.Services.AddSingleton<HostReadinessState>();
builder.Services.AddSingleton<IPackageReceiptAttemptRepository>(attemptRepository);
builder.Services.AddSingleton<IImmutablePackageStore>(packageStore);
builder.Services.AddSingleton<IRecoverableImmutablePackageStore>(packageStore);
builder.Services.AddSingleton<IExpectedStorageKeyProvider>(packageStore);
builder.Services.AddSingleton(packageValidator);
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

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authenticationSettings.Authority;
        options.Audience = authenticationSettings.Audience;
        options.RequireHttpsMetadata = authenticationSettings.RequireHttpsMetadata;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authenticationSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = authenticationSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        IkpAuthorizationPolicies.Upload,
        policy => policy
            .RequireAuthenticatedUser()
            .AddRequirements(new ScopeAuthorizationRequirement(AuthenticationHostSettings.UploadScope)));
    options.AddPolicy(
        IkpAuthorizationPolicies.Read,
        policy => policy
            .RequireAuthenticatedUser()
            .AddRequirements(new ScopeAuthorizationRequirement(AuthenticationHostSettings.ReadScope)));
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapIkpApi();

await app.RunAsync();

public partial class Program;
