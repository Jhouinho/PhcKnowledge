namespace PhcKnowledge.Import.Ikp;

public sealed class IkpPackageValidationException : Exception
{
    public IkpPackageValidationException(string message) : base(message) { }
    public IkpPackageValidationException(string message, Exception innerException) : base(message, innerException) { }
}