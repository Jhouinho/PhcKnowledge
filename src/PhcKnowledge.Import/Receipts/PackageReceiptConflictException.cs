namespace PhcKnowledge.Import.Receipts;

/// <summary>
/// Distinguishes client-imputable package identity conflicts from internal
/// receipt/archive inconsistencies without relying on exception message text.
/// </summary>
public enum PackageReceiptConflictKind
{
    IdentityMismatch = 0,
    InternalInconsistency = 1
}

public sealed class PackageReceiptConflictException : Exception
{
    public PackageReceiptConflictException(
        string message,
        PackageReceiptConflictKind kind = PackageReceiptConflictKind.IdentityMismatch)
        : base(message)
    {
        Kind = kind;
    }

    public PackageReceiptConflictKind Kind { get; }
}
