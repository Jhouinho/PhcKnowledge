/*
  PHC Knowledge — Incremento C: decisões humanas append-only sobre PackageImportAnalysis.
  Requer 004 (PackageImportAnalyses). Idempotente para reexecução do Install.
  Se 004 ainda não existir, este script não cria a tabela de decisões (no-op seguro).
*/

IF OBJECT_ID(N'dbo.PackageImportAnalyses', N'U') IS NOT NULL
BEGIN
    /* FK composta (AnalysisId, OrganizationId) exige UNIQUE nessas colunas na tabela de análises. */
    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.PackageImportAnalyses', N'U')
          AND name = N'UX_PackageImportAnalyses_AnalysisId_OrganizationId')
    BEGIN
        CREATE UNIQUE INDEX UX_PackageImportAnalyses_AnalysisId_OrganizationId
            ON dbo.PackageImportAnalyses (AnalysisId, OrganizationId);
    END;

    IF OBJECT_ID(N'dbo.PackageImportAnalysisDecisions', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PackageImportAnalysisDecisions
        (
            DecisionId UNIQUEIDENTIFIER NOT NULL,
            AnalysisId UNIQUEIDENTIFIER NOT NULL,
            OrganizationId UNIQUEIDENTIFIER NOT NULL,
            Decision VARCHAR(16) NOT NULL,
            DecisionSequence INT NOT NULL,
            DecidedAtUtc DATETIME2(7) NOT NULL,
            DecidedBySubject NVARCHAR(36) NOT NULL,
            DecidedByIssuer NVARCHAR(64) NOT NULL,
            Rationale NVARCHAR(2000) NULL,
            SemanticFingerprintSnapshot CHAR(64) NOT NULL,

            CONSTRAINT PK_PackageImportAnalysisDecisions
                PRIMARY KEY CLUSTERED (DecisionId),

            CONSTRAINT FK_PackageImportAnalysisDecisions_Analysis_Org
                FOREIGN KEY (AnalysisId, OrganizationId)
                REFERENCES dbo.PackageImportAnalyses (AnalysisId, OrganizationId),

            CONSTRAINT UX_PackageImportAnalysisDecisions_Analysis_Sequence
                UNIQUE (AnalysisId, DecisionSequence),

            CONSTRAINT CK_PackageImportAnalysisDecisions_Decision
                CHECK (Decision IN ('Confirmed', 'Rejected')),

            CONSTRAINT CK_PackageImportAnalysisDecisions_Sequence
                CHECK (DecisionSequence >= 1),

            CONSTRAINT CK_PackageImportAnalysisDecisions_Fingerprint
                CHECK (LEN(SemanticFingerprintSnapshot) = 64),

            CONSTRAINT CK_PackageImportAnalysisDecisions_SubjectLen
                CHECK (LEN(DecidedBySubject) = 36),

            CONSTRAINT CK_PackageImportAnalysisDecisions_IssuerLen
                CHECK (LEN(DecidedByIssuer) BETWEEN 1 AND 64)
        );

        CREATE INDEX IX_PackageImportAnalysisDecisions_Org_Analysis_Sequence
            ON dbo.PackageImportAnalysisDecisions (OrganizationId, AnalysisId, DecisionSequence DESC);
    END;
END;
