/*
  PHC Knowledge — durable recovery for the filesystem/SQL receipt boundary.
  PackageReceiptAttempts are operational recovery records, not receipts.
  The validated ZIP remains immutable and permanent in the content-addressed archive.
*/

IF OBJECT_ID(N'dbo.PackageReceiptAttempts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PackageReceiptAttempts
    (
        AttemptId UNIQUEIDENTIFIER NOT NULL,
        OrganizationId UNIQUEIDENTIFIER NOT NULL,
        PackageId UNIQUEIDENTIFIER NOT NULL,
        CaptureId UNIQUEIDENTIFIER NOT NULL,

        ContractVersion NVARCHAR(16) NOT NULL,
        ProducerProductId NVARCHAR(128) NOT NULL,
        ProducerProductVersion NVARCHAR(64) NOT NULL,
        OriginDatabaseName NVARCHAR(128) NULL,
        ValidationSummaryJson NVARCHAR(MAX) NOT NULL,

        ZipSha256 CHAR(64) NOT NULL,
        ZipByteLength BIGINT NOT NULL,
        StorageKey NVARCHAR(512) NOT NULL,
        StagingKey NVARCHAR(256) NOT NULL,

        Status VARCHAR(24) NOT NULL,
        AttemptCount INT NOT NULL CONSTRAINT DF_PackageReceiptAttempts_AttemptCount DEFAULT (0),
        NextAttemptAtUtc DATETIME2(3) NOT NULL,
        LeaseToken UNIQUEIDENTIFIER NULL,
        LeaseUntilUtc DATETIME2(3) NULL,

        LastErrorCode NVARCHAR(100) NULL,
        LastErrorMessage NVARCHAR(2000) NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL,
        UpdatedAtUtc DATETIME2(3) NOT NULL,
        CompletedAtUtc DATETIME2(3) NULL,
        RowVersion ROWVERSION NOT NULL,

        CONSTRAINT PK_PackageReceiptAttempts PRIMARY KEY CLUSTERED (AttemptId),
        CONSTRAINT CK_PackageReceiptAttempts_ByteLength CHECK (ZipByteLength > 0),
        CONSTRAINT CK_PackageReceiptAttempts_Sha256 CHECK (LEN(ZipSha256) = 64),
        CONSTRAINT CK_PackageReceiptAttempts_AttemptCount CHECK (AttemptCount >= 0),
        CONSTRAINT CK_PackageReceiptAttempts_Status CHECK
            (Status IN ('PendingArchive', 'Completed', 'NeedsAttention')),
        CONSTRAINT CK_PackageReceiptAttempts_Lease CHECK
            ((LeaseToken IS NULL AND LeaseUntilUtc IS NULL) OR
             (LeaseToken IS NOT NULL AND LeaseUntilUtc IS NOT NULL)),
        CONSTRAINT CK_PackageReceiptAttempts_Completed CHECK
            ((Status = 'Completed' AND CompletedAtUtc IS NOT NULL) OR
             (Status <> 'Completed' AND CompletedAtUtc IS NULL))
    );

    CREATE UNIQUE INDEX UX_PackageReceiptAttempts_Organization_Package
        ON dbo.PackageReceiptAttempts (OrganizationId, PackageId);

    CREATE UNIQUE INDEX UX_PackageReceiptAttempts_Organization_Sha256
        ON dbo.PackageReceiptAttempts (OrganizationId, ZipSha256);

    CREATE INDEX IX_PackageReceiptAttempts_Recovery
        ON dbo.PackageReceiptAttempts (Status, NextAttemptAtUtc, LeaseUntilUtc)
        INCLUDE (AttemptId, OrganizationId, PackageId, ZipSha256)
        WHERE Status = 'PendingArchive';
END;
