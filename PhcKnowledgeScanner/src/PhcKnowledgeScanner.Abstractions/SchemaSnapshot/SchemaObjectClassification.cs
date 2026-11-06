namespace PhcKnowledgeScanner.Abstractions.SchemaSnapshot;

/// <summary>
/// Classificação conservadora de objectos do schema.
/// Não afirma origem PHC standard apenas por ausência de prefixo <c>u_</c>.
/// </summary>
public static class SchemaObjectClassification
{
    public const string SqlSystem = "sql_system";
    public const string CandidateCustom = "candidate_custom";
    public const string Unknown = "unknown";

    public const string CriterionMsShipped = "sys.objects.is_ms_shipped";
    public const string CriterionNamePrefixU = "name_prefix_u_";
    public const string CriterionNoPositiveEvidence = "no_positive_standard_or_custom_evidence";
    public const string SourceTableInventoryClassification = "TableInventoryClassification";

    public static ObjectClassificationInfo Classify(bool isMsShipped, string objectName)
    {
        if (isMsShipped)
        {
            return new ObjectClassificationInfo(
                SqlSystem,
                CriterionMsShipped,
                SourceTableInventoryClassification);
        }

        if (!string.IsNullOrEmpty(objectName)
            && objectName.StartsWith("u_", StringComparison.OrdinalIgnoreCase))
        {
            return new ObjectClassificationInfo(
                CandidateCustom,
                CriterionNamePrefixU,
                SourceTableInventoryClassification);
        }

        return new ObjectClassificationInfo(
            Unknown,
            CriterionNoPositiveEvidence,
            SourceTableInventoryClassification);
    }
}

/// <summary>Origem e critério da classificação de um objecto.</summary>
public sealed record ObjectClassificationInfo(
    string Classification,
    string Criterion,
    string Source);
