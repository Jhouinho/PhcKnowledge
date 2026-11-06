using System.Text.Json.Serialization;

namespace PhcKnowledgeScanner.Abstractions.Inventory.Expression;

/// <summary>AST de uma expressão de ligação PHC (Camada interpretação).</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(LogicalExprNode), "logical")]
[JsonDerivedType(typeof(GroupExprNode), "group")]
[JsonDerivedType(typeof(ComparisonExprNode), "comparison")]
[JsonDerivedType(typeof(BinaryExprNode), "binary")]
[JsonDerivedType(typeof(FunctionCallExprNode), "function")]
[JsonDerivedType(typeof(FieldRefExprNode), "field")]
[JsonDerivedType(typeof(LiteralExprNode), "literal")]
[JsonDerivedType(typeof(UnsupportedExprNode), "unsupported")]
public abstract record ExprAstNode;

/// <summary>Operador lógico AND/OR com filhos (não achatado indevidamente).</summary>
public sealed record LogicalExprNode(
    string Operator,
    IReadOnlyList<ExprAstNode> Children) : ExprAstNode;

public sealed record GroupExprNode(ExprAstNode Inner) : ExprAstNode;

public sealed record ComparisonExprNode(
    ExprAstNode Left,
    string Operator,
    ExprAstNode Right) : ExprAstNode;

/// <summary>
/// Operação binária (ex. <c>+</c>). Não atribui semântica de concatenação — apenas a operação.
/// </summary>
public sealed record BinaryExprNode(
    string Operator,
    ExprAstNode Left,
    ExprAstNode Right) : ExprAstNode;

public sealed record FunctionCallExprNode(
    string FunctionName,
    IReadOnlyList<ExprAstNode> Arguments,
    string OriginalText) : ExprAstNode;

/// <summary>
/// Referência a campo. <see cref="Qualifier"/> é o qualificador de tabela quando existe;
/// um identificador isolado não é tratado como tabela.
/// </summary>
public sealed record FieldRefExprNode(
    string OriginalText,
    string? Qualifier,
    string FieldName,
    TableReferenceResolution TableResolution,
    string? TableObjectId) : ExprAstNode;

public sealed record LiteralExprNode(
    string Kind,
    string Value,
    string OriginalText) : ExprAstNode;

public sealed record UnsupportedExprNode(
    string OriginalText,
    string Reason) : ExprAstNode;
