/*==============================================================================
  Register Employee Attendance form (safe to re-run)
==============================================================================*/
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.tblAppForm', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.tblAppForm WHERE FormKey = N'EmployeeAttendance')
        INSERT INTO dbo.tblAppForm (FormKey, FormName, PagePath, Category, SortOrder, IsActive, CreatedOn)
        VALUES (N'EmployeeAttendance', N'Employee Attendance', N'/EmployeeAttendance.aspx', N'Transaction', 17, 1, GETDATE());
END
GO

PRINT N'EmployeeAttendance form seed completed.';
GO
