/*==============================================================================
  Seed Designation Setup and Employee Category Setup on Master Setup
  (Employee Setup category). Safe to re-run.
==============================================================================*/
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.tblAppForm', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.tblAppForm WHERE FormKey = N'DesignationSetup')
        INSERT INTO dbo.tblAppForm (FormKey, FormName, PagePath, Category, SortOrder, IsActive, CreatedOn)
        VALUES (N'DesignationSetup', N'Designation Setup', N'/DesignationSetup.aspx', N'Employee Setup', 36, 1, GETDATE());
    ELSE
        UPDATE dbo.tblAppForm
        SET FormName = N'Designation Setup',
            PagePath = N'/DesignationSetup.aspx',
            Category = N'Employee Setup',
            SortOrder = 36
        WHERE FormKey = N'DesignationSetup';

    IF NOT EXISTS (SELECT 1 FROM dbo.tblAppForm WHERE FormKey = N'EmployeeCategorySetup')
        INSERT INTO dbo.tblAppForm (FormKey, FormName, PagePath, Category, SortOrder, IsActive, CreatedOn)
        VALUES (N'EmployeeCategorySetup', N'Employee Category Setup', N'/EmployeeCategorySetup.aspx', N'Employee Setup', 43, 1, GETDATE());
    ELSE
        UPDATE dbo.tblAppForm
        SET FormName = N'Employee Category Setup',
            PagePath = N'/EmployeeCategorySetup.aspx',
            Category = N'Employee Setup',
            SortOrder = 43
        WHERE FormKey = N'EmployeeCategorySetup';
END
GO

PRINT N'DesignationSetup / EmployeeCategorySetup form seed completed.';
GO
