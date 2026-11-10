namespace PhcKnowledge.Import.Receipts;

public sealed class PackageReceiptConflictException : Exception
{
    public PackageReceiptConflictException(string message) : base(message) { }
}