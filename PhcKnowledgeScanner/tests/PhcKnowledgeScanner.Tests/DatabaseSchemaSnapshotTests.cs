using System.Text.Json;
using PhcKnowledgeScanner.Abstractions.SchemaSnapshot;
using PhcKnowledgeScanner.Export.SchemaSnapshot;

namespace PhcKnowledgeScanner.Tests;

public class DatabaseSchemaSnapshotTests
{
    [Fact]
    public void Classification_Uses_Explicit_Conservative_Rules()
    {
        var system = SchemaObjectClassification.Classify(true, "u_fake");
        Assert.Equal(SchemaObjectClassification.SqlSystem, system.Classification);
        Assert.Equal(SchemaObjectClassification.CriterionMsShipped, system.Criterion);

        var custom = SchemaObjectClassification.Classify(false, "u_HcaPmPedido");
        Assert.Equal(SchemaObjectClassification.CandidateCustom, custom.Classification);
        Assert.Equal(SchemaObjectClassification.CriterionNamePrefixU, custom.Criterion);

        var unknown = SchemaObjectClassification.Classify(false, "FT");
        Assert.Equal(SchemaObjectClassification.Unknown, unknown.Classification);
        Assert.Equal(SchemaObjectClassification.CriterionNoPositiveEvidence, unknown.Criterion);
    }

    [Fact]
    public async Task Writer_Exports_Stable_Json_Structure_And_Hash()
    {
        var snapshot = BuildFixtureSnapshot();
        var dir = CreateTempDir();
        try
        {
            var writer = new DatabaseSchemaSnapshotWriter();
            var result = await writer.WriteAsync(snapshot, dir, new SchemaSnapshotOptions { FailOnPrivacyFindings = true });

            Assert.True(result.Succeeded);
            Assert.True(result.Privacy.IsSafe);
            Assert.False(string.IsNullOrWhiteSpace(result.SnapshotHash));
            Assert.True(File.Exists(Path.Combine(dir, "manifest.json")));
            Assert.True(File.Exists(Path.Combine(dir, "tables", "dbo.FT.json")));
            Assert.True(File.Exists(Path.Combine(dir, "tables", "dbo.u_HcaPmPedido.json")));
            Assert.True(File.Exists(Path.Combine(dir, "relationships", "relationships.json")));
            Assert.True(File.Exists(Path.Combine(dir, "indexes", "indexes.json")));
            Assert.True(File.Exists(Path.Combine(dir, "views", "views.json")));
            Assert.True(File.Exists(Path.Combine(dir, "triggers", "triggers.json")));
            Assert.True(File.Exists(Path.Combine(dir, "procedures", "procedures.json")));
            Assert.True(File.Exists(Path.Combine(dir, "functions", "functions.json")));
            Assert.True(File.Exists(Path.Combine(dir, "documentation", "overview.md")));

            var reloaded = await DatabaseSchemaSnapshotWriter.ReadAsync(dir);
            Assert.Equal(2, reloaded.Tables.Count);
            Assert.Equal(result.SnapshotHash, reloaded.Manifest.SnapshotHash);
            Assert.Equal(SchemaSnapshotHasher.Compute(reloaded), reloaded.Manifest.SnapshotHash);

            var ft = reloaded.Tables.Single(t => t.Name == "FT");
            Assert.Equal(3, ft.Columns.Count);
            Assert.Equal("ft", ft.Columns[0].Name);
            Assert.True(ft.Columns[0].IsIdentity);
            Assert.Equal((byte)18, ft.Columns[1].Precision);
            Assert.Equal((byte)5, ft.Columns[1].Scale);
            Assert.False(ft.Columns[2].IsNullable);
            Assert.NotNull(ft.PrimaryKey);
            Assert.Equal("PK_FT", ft.PrimaryKey!.ConstraintName);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task Safe_Mode_Excludes_Sql_Definitions_And_Blocks_When_Present()
    {
        var snapshot = BuildFixtureSnapshot();
        snapshot.Views[0].SqlDefinition = "CREATE VIEW dbo.v_demo AS SELECT 1 AS x";
        snapshot.Manifest.IncludesSqlDefinitions = false;

        var validator = new SchemaSnapshotPrivacyValidator();
        var report = validator.Validate(snapshot);
        Assert.False(report.IsSafe);
        Assert.Contains(report.Findings, f => f.Code == "privacy.view_sql_present");

        var dir = CreateTempDir();
        try
        {
            var writer = new DatabaseSchemaSnapshotWriter();
            var result = await writer.WriteAsync(snapshot, dir, new SchemaSnapshotOptions { FailOnPrivacyFindings = true });
            Assert.False(result.Succeeded);
            Assert.False(Directory.Exists(dir) && File.Exists(Path.Combine(dir, "manifest.json")));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    [Fact]
    public void Privacy_Rejects_Connection_Strings_And_Credentials()
    {
        var snapshot = BuildFixtureSnapshot();
        snapshot.Manifest.SqlServerVersion = "Data Source=evil;Initial Catalog=phc;Password=secret123";

        var report = new SchemaSnapshotPrivacyValidator().Validate(snapshot);
        Assert.False(report.IsSafe);
        Assert.Contains(report.Findings, f => f.Code is "privacy.server_version_leak" or "privacy.credential_pattern");
    }

    [Fact]
    public void Privacy_Does_Not_Treat_Business_Table_Names_As_Data()
    {
        var snapshot = BuildFixtureSnapshot();
        var report = new SchemaSnapshotPrivacyValidator().Validate(snapshot);
        Assert.True(report.IsSafe);
        Assert.DoesNotContain(report.Findings, f => f.Severity == "Error");
    }

    [Fact]
    public void Comparer_Detects_Table_Column_Relation_And_Index_Changes()
    {
        var left = BuildFixtureSnapshot();
        var right = BuildFixtureSnapshot();
        right.Tables.Add(new SchemaTableSnapshot
        {
            SchemaName = "dbo",
            Name = "SL",
            Classification = SchemaObjectClassification.Classify(false, "SL"),
            Columns =
            [
                new SchemaColumnSnapshot { Name = "slstamp", SqlType = "char", MaxLength = 25, Ordinal = 1, IsNullable = false }
            ]
        });

        var ft = right.Tables.Single(t => t.Name == "FT");
        ft.Columns.Add(new SchemaColumnSnapshot
        {
            Name = "u_obs",
            SqlType = "varchar",
            MaxLength = 200,
            IsNullable = true,
            Ordinal = 4
        });
        ft.Columns.Single(c => c.Name == "total").IsNullable = true;
        ft.Columns.Single(c => c.Name == "total").SqlType = "float";

        right.Relationships.Add(new SchemaForeignKeySnapshot
        {
            ConstraintName = "FK_FT_SL",
            ParentSchema = "dbo",
            ParentTable = "FT",
            ReferencedSchema = "dbo",
            ReferencedTable = "SL",
            Columns = [new SchemaForeignKeyColumnSnapshot { ParentColumn = "ftstamp", ReferencedColumn = "slstamp", Ordinal = 1 }]
        });

        right.Indexes.Add(new SchemaIndexSnapshot
        {
            SchemaName = "dbo",
            TableName = "FT",
            IndexName = "IX_FT_total",
            IndexType = "NONCLUSTERED",
            KeyColumns = [new SchemaIndexColumnSnapshot { ColumnName = "total", Ordinal = 1 }]
        });

        left.Manifest.SnapshotHash = SchemaSnapshotHasher.Compute(left);
        right.Manifest.SnapshotHash = SchemaSnapshotHasher.Compute(right);

        var comparison = new DatabaseSchemaSnapshotComparer().Compare(left, right);
        Assert.Contains("dbo.SL", comparison.TablesAdded);
        Assert.Contains(comparison.ColumnsAdded, c => c.TableKey == "dbo.FT" && c.ColumnName == "u_obs");
        Assert.Contains(comparison.ColumnPropertyChanges, c =>
            c.TableKey == "dbo.FT" && c.ColumnName == "total" && c.Property == "sqlType");
        Assert.Contains(comparison.ColumnPropertyChanges, c =>
            c.TableKey == "dbo.FT" && c.ColumnName == "total" && c.Property == "isNullable");
        Assert.Contains(comparison.RelationshipsAdded, r => r.Contains("FK_FT_SL", StringComparison.Ordinal));
        Assert.Contains(comparison.IndexesAdded, i => i.Contains("IX_FT_total", StringComparison.Ordinal));
        Assert.NotEqual(comparison.LeftHash, comparison.RightHash);
    }

    [Fact]
    public void Comparer_Detects_DropAndRecreate_When_Table_Name_Is_Unchanged()
    {
        var left = BuildFixtureSnapshot();
        var right = BuildFixtureSnapshot();
        var leftTable = left.Tables.Single(t => t.Name == "FT");
        var rightTable = right.Tables.Single(t => t.Name == "FT");
        leftTable.ObjectId = 120045;
        rightTable.ObjectId = 220078;
        leftTable.Columns[0].ObjectId = 120045;
        rightTable.Columns[0].ObjectId = 220078;
        leftTable.Columns[0].ColumnId = 1;
        rightTable.Columns[0].ColumnId = 1;

        left.Manifest.SnapshotHash = SchemaSnapshotHasher.Compute(left);
        right.Manifest.SnapshotHash = SchemaSnapshotHasher.Compute(right);

        var comparison = new DatabaseSchemaSnapshotComparer().Compare(left, right);

        Assert.Contains(comparison.ColumnPropertyChanges, c =>
            c.TableKey == "dbo.FT" && c.Property == "objectId" &&
            c.Before == "120045" && c.After == "220078");
        Assert.NotEqual(left.Manifest.SnapshotHash, right.Manifest.SnapshotHash);
    }

    [Fact]
    public void Legacy_Snapshot_Without_Native_Identifiers_Remains_Readable()
    {
        const string json = """
        {
          "schemaVersion": "1.0",
          "tables": [
            {
              "schemaName": "dbo",
              "name": "FT",
              "columns": [
                { "name": "ft", "sqlType": "int", "ordinal": 1 }
              ]
            }
          ]
        }
        """;

        var snapshot = JsonSerializer.Deserialize<DatabaseSchemaSnapshot>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(snapshot);
        var table = Assert.Single(snapshot!.Tables);
        Assert.Null(table.ObjectId);
        var column = Assert.Single(table.Columns);
        Assert.Null(column.ObjectId);
        Assert.Null(column.ColumnId);
    }

    [Fact]
    public void Fixture_Snapshot_Has_Pk_Fk_Index_Trigger_Procedure_Function_Metadata()
    {
        var snapshot = BuildFixtureSnapshot();
        var ft = snapshot.Tables.Single(t => t.Name == "FT");
        Assert.NotNull(ft.PrimaryKey);
        Assert.Equal(2, ft.PrimaryKey!.Columns.Count);
        Assert.Single(snapshot.Relationships);
        Assert.Equal(2, snapshot.Relationships[0].Columns.Count);
        Assert.Equal("NO_ACTION", snapshot.Relationships[0].OnDeleteAction);
        Assert.False(snapshot.Relationships[0].IsDisabled);
        Assert.Contains(snapshot.Indexes, i => i.IsPrimaryKey);
        Assert.Contains(snapshot.Triggers, t => t.Events.Contains("INSERT"));
        Assert.Contains(snapshot.Procedures, p => p.Parameters.Count == 1);
        Assert.Contains(snapshot.Functions, f => f.ReturnType == "int");
        Assert.All(snapshot.Views, v => Assert.Null(v.SqlDefinition));
        Assert.All(snapshot.Procedures, p => Assert.Null(p.SqlDefinition));
        Assert.All(snapshot.Functions, f => Assert.Null(f.SqlDefinition));
        Assert.All(snapshot.Triggers, t => Assert.Null(t.SqlDefinition));
    }

    [Fact]
    public async Task Partial_Category_Failure_Is_Represented_In_Manifest()
    {
        var snapshot = BuildFixtureSnapshot();
        snapshot.Manifest.CategoryStatuses.Add(new SchemaSnapshotCategoryStatus
        {
            Category = "indexes",
            Status = SchemaSnapshotCompletionStatus.Failed,
            Detail = "SqlException: VIEW DEFINITION permission denied"
        });
        snapshot.Manifest.Warnings.Add("Categoria 'indexes' incompleta");
        snapshot.Manifest.CompletionStatus = SchemaSnapshotCompletionStatus.Partial;
        snapshot.Manifest.Limitations.Add("Permissões insuficientes em indexes.");

        var dir = CreateTempDir();
        try
        {
            var writer = new DatabaseSchemaSnapshotWriter();
            var result = await writer.WriteAsync(snapshot, dir);
            Assert.True(result.Succeeded);
            var json = await File.ReadAllTextAsync(Path.Combine(dir, "manifest.json"));
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("Partial", doc.RootElement.GetProperty("completionStatus").GetString());
            Assert.Contains(doc.RootElement.GetProperty("warnings").EnumerateArray(), w =>
                w.GetString()!.Contains("indexes", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task Sample_Fixture_Directory_Is_Safe_For_Repository()
    {
        var snapshot = BuildFixtureSnapshot();
        snapshot.Manifest.OriginFingerprint = "a1b2c3d4e5f60718";
        snapshot.Manifest.ScannerVersion = "test";
        var sampleDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "samples", "database-schema-snapshot"));

        Directory.CreateDirectory(sampleDir);
        var writer = new DatabaseSchemaSnapshotWriter();
        var result = await writer.WriteAsync(snapshot, sampleDir, new SchemaSnapshotOptions { FailOnPrivacyFindings = true });
        Assert.True(result.Succeeded, string.Join("; ", result.Privacy.Findings.Select(f => f.Message)));
        Assert.True(result.Privacy.IsSafe);

        var overview = await File.ReadAllTextAsync(Path.Combine(sampleDir, "documentation", "overview.md"));
        Assert.DoesNotContain("Password=", overview, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Initial Catalog=", overview, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CREATE PROCEDURE", overview, StringComparison.OrdinalIgnoreCase);
    }

    private static DatabaseSchemaSnapshot BuildFixtureSnapshot()
    {
        var ft = new SchemaTableSnapshot
        {
            SchemaName = "dbo",
            Name = "FT",
            ObjectType = "USER_TABLE",
            Description = "Fixture table — not real client data",
            Classification = SchemaObjectClassification.Classify(false, "FT"),
            Columns =
            [
                new SchemaColumnSnapshot
                {
                    Name = "ft",
                    SqlType = "int",
                    Precision = 10,
                    Scale = 0,
                    IsNullable = false,
                    IsIdentity = true,
                    IdentitySeed = 1,
                    IdentityIncrement = 1,
                    Ordinal = 1,
                    IsPrimaryKey = true
                },
                new SchemaColumnSnapshot
                {
                    Name = "total",
                    SqlType = "decimal",
                    Precision = 18,
                    Scale = 5,
                    IsNullable = false,
                    Ordinal = 2,
                    DefaultDefinition = "((0))"
                },
                new SchemaColumnSnapshot
                {
                    Name = "ftstamp",
                    SqlType = "char",
                    MaxLength = 25,
                    IsNullable = false,
                    Ordinal = 3,
                    Collation = "Latin1_General_CI_AS",
                    IsPrimaryKey = true
                }
            ],
            PrimaryKey = new SchemaPrimaryKeySnapshot
            {
                ConstraintName = "PK_FT",
                Columns =
                [
                    new SchemaKeyColumnSnapshot { ColumnName = "ft", Ordinal = 1 },
                    new SchemaKeyColumnSnapshot { ColumnName = "ftstamp", Ordinal = 2 }
                ]
            }
        };

        var pedido = new SchemaTableSnapshot
        {
            SchemaName = "dbo",
            Name = "u_HcaPmPedido",
            Classification = SchemaObjectClassification.Classify(false, "u_HcaPmPedido"),
            Columns =
            [
                new SchemaColumnSnapshot
                {
                    Name = "u_id",
                    SqlType = "uniqueidentifier",
                    IsNullable = false,
                    Ordinal = 1,
                    IsPrimaryKey = true
                },
                new SchemaColumnSnapshot
                {
                    Name = "u_ftstamp",
                    SqlType = "char",
                    MaxLength = 25,
                    IsNullable = true,
                    Ordinal = 2
                },
                new SchemaColumnSnapshot
                {
                    Name = "u_computed",
                    SqlType = "int",
                    IsNullable = true,
                    IsComputed = true,
                    ComputedDefinition = "([u_id])",
                    Ordinal = 3
                }
            ],
            PrimaryKey = new SchemaPrimaryKeySnapshot
            {
                ConstraintName = "PK_u_HcaPmPedido",
                Columns = [new SchemaKeyColumnSnapshot { ColumnName = "u_id", Ordinal = 1 }]
            }
        };

        return new DatabaseSchemaSnapshot
        {
            Manifest = new SchemaSnapshotManifest
            {
                SchemaVersion = DatabaseSchemaSnapshot.CurrentSchemaVersion,
                ScannerVersion = "0.0.0-test",
                SqlServerProductVersion = "16.0.1000.5",
                SqlServerVersion = "Microsoft SQL Server 2022 (fixture)",
                CollectedAtUtc = new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero),
                OriginFingerprint = "a1b2c3d4e5f60718",
                IncludesSqlDefinitions = false,
                SafeForPublicShare = true,
                PhcSignatureDetected = true,
                CompletionStatus = SchemaSnapshotCompletionStatus.Succeeded,
                ObjectCounts = new SchemaSnapshotObjectCounts(),
                Classification = new SchemaSnapshotClassificationSummary(),
                Limitations =
                [
                    "Fixture de teste — sem dados de cliente.",
                    "Extração limitada a metadados de catálogo (sys.*). Sem registos de negócio."
                ]
            },
            Tables = [ft, pedido],
            Relationships =
            [
                new SchemaForeignKeySnapshot
                {
                    ConstraintName = "FK_u_HcaPmPedido_FT",
                    ParentSchema = "dbo",
                    ParentTable = "u_HcaPmPedido",
                    ReferencedSchema = "dbo",
                    ReferencedTable = "FT",
                    OnDeleteAction = "NO_ACTION",
                    OnUpdateAction = "NO_ACTION",
                    IsDisabled = false,
                    IsNotTrusted = false,
                    RelationSource = "sql.foreign_key",
                    Columns =
                    [
                        new SchemaForeignKeyColumnSnapshot { ParentColumn = "u_ftstamp", ReferencedColumn = "ftstamp", Ordinal = 1 },
                        new SchemaForeignKeyColumnSnapshot { ParentColumn = "u_id", ReferencedColumn = "ft", Ordinal = 2 }
                    ]
                }
            ],
            Indexes =
            [
                new SchemaIndexSnapshot
                {
                    SchemaName = "dbo",
                    TableName = "FT",
                    IndexName = "PK_FT",
                    IndexType = "CLUSTERED",
                    IsUnique = true,
                    IsPrimaryKey = true,
                    KeyColumns =
                    [
                        new SchemaIndexColumnSnapshot { ColumnName = "ft", Ordinal = 1 },
                        new SchemaIndexColumnSnapshot { ColumnName = "ftstamp", Ordinal = 2 }
                    ]
                },
                new SchemaIndexSnapshot
                {
                    SchemaName = "dbo",
                    TableName = "u_HcaPmPedido",
                    IndexName = "IX_u_HcaPmPedido_ftstamp",
                    IndexType = "NONCLUSTERED",
                    IsUnique = false,
                    FilterDefinition = "([u_ftstamp] IS NOT NULL)",
                    KeyColumns = [new SchemaIndexColumnSnapshot { ColumnName = "u_ftstamp", Ordinal = 1 }],
                    IncludedColumns = ["u_id"]
                }
            ],
            Views =
            [
                new SchemaViewSnapshot
                {
                    SchemaName = "dbo",
                    Name = "v_fixture",
                    Classification = SchemaObjectClassification.Classify(false, "v_fixture"),
                    HasDefinitionAvailable = true
                }
            ],
            Triggers =
            [
                new SchemaTriggerSnapshot
                {
                    SchemaName = "dbo",
                    Name = "tr_u_HcaPmPedido_ins",
                    ParentSchema = "dbo",
                    ParentObject = "u_HcaPmPedido",
                    ParentObjectType = "USER_TABLE",
                    Events = ["INSERT", "UPDATE"],
                    Classification = SchemaObjectClassification.Classify(false, "u_HcaPmPedido"),
                    HasDefinitionAvailable = true
                }
            ],
            Procedures =
            [
                new SchemaRoutineSnapshot
                {
                    SchemaName = "dbo",
                    Name = "usp_fixture",
                    RoutineKind = "PROCEDURE",
                    Classification = SchemaObjectClassification.Classify(false, "usp_fixture"),
                    HasDefinitionAvailable = true,
                    Parameters =
                    [
                        new SchemaParameterSnapshot
                        {
                            Name = "@ftstamp",
                            Ordinal = 1,
                            SqlType = "char",
                            MaxLength = 25
                        }
                    ]
                }
            ],
            Functions =
            [
                new SchemaRoutineSnapshot
                {
                    SchemaName = "dbo",
                    Name = "fn_fixture",
                    RoutineKind = "SQL_SCALAR_FUNCTION",
                    ReturnType = "int",
                    Classification = SchemaObjectClassification.Classify(false, "fn_fixture"),
                    HasDefinitionAvailable = true,
                    Parameters =
                    [
                        new SchemaParameterSnapshot
                        {
                            Name = "@value",
                            Ordinal = 1,
                            SqlType = "int"
                        }
                    ]
                }
            ],
            UserTypes =
            [
                new SchemaUserTypeSnapshot
                {
                    SchemaName = "dbo",
                    Name = "udt_fixture",
                    SystemTypeName = "nvarchar",
                    MaxLength = 100,
                    IsNullable = true
                }
            ],
            Synonyms =
            [
                new SchemaSynonymSnapshot
                {
                    SchemaName = "dbo",
                    Name = "syn_fixture",
                    BaseObjectName = "[dbo].[FT]"
                }
            ]
        };
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "PhcKnowledgeScanner.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
