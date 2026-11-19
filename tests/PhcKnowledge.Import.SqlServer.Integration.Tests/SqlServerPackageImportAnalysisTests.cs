using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Ikp.Analysis;
using PhcKnowledge.Import.Receipts;
using PhcKnowledge.Import.Storage;

namespace PhcKnowledge.Import.SqlServer.Integration.Tests;

[Collection(SqlServerIntegrationCollection.Name)]
public sealed class SqlServerPackageImportAnalysisTests
{
    private static string RequireConnectionString()
    {
        var value = Environment.GetEnvironmentVariable("PHC_KNOWLEDGE_SQL_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("PHC_KNOWLEDGE_SQL_TEST_CONNECTION_STRING não está definida.");
        var builder = new SqlConnectionStringBuilder(value);
        if (!string.Equals(builder.InitialCatalog, "PhcKnowledgeReceiptTests", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Os testes só podem executar na base de dados isolada PhcKnowledgeReceiptTests.");
        return value;
    }

    private static async Task EnsureDatabaseAsync(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        await using var command = new SqlCommand("""
            IF DB_ID(@DatabaseName) IS NULL
            BEGIN
                DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@DatabaseName);
                EXEC sys.sp_executesql @sql;
            END;
            """, connection);
        command.Parameters.AddWithValue("@DatabaseName", databaseName);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task EnsureSchemaAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (var migration in new[]
                 {
                     "001_package_ingestion_receipts.sql",
                     "002_package_receipt_attempts.sql",
                     "003_organizations.sql",
                     "004_package_import_analyses.sql"
                 })
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Migrations", migration);
            var sql = await File.ReadAllTextAsync(path);
            foreach (var batch in Regex.Split(sql, @"(?im)^\s*GO\s*(?:--.*)?$")
                         .Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                await using var command = new SqlCommand(batch, connection);
                await command.ExecuteNonQueryAsync();
            }
        }
    }

    private async Task<string> SetupAsync()
    {
        var connectionString = RequireConnectionString();
        await EnsureDatabaseAsync(connectionString);
        await EnsureSchemaAsync(connectionString);
        return connectionString;
    }

    private static async Task CleanupAsync(string connectionString, params Guid[] organizationIds)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (var organizationId in organizationIds)
        {
            await using var command = new SqlCommand("""
                DELETE FROM dbo.PackageImportAnalyses WHERE OrganizationId = @OrganizationId;
                DELETE FROM dbo.PackageIngestionReceipts WHERE OrganizationId = @OrganizationId;
                DELETE FROM dbo.PackageReceiptAttempts WHERE OrganizationId = @OrganizationId;
                """, connection);
            command.Parameters.AddWithValue("@OrganizationId", organizationId);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static PackageImportAnalysis SampleAnalysis(
        Guid organizationId, Guid receiptId, Guid packageId, string zipSha256)
    {
        var unavailable = new PackageImportUnavailableObject(
            "proc:1", "stored_procedure", "Unavailable", "sql.server.structure.procedures",
            "P1", null, null, null);
        var issue = new IkpInspectedIssue(
            "issue:1", "sql.server.structure.procedures", "Warning",
            "sql.procedure.definition_missing", "missing def", null, null);

        return new PackageImportAnalysis(
            organizationId, receiptId, packageId, Guid.NewGuid(), "0.1",
            "PhcKnowledgeScanner", "test", "DB", "sqlserver", null, null, "Full", "Complete", "1.0",
            zipSha256, zipSha256, PackageImportAnalysisSchema.Version,
            DateTimeOffset.UtcNow,
            new PackageImportIntegrityAnalysis(true, true, true, Array.Empty<string>()),
            Array.Empty<IkpInspectedCapabilityOperation>(),
            Array.Empty<IkpInspectedCoverageOperation>(),
            new PackageImportObjectIndexAnalysis(
                1, new SortedDictionary<string, int> { ["stored_procedure"] = 1 },
                new SortedDictionary<string, int> { ["Unavailable"] = 1 },
                0, 1, 0, 0, 1, 0, 0, 0, 0),
            new PackageImportArtifactsAnalysis(
                0, true, Array.Empty<IkpInspectedArtifact>(),
                new SortedDictionary<string, int>()),
            new PackageImportRelationsAnalysis(
                false, 0, 0, 0, new SortedDictionary<string, int>()),
            [issue],
            Array.Empty<string>(),
            new PackageImportReadinessAnalysis(
                MaterializationReadiness.ReadyWithWarnings,
                [
                    new PackageImportReadinessFinding(
                        "analysis.warning.issue", MaterializationFindingSeverity.Warning,
                        "missing def", "issue:1")
                ]),
            [unavailable]);
    }

    [SqlServerFact]
    public async Task Insert_get_is_idempotent_and_org_isolated()
    {
        var connectionString = await SetupAsync();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        try
        {
            var validation = NewValidation();
            var attempts = new SqlServerPackageReceiptAttemptRepository(connectionString);
            var storeKey = $"sha256/{validation.ZipSha256[..2]}/{validation.ZipSha256}.ikp.zip";
            var registration = await attempts.CreateOrGetPendingAsync(
                orgA, validation, storeKey, Guid.NewGuid().ToString("N"));
            var lease = await attempts.ClaimByIdAsync(
                registration.Attempt.AttemptId, TimeSpan.FromMinutes(1), DateTimeOffset.UtcNow);
            Assert.NotNull(lease);
            var completed = await attempts.CompleteAsync(
                lease!.Attempt.AttemptId, lease.LeaseToken, new ArchivedPackage(storeKey, false));
            var receipt = completed.Receipt;

            var repo = new SqlServerPackageImportAnalysisRepository(connectionString);
            var analysis = SampleAnalysis(orgA, receipt.ReceiptId, receipt.PackageId, receipt.ZipSha256);
            var first = await repo.InsertOrGetExistingAsync(
                PersistedPackageImportAnalysis.FromAnalysis(Guid.NewGuid(), analysis, receipt.ZipByteLength, DateTimeOffset.UtcNow));
            var second = await repo.InsertOrGetExistingAsync(
                PersistedPackageImportAnalysis.FromAnalysis(Guid.NewGuid(), analysis with
                {
                    AnalyzedAtUtc = DateTimeOffset.UtcNow.AddHours(1)
                }, receipt.ZipByteLength, DateTimeOffset.UtcNow));

            Assert.Equal(first.AnalysisId, second.AnalysisId);
            var loaded = await repo.GetAsync(
                orgA, receipt.PackageId, receipt.ZipSha256, PackageImportAnalysisSchema.Version);
            Assert.NotNull(loaded);
            Assert.Equal(first.SemanticFingerprint, loaded!.SemanticFingerprint);
            Assert.Null(await repo.GetAsync(
                orgB, receipt.PackageId, receipt.ZipSha256, PackageImportAnalysisSchema.Version));

            var roundtrip = loaded.ToAnalysis();
            Assert.Single(roundtrip.UnavailableObjects);
            Assert.Null(roundtrip.Issues[0].SourceObjectId);
        }
        finally
        {
            await CleanupAsync(connectionString, orgA, orgB);
        }
    }

    [SqlServerFact]
    public async Task Concurrent_inserts_yield_single_row()
    {
        var connectionString = await SetupAsync();
        var org = Guid.NewGuid();
        try
        {
            var validation = NewValidation();
            var attempts = new SqlServerPackageReceiptAttemptRepository(connectionString);
            var storeKey = $"sha256/{validation.ZipSha256[..2]}/{validation.ZipSha256}.ikp.zip";
            var registration = await attempts.CreateOrGetPendingAsync(
                org, validation, storeKey, Guid.NewGuid().ToString("N"));
            var lease = await attempts.ClaimByIdAsync(
                registration.Attempt.AttemptId, TimeSpan.FromMinutes(1), DateTimeOffset.UtcNow);
            var completed = await attempts.CompleteAsync(
                lease!.Attempt.AttemptId, lease.LeaseToken, new ArchivedPackage(storeKey, false));
            var receipt = completed.Receipt;
            var analysis = SampleAnalysis(org, receipt.ReceiptId, receipt.PackageId, receipt.ZipSha256);

            var repo = new SqlServerPackageImportAnalysisRepository(connectionString);
            var tasks = Enumerable.Range(0, 8).Select(_ =>
                repo.InsertOrGetExistingAsync(
                    PersistedPackageImportAnalysis.FromAnalysis(
                        Guid.NewGuid(), analysis, receipt.ZipByteLength, DateTimeOffset.UtcNow)));
            var results = await Task.WhenAll(tasks);
            Assert.True(results.Select(x => x.AnalysisId).Distinct().Count() == 1);

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using var count = new SqlCommand("""
                SELECT COUNT(1) FROM dbo.PackageImportAnalyses
                WHERE OrganizationId = @OrganizationId AND PackageId = @PackageId;
                """, connection);
            count.Parameters.AddWithValue("@OrganizationId", org);
            count.Parameters.AddWithValue("@PackageId", receipt.PackageId);
            Assert.Equal(1, (int)(await count.ExecuteScalarAsync())!);
        }
        finally
        {
            await CleanupAsync(connectionString, org);
        }
    }

    [SqlServerFact]
    public async Task Different_schema_version_creates_separate_row()
    {
        var connectionString = await SetupAsync();
        var org = Guid.NewGuid();
        try
        {
            var validation = NewValidation();
            var attempts = new SqlServerPackageReceiptAttemptRepository(connectionString);
            var storeKey = $"sha256/{validation.ZipSha256[..2]}/{validation.ZipSha256}.ikp.zip";
            var registration = await attempts.CreateOrGetPendingAsync(
                org, validation, storeKey, Guid.NewGuid().ToString("N"));
            var lease = await attempts.ClaimByIdAsync(
                registration.Attempt.AttemptId, TimeSpan.FromMinutes(1), DateTimeOffset.UtcNow);
            var completed = await attempts.CompleteAsync(
                lease!.Attempt.AttemptId, lease.LeaseToken, new ArchivedPackage(storeKey, false));
            var receipt = completed.Receipt;
            var analysis = SampleAnalysis(org, receipt.ReceiptId, receipt.PackageId, receipt.ZipSha256);
            var repo = new SqlServerPackageImportAnalysisRepository(connectionString);

            var vCurrent = await repo.InsertOrGetExistingAsync(
                PersistedPackageImportAnalysis.FromAnalysis(
                    Guid.NewGuid(), analysis, receipt.ZipByteLength, DateTimeOffset.UtcNow));

            var legacyJson = PackageImportAnalysisSerialization.Serialize(analysis);
            var legacy = new PersistedPackageImportAnalysis(
                Guid.NewGuid(), org, receipt.ReceiptId, receipt.PackageId, receipt.CaptureId,
                receipt.ZipSha256, receipt.ZipByteLength, receipt.ZipSha256, "0.1", "0.1",
                "Ready", "c".PadLeft(64, 'c'), legacyJson, 1, 0, 1, 1,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            var vLegacy = await repo.InsertOrGetExistingAsync(legacy);

            Assert.NotEqual(vCurrent.AnalysisId, vLegacy.AnalysisId);
            Assert.NotNull(await repo.GetAsync(org, receipt.PackageId, receipt.ZipSha256, "0.1"));
            Assert.NotNull(await repo.GetAsync(
                org, receipt.PackageId, receipt.ZipSha256, PackageImportAnalysisSchema.Version));
        }
        finally
        {
            await CleanupAsync(connectionString, org);
        }
    }

    private static IkpPackageValidationResult NewValidation()
    {
        var hash = Convert.ToHexString(SHA256.HashData(Guid.NewGuid().ToByteArray())).ToLowerInvariant();
        return new IkpPackageValidationResult(
            Guid.NewGuid(), Guid.NewGuid(), "0.1", "PhcKnowledgeScanner", "test",
            "AnalysisTests", hash, 2048, 1, 1, 1, 1, 1, 0, hash);
    }
}
