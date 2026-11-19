/*
  PHC Knowledge — Incremento B: persistência e auditoria de PackageImportAnalysis.
  A análise é o resultado da interpretação de um pacote/receipt, não conhecimento materializado.
  Unicidade: OrganizationId + PackageId + ZipSha256 + AnalysisSchemaVersion.
*/

IF OBJECT_ID(N'dbo.PackageImportAnalyses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PackageImportAnalyses
    (
        AnalysisId UNIQUEIDENTIFIER NOT NULL,
        OrganizationId UNIQUEIDENTIFIER NOT NULL,
        ReceiptId UNIQUEIDENTIFIER NOT NULL,
        PackageId UNIQUEIDENTIFIER NOT NULL,
        CaptureId UNIQUEIDENTIFIER NOT NULL,

        ZipSha256 CHAR(64) NOT NULL,
        ZipByteLength BIGINT NOT NULL,
        LogicalContentDigest CHAR(64) NULL,
        AnalysisSchemaVersion NVARCHAR(16) NOT NULL,
        ContractVersion NVARCHAR(16) NOT NULL,

        ReadinessStatus VARCHAR(32) NOT NULL,
        SemanticFingerprint CHAR(64) NOT NULL,
        AnalysisJson NVARCHAR(MAX) NOT NULL,

        ObjectCount INT NOT NULL,
        ArtifactCount INT NOT NULL,
        IssueCount INT NOT NULL,
        UnavailableObjectCount INT NOT NULL,

        AnalyzedAtUtc DATETIME2(7) NOT NULL,
        CreatedAtUtc DATETIME2(7) NOT NULL,

        CONSTRAINT PK_PackageImportAnalyses
            PRIMARY KEY CLUSTERED (AnalysisId),

        CONSTRAINT FK_PackageImportAnalyses_Receipt
            FOREIGN KEY (ReceiptId)
            REFERENCES dbo.PackageIngestionReceipts (ReceiptId),

        CONSTRAINT CK_PackageImportAnalyses_ByteLength
            CHECK (ZipByteLength > 0),

        CONSTRAINT CK_PackageImportAnalyses_Sha256
            CHECK (LEN(ZipSha256) = 64),

        CONSTRAINT CK_PackageImportAnalyses_SemanticFingerprint
            CHECK (LEN(SemanticFingerprint) = 64),

        CONSTRAINT CK_PackageImportAnalyses_AnalysisJson
            CHECK (ISJSON(AnalysisJson) = 1),

        CONSTRAINT CK_PackageImportAnalyses_Readiness
            CHECK (ReadinessStatus IN ('Ready', 'ReadyWithWarnings', 'Blocked')),

        CONSTRAINT CK_PackageImportAnalyses_Counts
            CHECK (ObjectCount >= 0 AND ArtifactCount >= 0 AND IssueCount >= 0 AND UnavailableObjectCount >= 0)
    );

    CREATE UNIQUE INDEX UX_PackageImportAnalyses_Org_Package_Sha_Schema
        ON dbo.PackageImportAnalyses (OrganizationId, PackageId, ZipSha256, AnalysisSchemaVersion);

    CREATE INDEX IX_PackageImportAnalyses_Organization_Created
        ON dbo.PackageImportAnalyses (OrganizationId, CreatedAtUtc DESC);

    CREATE INDEX IX_PackageImportAnalyses_Receipt
        ON dbo.PackageImportAnalyses (ReceiptId);
END;
