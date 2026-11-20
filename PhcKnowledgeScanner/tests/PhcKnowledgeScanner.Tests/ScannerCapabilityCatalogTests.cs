using PhcKnowledgeScanner.Abstractions.Coverage;

namespace PhcKnowledgeScanner.Tests;

public sealed class ScannerCapabilityCatalogTests
{
    [Fact]
    public void Sql_Triggers_Declare_Version_2_0_Without_Changing_Other_Operations()
    {
        var capabilities = ScannerCapabilityCatalog.ForSqlServerStructure;
        var triggers = Assert.Single(capabilities, x => x.OperationId == KnownOperations.SqlTriggers);
        var tables = Assert.Single(capabilities, x => x.OperationId == KnownOperations.SqlTablesInventory);
        var procedures = Assert.Single(capabilities, x => x.OperationId == KnownOperations.SqlProcedures);

        Assert.Equal("2.0", triggers.CapabilityVersion);
        Assert.Equal(KnownOperations.Version, tables.CapabilityVersion);
        Assert.Equal(KnownOperations.Version, procedures.CapabilityVersion);
        Assert.Equal("sql.server.structure", triggers.CollectorId);
        Assert.True(triggers.Supported);
    }

    [Fact]
    public void Unsupported_Operations_Keep_Default_Capability_Version()
    {
        var unsupported = ScannerCapabilityCatalog.Unsupported("future.operation");

        Assert.Equal(KnownOperations.Version, unsupported.CapabilityVersion);
        Assert.False(unsupported.Supported);
    }
}
