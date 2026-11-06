using System.Text.Json;
using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Workspace;

namespace PhcKnowledgeScanner.Analysis;

/// <summary>
/// Deriva relações FK a partir de factos já recolhidos. Não altera factos.
/// Camada: Relation (não Inference).
/// </summary>
public sealed class ForeignKeyRelationAnalyzer
{
    public async Task<int> AnalyzeAsync(IScanWorkspace workspace, CancellationToken cancellationToken)
    {
        var fks = await workspace.GetObjectsAsync(ObjectKind.ForeignKey, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var count = 0;

        foreach (var fk in fks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(fk.SummaryJson))
            {
                continue;
            }

            using var doc = JsonDocument.Parse(fk.SummaryJson);
            var root = doc.RootElement;
            var refSchema = root.GetProperty("referencedSchema").GetString();
            var refTable = root.GetProperty("referencedTable").GetString();
            if (refSchema is null || refTable is null)
            {
                continue;
            }

            var toId = $"table:{refSchema}.{refTable}";
            var fromId = fk.ParentObjectId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(fromId))
            {
                continue;
            }

            // Convenção estável: rel:fk:{fkObjectId}. Distinto do facto FK (object_id fk:…).
            var relation = new DerivedRelationRecord(
                RelationId: $"rel:fk:{fk.ObjectId}",
                RelationType: "sql.foreign_key",
                FromObjectId: fromId,
                ToObjectId: toId,
                Evidence: $"Facto FK '{fk.Name}' ({fk.ObjectId}) referencia {toId}.",
                Layer: KnowledgeLayer.Relation,
                Confidence: 1.0,
                Explanation: "Relação derivada diretamente do metadado de foreign key recolhido (facto).",
                ValidationState: "Unvalidated");

            await workspace.SaveRelationAsync(relation, cancellationToken).ConfigureAwait(false);
            count++;
        }

        return count;
    }
}
