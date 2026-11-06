using System.Security.Cryptography;
using System.Text;
using PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;

namespace PhcKnowledgeScanner.Analysis.Knowledge;

/// <summary>
/// IDs determinísticos de evidências e KnowledgeRelations.
/// Sem GUIDs/timestamps. A ordem de leitura não influencia (normalização por sort).
/// </summary>
/// <remarks>
/// Exact: o ID incorpora o <c>correspondence_group_id</c> (baseado em nomes + hash da expressão),
/// pelo que uma mudança posterior de resolução table:* <b>não</b> altera o ID Exact.
/// Singleton declared / FK: IDs baseados na identidade da fonte.
/// </remarks>
public static class KnowledgeRelationIds
{
    public const string ConsolidationVersion = "kr-v1";

    public static string ForeignKeyEvidenceId(string fkObjectId) => fkObjectId;

    public static string DeclaredEvidenceId(string declaredRelationId) => declaredRelationId;

    public static string InterpretationEvidenceId(string declaredRelationId, string parserVersion) =>
        $"interp:{declaredRelationId}:{parserVersion}";

    public static string ExactKnowledgeRelationId(string correspondenceGroupId) =>
        $"kr:exact:{correspondenceGroupId}";

    public static string SingletonDeclaredKnowledgeRelationId(string declaredRelationId) =>
        $"kr:declared:{declaredRelationId}";

    public static string ForeignKeyKnowledgeRelationId(string fkObjectId) =>
        $"kr:fk:{fkObjectId}";

    /// <summary>
    /// Canonicaliza a lista de evidências de ligação (sort ordinal) para estabilidade.
    /// </summary>
    public static IReadOnlyList<string> CanonicalizeEvidenceIds(IEnumerable<string> evidenceIds) =>
        evidenceIds
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

    public static string StableHash(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
    }

    public static EndpointResolutionState ResolveEndpointState(RelationEndpoint from, RelationEndpoint to)
    {
        var fromOk = from.Resolution == Abstractions.Inventory.TableReferenceResolution.Resolved
                     && !string.IsNullOrWhiteSpace(from.ObjectId);
        var toOk = to.Resolution == Abstractions.Inventory.TableReferenceResolution.Resolved
                   && !string.IsNullOrWhiteSpace(to.ObjectId);
        if (fromOk && toOk)
        {
            return EndpointResolutionState.BothResolved;
        }

        if (fromOk || toOk)
        {
            return EndpointResolutionState.Partial;
        }

        return EndpointResolutionState.Unresolved;
    }
}
