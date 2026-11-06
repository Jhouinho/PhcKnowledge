using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PhcKnowledgeScanner.Abstractions.SchemaSnapshot;

namespace PhcKnowledgeScanner.Export.SchemaSnapshot;

/// <summary>Calcula hash canónico SHA-256 do conteúdo estrutural (exclui SnapshotHash).</summary>
public static class SchemaSnapshotHasher
{
    public static string Compute(DatabaseSchemaSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var previous = snapshot.Manifest.SnapshotHash;
        snapshot.Manifest.SnapshotHash = "";
        try
        {
            var payload = new
            {
                snapshot.Manifest.SchemaVersion,
                snapshot.Manifest.IncludesSqlDefinitions,
                snapshot.Manifest.OriginFingerprint,
                snapshot.Manifest.ObjectCounts,
                snapshot.Manifest.Classification,
                Tables = snapshot.Tables,
                Relationships = snapshot.Relationships,
                Indexes = snapshot.Indexes,
                Views = StripDefinitions(snapshot.Views.Select(v => new
                {
                    v.SchemaName,
                    v.Name,
                    v.IsMsShipped,
                    v.Classification,
                    v.HasDefinitionAvailable,
                    SqlDefinition = snapshot.Manifest.IncludesSqlDefinitions ? v.SqlDefinition : null
                })),
                Triggers = StripDefinitions(snapshot.Triggers.Select(t => new
                {
                    t.SchemaName,
                    t.Name,
                    t.ParentSchema,
                    t.ParentObject,
                    t.ParentObjectType,
                    t.IsDisabled,
                    t.IsInsteadOf,
                    t.Events,
                    t.Classification,
                    t.HasDefinitionAvailable,
                    SqlDefinition = snapshot.Manifest.IncludesSqlDefinitions ? t.SqlDefinition : null
                })),
                Procedures = StripDefinitions(snapshot.Procedures.Select(ToRoutineDto)),
                Functions = StripDefinitions(snapshot.Functions.Select(ToRoutineDto)),
                UserTypes = snapshot.UserTypes,
                Synonyms = snapshot.Synonyms
            };

            var json = JsonSerializer.Serialize(payload, SchemaSnapshotJson.Options);
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
        finally
        {
            snapshot.Manifest.SnapshotHash = previous;
        }

        object ToRoutineDto(SchemaRoutineSnapshot r) => new
        {
            r.SchemaName,
            r.Name,
            r.RoutineKind,
            r.ReturnType,
            r.IsMsShipped,
            r.Classification,
            r.Parameters,
            r.HasDefinitionAvailable,
            SqlDefinition = snapshot.Manifest.IncludesSqlDefinitions ? r.SqlDefinition : null
        };
    }

    private static IEnumerable<T> StripDefinitions<T>(IEnumerable<T> items) => items;
}
