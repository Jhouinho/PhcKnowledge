using PhcKnowledgeScanner.Abstractions.Inventory;
using PhcKnowledgeScanner.Abstractions.Inventory.Expression;
using PhcKnowledgeScanner.Abstractions.Inventory.Knowledge;
using PhcKnowledgeScanner.Abstractions.Workspace;

namespace PhcKnowledgeScanner.Analysis.Knowledge;

/// <summary>Contrato de projeção relacional (testável; uma invocação por sessão).</summary>
public interface IRelationalKnowledgeProjector
{
    Task<RelationalKnowledgeSnapshot> ProjectAsync(
        IScanWorkspace workspace,
        CancellationToken cancellationToken = default);

    RelationalKnowledgeSnapshot Project(
        IReadOnlyList<TechnicalObjectRecord> foreignKeys,
        IReadOnlyList<DeclaredRelationRecord> declaredRelations,
        IReadOnlyList<DeclaredRelationInterpretationRecord> interpretations);
}
