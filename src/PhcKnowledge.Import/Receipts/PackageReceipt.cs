namespace PhcKnowledge.Import.Receipts;

public sealed record PackageReceipt(
    Guid ReceiptId,
    Guid OrganizationId,
    Guid PackageId,
    Guid CaptureId,
    string ContractVersion,
    string ProducerProductId,
    string ProducerProductVersion,
    string? OriginDatabaseName,
    string ZipSha256,
    long ZipByteLength,
    string StorageKey,
    DateTimeOffset ReceivedAtUtc,
    string ValidationSummaryJson);

public sealed record PackageReceiptRegistration(
    PackageReceipt Receipt,
    bool AlreadyRegistered);