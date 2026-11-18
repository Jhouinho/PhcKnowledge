/*
  PHC Knowledge — canonical platform organization (single-tenant company).
  OrganizationId is a stable GUID created once by an explicit ensure operation (NEWID()).
  Receipts/attempts keep OrganizationId without an FK in this phase.
  PHC customers are not rows in this table.
*/

IF OBJECT_ID(N'dbo.Organizations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Organizations
    (
        OrganizationId UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        CreatedAtUtc DATETIME2(7) NOT NULL,
        IsCanonical BIT NOT NULL,

        CONSTRAINT PK_Organizations
            PRIMARY KEY CLUSTERED (OrganizationId),

        CONSTRAINT CK_Organizations_Name
            CHECK (LEN(LTRIM(RTRIM(Name))) > 0),

        CONSTRAINT CK_Organizations_IsCanonical
            CHECK (IsCanonical = 1)
    );

    /* At most one canonical organization (and, with CK_IsCanonical, at most one row). */
    CREATE UNIQUE INDEX UX_Organizations_Canonical
        ON dbo.Organizations (IsCanonical)
        WHERE IsCanonical = 1;
END;
