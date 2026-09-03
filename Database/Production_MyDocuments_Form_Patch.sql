/*==============================================================================
  Seed / ensure My Documents form registration (safe to re-run).
==============================================================================*/
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.tblAppForm', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.tblAppForm WHERE FormKey = N'MyDocuments')
    BEGIN
        INSERT INTO dbo.tblAppForm (FormKey, FormName, PagePath, Category, SortOrder, IsActive, CreatedOn)
        VALUES (N'MyDocuments', N'My Documents', N'/MyDocuments.aspx', N'Transaction', 3, 1, GETDATE());
    END
END
GO

PRINT N'MyDocuments form seed completed.';
GO
