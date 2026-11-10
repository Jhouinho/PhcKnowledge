/*
  PHC Knowledge — first central import persistence slice.
  OrganizationId is supplied by the authenticated organization context.
  The Organization table and FK are intentionally owned by the platform
  identity/tenancy phase and are not invented in this import migration.
*/

IF OBJECT_ID(N'dbo.PackageIngestionReceipts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PackageIngestionReceipts
    (
        ReceiptId UNIQUEIDENTIFIER NOT NULL,
        OrganizationId UNIQUEIDENTIFIER NOT NULL,
        PackageId UNIQUEIDENTIFIER NOT NULL,
        CaptureId UNIQUEIDENTIFIER NOT NULL,
        ContractVersion NVARCHAR(16) NOT NULL,
        ProducerProductId NVARCHAR(128) NOT NULL,
        ProducerProductVersion NVARCHAR(64) NOT NULL,
        OriginDatabaseName NVARCHAR(128) NULL,
        ZipSha256 CHAR(64) NOT NULL,
        ZipByteLength BIGINT NOT NULL,
        StorageKey NVARCHAR(512) NOT NULL,
        ReceivedAtUtc DATETIME2(7) NOT NULL,
        ValidationSummaryJson NVARCHAR(MAX) NOT NULL,

        CONSTRAINT PK_PackageIngestionReceipts
            PRIMARY KEY CLUSTERED (ReceiptId),

        CONSTRAINT CK_PackageIngestionReceipts_ByteLength
            CHECK (ZipByteLength > 0),

        CONSTRAINT CK_PackageIngestionReceipts_Sha256
            CHECK (LEN(ZipSha256) = 64)
    );

    CREATE UNIQUE INDEX UX_PackageIngestionReceipts_Organization_Package
        ON dbo.PackageIngestionReceipts (OrganizationId, PackageId);

    CREATE UNIQUE INDEX UX_PackageIngestionReceipts_Organization_Sha256
        ON dbo.PackageIngestionReceipts (OrganizationId, ZipSha256);

    CREATE INDEX IX_PackageIngestionReceipts_Organization_Received
        ON dbo.PackageIngestionReceipts (OrganizationId, ReceivedAtUtc DESC);
END;
