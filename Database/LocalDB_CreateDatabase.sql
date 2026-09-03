-- =============================================
-- HRMS – Create LocalDB database (HRMSDB)
-- Data files: D:\Project\DATA
-- Instance:   (LocalDB)\MSSQLLocalDB
-- =============================================

SET NOCOUNT ON;
GO

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'HRMSDB')
BEGIN
    DECLARE @mdf NVARCHAR(260) = N'D:\Project\DATA\HRMSDB.mdf';
    DECLARE @ldf NVARCHAR(260) = N'D:\Project\DATA\HRMSDB_log.ldf';
    DECLARE @sql NVARCHAR(MAX) = N'
        CREATE DATABASE HRMSDB ON PRIMARY (
            NAME = N''HRMSDB'',
            FILENAME = N''' + @mdf + N''',
            SIZE = 64MB,
            FILEGROWTH = 64MB
        )
        LOG ON (
            NAME = N''HRMSDB_log'',
            FILENAME = N''' + @ldf + N''',
            SIZE = 16MB,
            FILEGROWTH = 16MB
        );';
    EXEC sp_executesql @sql;
    PRINT 'Created database HRMSDB under D:\Project\DATA';
END
ELSE
BEGIN
    PRINT 'Database HRMSDB already exists.';
END
GO

ALTER DATABASE HRMSDB SET RECOVERY SIMPLE;
GO

USE HRMSDB;
GO

PRINT 'LocalDB_CreateDatabase.sql completed.';
GO
