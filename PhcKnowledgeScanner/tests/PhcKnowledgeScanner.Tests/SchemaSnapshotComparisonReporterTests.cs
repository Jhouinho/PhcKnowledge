using PhcKnowledgeScanner.Abstractions.SchemaSnapshot;
using PhcKnowledgeScanner.Export.SchemaSnapshot;

namespace PhcKnowledgeScanner.Tests;

public class SchemaSnapshotComparisonReporterTests
{
    [Fact]
    public void Reports_Table_Added_And_Removed()
    {
        var left = BuildBase();
        var right = BuildBase();
        right.Tables.Add(new SchemaTableSnapshot { SchemaName = "dbo", Name = "NewTable", ObjectId = 10 });
        left.Tables.Add(new SchemaTableSnapshot { SchemaName = "dbo", Name = "OldTable", ObjectId = 11 });

        var output = Format(left, right);

        Assert.Contains("--- Tables ---", output, StringComparison.Ordinal);
        Assert.Contains("+ added: dbo.NewTable", output, StringComparison.Ordinal);
        Assert.Contains("- removed: dbo.OldTable", output, StringComparison.Ordinal);
        Assert.Contains("Tables: +1 -1 ~identity=0", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_Table_Native_Identity_Replacement_Same_Name()
    {
        var left = BuildBase();
        var right = BuildBase();
        left.Tables[0].ObjectId = 100;
        right.Tables[0].ObjectId = 200;

        var output = Format(left, right);

        Assert.Contains("--- Tables ---", output, StringComparison.Ordinal);
        Assert.Contains("~ native identity: dbo.Stable", output, StringComparison.Ordinal);
        Assert.Contains("previous=100", output, StringComparison.Ordinal);
        Assert.Contains("new=200", output, StringComparison.Ordinal);
        Assert.DoesNotContain("+ added: dbo.Stable", output, StringComparison.Ordinal);
        Assert.DoesNotContain("- removed: dbo.Stable", output, StringComparison.Ordinal);
        Assert.Contains("Native identity changes: 1", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_Column_Native_Identity_Change()
    {
        var left = BuildBase();
        var right = BuildBase();
        left.Tables[0].Columns[0].ObjectId = 100;
        right.Tables[0].Columns[0].ObjectId = 200;
        left.Tables[0].Columns[0].ColumnId = 1;
        right.Tables[0].Columns[0].ColumnId = 2;

        var output = Format(left, right);

        Assert.Contains("--- Columns ---", output, StringComparison.Ordinal);
        Assert.Contains("~ native identity (objectId): dbo.Stable.Id", output, StringComparison.Ordinal);
        Assert.Contains("previous=100", output, StringComparison.Ordinal);
        Assert.Contains("new=200", output, StringComparison.Ordinal);
        Assert.Contains("~ native identity (columnId): dbo.Stable.Id", output, StringComparison.Ordinal);
        Assert.Contains("previous=1", output, StringComparison.Ordinal);
        Assert.Contains("new=2", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_PrimaryKey_And_Supporting_Index_Identity()
    {
        var left = BuildBase();
        var right = BuildBase();
        left.Tables[0].PrimaryKey!.ObjectId = 901;
        right.Tables[0].PrimaryKey!.ObjectId = 902;
        left.Tables[0].PrimaryKey!.IndexId = 1;
        right.Tables[0].PrimaryKey!.IndexId = 2;

        var output = Format(left, right);

        Assert.Contains("--- Primary Keys ---", output, StringComparison.Ordinal);
        Assert.Contains(
            "~ constraint native identity: dbo.Stable.PK_Stable | previous=901 | new=902",
            output,
            StringComparison.Ordinal);
        Assert.Contains(
            "~ supporting index native identity: dbo.Stable.PK_Stable | previous=1 | new=2",
            output,
            StringComparison.Ordinal);
        Assert.Contains("Primary Keys: ~constraintIdentity=1 ~supportingIndexIdentity=1", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_Index_And_ForeignKey_Changes_Including_Native_Ids()
    {
        var left = BuildBase();
        var right = BuildBase();
        left.Indexes[0].ObjectId = 100;
        left.Indexes[0].IndexId = 2;
        right.Indexes[0].ObjectId = 200;
        right.Indexes[0].IndexId = 2;
        left.Relationships[0].ObjectId = 50;
        right.Relationships[0].ObjectId = 60;
        right.Relationships.Add(new SchemaForeignKeySnapshot
        {
            ConstraintName = "FK_New",
            ParentSchema = "dbo",
            ParentTable = "Stable",
            ReferencedSchema = "dbo",
            ReferencedTable = "Stable",
            ObjectId = 70,
            Columns = [new SchemaForeignKeyColumnSnapshot { ParentColumn = "Id", ReferencedColumn = "Id", Ordinal = 1 }]
        });

        var output = Format(left, right);

        Assert.Contains("--- Indexes ---", output, StringComparison.Ordinal);
        Assert.Contains("+ added/replaced: index:dbo.Stable.IX_Stable_Name | native_id=", output, StringComparison.Ordinal);
        Assert.Contains("- removed/replaced: index:dbo.Stable.IX_Stable_Name | native_id=", output, StringComparison.Ordinal);
        Assert.Contains("--- Foreign Keys ---", output, StringComparison.Ordinal);
        Assert.Contains("+ added/replaced: foreign_key:dbo.Stable.FK_New", output, StringComparison.Ordinal);
        Assert.Contains("foreign_key:dbo.Stable.FK_Stable | native_id=", output, StringComparison.Ordinal);
        Assert.Contains("Native identity changes:", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_OtherObjectChanges_For_View_Trigger_Procedure_Function()
    {
        var left = BuildBase();
        var right = BuildBase();
        left.Views[0].ObjectId = 1;
        right.Views[0].ObjectId = 2;
        left.Triggers[0].ObjectId = 3;
        right.Triggers[0].ObjectId = 4;
        left.Procedures[0].ObjectId = 5;
        right.Procedures[0].ObjectId = 6;
        left.Functions[0].ObjectId = 7;
        right.Functions[0].ObjectId = 8;
        right.Views.Add(new SchemaViewSnapshot { SchemaName = "dbo", Name = "v_new", ObjectId = 9 });

        var output = Format(left, right);

        Assert.Contains("--- Views ---", output, StringComparison.Ordinal);
        Assert.Contains("view:dbo.v_demo | native_id=", output, StringComparison.Ordinal);
        Assert.Contains("+ added/replaced: view:dbo.v_new", output, StringComparison.Ordinal);
        Assert.Contains("--- Triggers ---", output, StringComparison.Ordinal);
        Assert.Contains("trigger:dbo.tr_demo | native_id=", output, StringComparison.Ordinal);
        Assert.Contains("--- Procedures ---", output, StringComparison.Ordinal);
        Assert.Contains("procedure:dbo.usp_demo | native_id=", output, StringComparison.Ordinal);
        Assert.Contains("--- Functions ---", output, StringComparison.Ordinal);
        Assert.Contains("function:dbo.fn_demo | native_id=", output, StringComparison.Ordinal);
        Assert.Contains("Views: ", output, StringComparison.Ordinal);
        Assert.Contains("Triggers: ", output, StringComparison.Ordinal);
        Assert.Contains("Procedures: ", output, StringComparison.Ordinal);
        Assert.Contains("Functions: ", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_No_Differences_Explicitly()
    {
        var left = BuildBase();
        var right = BuildBase();
        left.Tables[0].ObjectId = 10;
        right.Tables[0].ObjectId = 10;

        var output = Format(left, right);

        Assert.Contains("Total changes: 0", output, StringComparison.Ordinal);
        Assert.Contains("No differences.", output, StringComparison.Ordinal);
        Assert.DoesNotContain("--- Tables ---", output, StringComparison.Ordinal);
        Assert.Contains("Native identity changes: 0", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Legacy_Snapshots_Without_Native_Ids_Do_Not_Invent_Identity_Changes()
    {
        var left = BuildBase();
        var right = BuildBase();
        // Left legado sem IDs; right com IDs — comparer não infere substituição.
        right.Tables[0].ObjectId = 999;
        right.Tables[0].Columns[0].ObjectId = 999;
        right.Tables[0].Columns[0].ColumnId = 1;
        right.Tables[0].PrimaryKey!.ObjectId = 1000;
        right.Tables[0].PrimaryKey!.IndexId = 1;
        right.Indexes[0].ObjectId = 999;
        right.Indexes[0].IndexId = 2;
        right.Relationships[0].ObjectId = 50;
        right.Views[0].ObjectId = 1;
        right.Triggers[0].ObjectId = 2;
        right.Procedures[0].ObjectId = 3;
        right.Functions[0].ObjectId = 4;

        var comparison = new DatabaseSchemaSnapshotComparer().Compare(left, right);
        var output = SchemaSnapshotComparisonReporter.Format(comparison);

        Assert.Empty(comparison.OtherObjectChanges);
        Assert.Contains("Total changes: 0", output, StringComparison.Ordinal);
        Assert.Contains("No differences.", output, StringComparison.Ordinal);
        Assert.Contains("Native identity changes: 0", output, StringComparison.Ordinal);
        Assert.DoesNotContain("previous=", output, StringComparison.Ordinal);
        Assert.DoesNotContain("native_id=", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Does_Not_Silently_Truncate_Many_Changes()
    {
        var left = BuildBase();
        var right = BuildBase();
        for (var i = 0; i < 30; i++)
        {
            right.Tables.Add(new SchemaTableSnapshot
            {
                SchemaName = "dbo",
                Name = $"T{i:00}",
                ObjectId = 1000 + i
            });
        }

        var output = Format(left, right);

        for (var i = 0; i < 30; i++)
        {
            Assert.Contains($"+ added: dbo.T{i:00}", output, StringComparison.Ordinal);
        }

        Assert.Contains("Tables: +30 -0 ~identity=0", output, StringComparison.Ordinal);
    }

    private static string Format(DatabaseSchemaSnapshot left, DatabaseSchemaSnapshot right)
    {
        left.Manifest.SnapshotHash = SchemaSnapshotHasher.Compute(left);
        right.Manifest.SnapshotHash = SchemaSnapshotHasher.Compute(right);
        var comparison = new DatabaseSchemaSnapshotComparer().Compare(left, right);
        return SchemaSnapshotComparisonReporter.Format(comparison);
    }

    private static DatabaseSchemaSnapshot BuildBase() =>
        new()
        {
            Manifest = new SchemaSnapshotManifest
            {
                SchemaVersion = DatabaseSchemaSnapshot.CurrentSchemaVersion,
                ScannerVersion = "0.0.0-test",
                CollectedAtUtc = new DateTimeOffset(2026, 11, 8, 12, 0, 0, TimeSpan.Zero),
                OriginFingerprint = "fixture",
                IncludesSqlDefinitions = false,
                SafeForPublicShare = true,
                CompletionStatus = SchemaSnapshotCompletionStatus.Succeeded
            },
            Tables =
            [
                new SchemaTableSnapshot
                {
                    SchemaName = "dbo",
                    Name = "Stable",
                    Columns =
                    [
                        new SchemaColumnSnapshot { Name = "Id", SqlType = "int", Ordinal = 1, IsNullable = false },
                        new SchemaColumnSnapshot { Name = "Name", SqlType = "nvarchar", MaxLength = 100, Ordinal = 2, IsNullable = false }
                    ],
                    PrimaryKey = new SchemaPrimaryKeySnapshot
                    {
                        ConstraintName = "PK_Stable",
                        Columns = [new SchemaKeyColumnSnapshot { ColumnName = "Id", Ordinal = 1 }]
                    }
                }
            ],
            Indexes =
            [
                new SchemaIndexSnapshot
                {
                    SchemaName = "dbo",
                    TableName = "Stable",
                    IndexName = "IX_Stable_Name",
                    IndexType = "NONCLUSTERED",
                    KeyColumns = [new SchemaIndexColumnSnapshot { ColumnName = "Name", Ordinal = 1 }]
                }
            ],
            Relationships =
            [
                new SchemaForeignKeySnapshot
                {
                    ConstraintName = "FK_Stable",
                    ParentSchema = "dbo",
                    ParentTable = "Stable",
                    ReferencedSchema = "dbo",
                    ReferencedTable = "Stable",
                    Columns =
                    [
                        new SchemaForeignKeyColumnSnapshot
                        {
                            ParentColumn = "Id",
                            ReferencedColumn = "Id",
                            Ordinal = 1
                        }
                    ]
                }
            ],
            Views = [new SchemaViewSnapshot { SchemaName = "dbo", Name = "v_demo" }],
            Triggers =
            [
                new SchemaTriggerSnapshot
                {
                    SchemaName = "dbo",
                    Name = "tr_demo",
                    ParentSchema = "dbo",
                    ParentObject = "Stable"
                }
            ],
            Procedures = [new SchemaRoutineSnapshot { SchemaName = "dbo", Name = "usp_demo", RoutineKind = "PROCEDURE" }],
            Functions = [new SchemaRoutineSnapshot { SchemaName = "dbo", Name = "fn_demo", RoutineKind = "FUNCTION" }]
        };
}
