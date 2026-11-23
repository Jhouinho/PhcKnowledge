using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using PhcKnowledge.Import.Ikp;
using PhcKnowledge.Import.Ikp.Analysis;
using PhcKnowledge.Import.Receipts;
using PhcKnowledge.Import.Storage;
namespace PhcKnowledge.Import.SqlServer.Integration.Tests;

[Collection(SqlServerIntegrationCollection.Name)]
public sealed class SqlServerPackageImportAnalysisDecisionTests
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
                     "004_package_import_analyses.sql",
                     "005_package_import_analysis_decisions.sql"
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
                IF OBJECT_ID(N'dbo.PackageImportAnalysisDecisions', N'U') IS NOT NULL
                    DELETE FROM dbo.PackageImportAnalysisDecisions WHERE OrganizationId = @OrganizationId;
                DELETE FROM dbo.PackageImportAnalyses WHERE OrganizationId = @OrganizationId;
                DELETE FROM dbo.PackageIngestionReceipts WHERE OrganizationId = @OrganizationId;
                DELETE FROM dbo.PackageReceiptAttempts WHERE OrganizationId = @OrganizationId;
                """, connection);
            command.Parameters.AddWithValue("@OrganizationId", organizationId);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static async Task<PersistedPackageImportAnalysis> SeedAnalysisAsync(
        string connectionString, Guid organizationId, MaterializationReadiness readiness)
    {
        var validation = NewValidation();
        var attempts = new SqlServerPackageReceiptAttemptRepository(connectionString);
        var storeKey = $"sha256/{validation.ZipSha256[..2]}/{validation.ZipSha256}.ikp.zip";
        var registration = await attempts.CreateOrGetPendingAsync(
            organizationId, validation, storeKey, Guid.NewGuid().ToString("N"));
        var lease = await attempts.ClaimByIdAsync(
            registration.Attempt.AttemptId, TimeSpan.FromMinutes(1), DateTimeOffset.UtcNow);
        Assert.NotNull(lease);
        var completed = await attempts.CompleteAsync(
            lease!.Attempt.AttemptId, lease.LeaseToken, new ArchivedPackage(storeKey, false));
        var receipt = completed.Receipt;

        var analysis = SampleAnalysis(
            organizationId, receipt.ReceiptId, receipt.PackageId, receipt.ZipSha256, readiness);
        var repo = new SqlServerPackageImportAnalysisRepository(connectionString);
        return await repo.InsertOrGetExistingAsync(
            PersistedPackageImportAnalysis.FromAnalysis(
                Guid.NewGuid(), analysis, receipt.ZipByteLength, DateTimeOffset.UtcNow));
    }

    [SqlServerFact]
    public async Task First_decision_gets_sequence_1_and_is_effective()
    {
        var cs = await SetupAsync();
        var org = Guid.NewGuid();
        try
        {
            var persisted = await SeedAnalysisAsync(cs, org, MaterializationReadiness.Ready);
            var service = CreateService(cs);
            var actor = new PackageImportAnalysisDecisionActor(Guid.NewGuid(), org);

            var result = await service.DecideAsync(
                persisted, PackageImportAnalysisDecisionKind.Confirmed, null, actor);

            Assert.False(result.IdempotentReplay);
            Assert.Equal(1, result.Decision.DecisionSequence);
            var effective = await service.GetEffectiveAsync(persisted.AnalysisId, org);
            Assert.NotNull(effective);
            Assert.Equal(result.Decision.DecisionId, effective!.DecisionId);
            Assert.Equal(persisted.SemanticFingerprint, effective.SemanticFingerprintSnapshot);
        }
        finally
        {
            await CleanupAsync(cs, org);
        }
    }

    [SqlServerFact]
    public async Task Sequence_increments_and_history_is_descending()
    {
        var cs = await SetupAsync();
        var org = Guid.NewGuid();
        try
        {
            var persisted = await SeedAnalysisAsync(cs, org, MaterializationReadiness.ReadyWithWarnings);
            var service = CreateService(cs);
            var actor = new PackageImportAnalysisDecisionActor(Guid.NewGuid(), org);

            await service.DecideAsync(persisted, PackageImportAnalysisDecisionKind.Confirmed, null, actor);
            await service.DecideAsync(
                persisted, PackageImportAnalysisDecisionKind.Rejected, "reavaliado", actor);

            var history = await service.ListHistoryAsync(persisted.AnalysisId, org);
            Assert.Equal(2, history.Count);
            Assert.Equal(new[] { 2, 1 }, history.Select(x => x.DecisionSequence).ToArray());
            Assert.Equal(PackageImportAnalysisDecisionKind.Rejected, history[0].Decision);

            var effective = await service.GetEffectiveAsync(persisted.AnalysisId, org);
            Assert.Equal(2, effective!.DecisionSequence);
        }
        finally
        {
            await CleanupAsync(cs, org);
        }
    }

    [SqlServerFact]
    public async Task GetByAnalysisId_requires_organization_isolation()
    {
        var cs = await SetupAsync();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        try
        {
            var persisted = await SeedAnalysisAsync(cs, orgA, MaterializationReadiness.Ready);
            var analyses = new SqlServerPackageImportAnalysisRepository(cs);

            Assert.NotNull(await analyses.GetByAnalysisIdAsync(orgA, persisted.AnalysisId));
            Assert.Null(await analyses.GetByAnalysisIdAsync(orgB, persisted.AnalysisId));

            var service = CreateService(cs);
            await service.DecideAsync(
                persisted,
                PackageImportAnalysisDecisionKind.Confirmed,
                null,
                new PackageImportAnalysisDecisionActor(Guid.NewGuid(), orgA));

            Assert.Null(await service.GetEffectiveAsync(persisted.AnalysisId, orgB));
            Assert.Empty(await service.ListHistoryAsync(persisted.AnalysisId, orgB));
        }
        finally
        {
            await CleanupAsync(cs, orgA, orgB);
        }
    }

    [SqlServerFact]
    public async Task Exact_rationale_replay_is_idempotent_without_new_row()
    {
        var cs = await SetupAsync();
        var org = Guid.NewGuid();
        try
        {
            var persisted = await SeedAnalysisAsync(cs, org, MaterializationReadiness.Ready);
            var service = CreateService(cs);
            var actor = new PackageImportAnalysisDecisionActor(Guid.NewGuid(), org);

            var first = await service.DecideAsync(
                persisted, PackageImportAnalysisDecisionKind.Confirmed, "ok", actor);
            var second = await service.DecideAsync(
                persisted, PackageImportAnalysisDecisionKind.Confirmed, "ok", actor);

            Assert.False(first.IdempotentReplay);
            Assert.True(second.IdempotentReplay);
            Assert.Equal(first.Decision.DecisionId, second.Decision.DecisionId);
            Assert.Single(await service.ListHistoryAsync(persisted.AnalysisId, org));
        }
        finally
        {
            await CleanupAsync(cs, org);
        }
    }

    [SqlServerFact]
    public async Task Whitespace_difference_creates_new_decision()
    {
        var cs = await SetupAsync();
        var org = Guid.NewGuid();
        try
        {
            var persisted = await SeedAnalysisAsync(cs, org, MaterializationReadiness.Ready);
            var service = CreateService(cs);
            var actor = new PackageImportAnalysisDecisionActor(Guid.NewGuid(), org);

            var first = await service.DecideAsync(
                persisted, PackageImportAnalysisDecisionKind.Confirmed, "ok", actor);
            var second = await service.DecideAsync(
                persisted, PackageImportAnalysisDecisionKind.Confirmed, "ok ", actor);

            Assert.False(second.IdempotentReplay);
            Assert.Equal(2, second.Decision.DecisionSequence);
            Assert.Equal("ok", first.Decision.Rationale);
            Assert.Equal("ok ", second.Decision.Rationale);
        }
        finally
        {
            await CleanupAsync(cs, org);
        }
    }

    [SqlServerFact]
    public async Task Concurrent_same_decision_yields_single_row()
    {
        var cs = await SetupAsync();
        var org = Guid.NewGuid();
        try
        {
            var persisted = await SeedAnalysisAsync(cs, org, MaterializationReadiness.Ready);
            var actor = new PackageImportAnalysisDecisionActor(Guid.NewGuid(), org);

            var tasks = Enumerable.Range(0, 8).Select(_ =>
            {
                var service = CreateService(cs);
                return service.DecideAsync(
                    persisted, PackageImportAnalysisDecisionKind.Confirmed, "race", actor);
            });
            var results = await Task.WhenAll(tasks);

            Assert.Contains(results, r => !r.IdempotentReplay);
            Assert.All(results, r => Assert.Equal(1, r.Decision.DecisionSequence));
            Assert.True(results.Select(r => r.Decision.DecisionId).Distinct().Count() == 1);

            await using var connection = new SqlConnection(cs);
            await connection.OpenAsync();
            await using var count = new SqlCommand("""
                SELECT COUNT(1) FROM dbo.PackageImportAnalysisDecisions
                WHERE AnalysisId = @AnalysisId AND OrganizationId = @OrganizationId;
                """, connection);
            count.Parameters.AddWithValue("@AnalysisId", persisted.AnalysisId);
            count.Parameters.AddWithValue("@OrganizationId", org);
            Assert.Equal(1, (int)(await count.ExecuteScalarAsync())!);
        }
        finally
        {
            await CleanupAsync(cs, org);
        }
    }

    [SqlServerFact]
    public async Task Concurrent_different_analyses_are_independent()
    {
        var cs = await SetupAsync();
        var org = Guid.NewGuid();
        try
        {
            var a = await SeedAnalysisAsync(cs, org, MaterializationReadiness.Ready);
            var b = await SeedAnalysisAsync(cs, org, MaterializationReadiness.Ready);
            var actor = new PackageImportAnalysisDecisionActor(Guid.NewGuid(), org);

            var tasks = new[]
            {
                CreateService(cs).DecideAsync(a, PackageImportAnalysisDecisionKind.Confirmed, null, actor),
                CreateService(cs).DecideAsync(b, PackageImportAnalysisDecisionKind.Confirmed, null, actor),
                CreateService(cs).DecideAsync(a, PackageImportAnalysisDecisionKind.Confirmed, null, actor),
                CreateService(cs).DecideAsync(b, PackageImportAnalysisDecisionKind.Confirmed, null, actor)
            };
            var results = await Task.WhenAll(tasks);

            Assert.Equal(2, results.Select(r => r.Decision.AnalysisId).Distinct().Count());
            Assert.All(results, r => Assert.Equal(1, r.Decision.DecisionSequence));
            Assert.Single(await CreateService(cs).ListHistoryAsync(a.AnalysisId, org));
            Assert.Single(await CreateService(cs).ListHistoryAsync(b.AnalysisId, org));
            Assert.NotEqual(a.AnalysisId, b.AnalysisId);
        }
        finally
        {
            await CleanupAsync(cs, org);
        }
    }

    [SqlServerFact]
    public async Task Unique_and_fk_constraints_reject_invalid_rows()
    {
        var cs = await SetupAsync();
        var org = Guid.NewGuid();
        try
        {
            var persisted = await SeedAnalysisAsync(cs, org, MaterializationReadiness.Ready);
            var repo = new SqlServerPackageImportAnalysisDecisionRepository(cs);
            var actor = new PackageImportAnalysisDecisionActor(Guid.NewGuid(), org);
            var row = new PackageImportAnalysisDecision(
                Guid.NewGuid(), persisted.AnalysisId, org,
                PackageImportAnalysisDecisionKind.Confirmed, 1,
                DateTimeOffset.UtcNow, actor.Subject,
                PackageImportAnalysisDecisionIdentity.Issuer, null,
                persisted.SemanticFingerprint);

            await repo.AppendAsync(row);
            await Assert.ThrowsAsync<PackageImportAnalysisDecisionSequenceConflictException>(() =>
                repo.AppendAsync(row with { DecisionId = Guid.NewGuid() }));

            await using var connection = new SqlConnection(cs);
            await connection.OpenAsync();
            await using var orphan = new SqlCommand("""
                INSERT INTO dbo.PackageImportAnalysisDecisions
                (DecisionId, AnalysisId, OrganizationId, Decision, DecisionSequence,
                 DecidedAtUtc, DecidedBySubject, DecidedByIssuer, Rationale, SemanticFingerprintSnapshot)
                VALUES
                (@DecisionId, @AnalysisId, @OrganizationId, 'Confirmed', 1,
                 SYSUTCDATETIME(), @Subject, @Issuer, NULL, @Fingerprint);
                """, connection);
            orphan.Parameters.AddWithValue("@DecisionId", Guid.NewGuid());
            orphan.Parameters.AddWithValue("@AnalysisId", Guid.NewGuid());
            orphan.Parameters.AddWithValue("@OrganizationId", org);
            orphan.Parameters.AddWithValue("@Subject", actor.Subject);
            orphan.Parameters.AddWithValue("@Issuer", PackageImportAnalysisDecisionIdentity.Issuer);
            orphan.Parameters.AddWithValue("@Fingerprint", persisted.SemanticFingerprint);
            var ex = await Assert.ThrowsAsync<SqlException>(() => orphan.ExecuteNonQueryAsync());
            Assert.Contains(ex.Errors.Cast<SqlError>(), e => e.Number is 547);
        }
        finally
        {
            await CleanupAsync(cs, org);
        }
    }

    [SqlServerFact]
    public async Task Migration_005_is_idempotent_on_reapply()
    {
        var cs = await SetupAsync();
        await EnsureSchemaAsync(cs);
        await EnsureSchemaAsync(cs);

        await using var connection = new SqlConnection(cs);
        await connection.OpenAsync();
        await using var command = new SqlCommand("""
            SELECT
              CASE WHEN OBJECT_ID(N'dbo.PackageImportAnalysisDecisions', N'U') IS NULL THEN 0 ELSE 1 END,
              CASE WHEN EXISTS (
                SELECT 1 FROM sys.indexes
                WHERE object_id = OBJECT_ID(N'dbo.PackageImportAnalyses', N'U')
                  AND name = N'UX_PackageImportAnalyses_AnalysisId_OrganizationId'
                  AND is_unique = 1) THEN 1 ELSE 0 END;
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal(1, reader.GetInt32(1));
    }

    private static PackageImportAnalysisDecisionService CreateService(string connectionString) =>
        new(new SqlServerPackageImportAnalysisDecisionRepository(connectionString));

    private static PackageImportAnalysis SampleAnalysis(
        Guid organizationId,
        Guid receiptId,
        Guid packageId,
        string zipSha256,
        MaterializationReadiness readiness)
    {
        return new PackageImportAnalysis(
            organizationId, receiptId, packageId, Guid.NewGuid(), "0.1",
            "PhcKnowledgeScanner", "test", "DB", "sqlserver", null, null, "Full", "Complete", "1.0",
            zipSha256, zipSha256, PackageImportAnalysisSchema.Version,
            DateTimeOffset.UtcNow,
            new PackageImportIntegrityAnalysis(true, true, true, Array.Empty<string>()),
            Array.Empty<IkpInspectedCapabilityOperation>(),
            Array.Empty<IkpInspectedCoverageOperation>(),
            new PackageImportObjectIndexAnalysis(
                0, new SortedDictionary<string, int>(), new SortedDictionary<string, int>(),
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            new PackageImportArtifactsAnalysis(
                0, true, Array.Empty<IkpInspectedArtifact>(), new SortedDictionary<string, int>()),
            new PackageImportRelationsAnalysis(
                false, 0, 0, 0, new SortedDictionary<string, int>()),
            Array.Empty<IkpInspectedIssue>(),
            Array.Empty<string>(),
            new PackageImportReadinessAnalysis(readiness, Array.Empty<PackageImportReadinessFinding>()),
            Array.Empty<PackageImportUnavailableObject>());
    }

    private static IkpPackageValidationResult NewValidation()
    {
        var hash = Convert.ToHexString(SHA256.HashData(Guid.NewGuid().ToByteArray())).ToLowerInvariant();
        return new IkpPackageValidationResult(
            Guid.NewGuid(), Guid.NewGuid(), "0.1", "PhcKnowledgeScanner", "test",
            "DecisionTests", hash, 2048, 1, 1, 1, 1, 1, 0, hash);
    }
}
