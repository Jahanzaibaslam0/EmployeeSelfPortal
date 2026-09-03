@echo off
title HRMS LocalDB Full Schema Setup
setlocal EnableExtensions
cd /d "%~dp0"

set "INSTANCE=(LocalDB)\MSSQLLocalDB"
set "DATADIR=D:\Project\DATA"
set "DB=HRMSDB"

echo.
echo  ========================================
echo   HRMS LocalDB Schema Setup
echo  ========================================
echo  Instance: %INSTANCE%
echo  Database: %DB%
echo  Files:    %DATADIR%
echo.

where sqlcmd >nul 2>&1
if errorlevel 1 (
  echo ERROR: sqlcmd not found.
  echo Install "SQL Server Command Line Utilities" or use SQL Server Management Studio
  echo and run the .sql files in this folder manually.
  pause
  exit /b 1
)

where sqllocaldb >nul 2>&1
if errorlevel 1 (
  echo ERROR: sqllocaldb not found. Install SQL Server Express LocalDB.
  echo https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb
  pause
  exit /b 1
)

echo Starting LocalDB...
sqllocaldb start MSSQLLocalDB
if errorlevel 1 (
  echo Failed to start MSSQLLocalDB.
  pause
  exit /b 1
)

if not exist "%DATADIR%" (
  echo Creating %DATADIR% ...
  mkdir "%DATADIR%"
)

echo.
echo [1/4] Creating database ^(files under %DATADIR%^)...
sqlcmd -S "%INSTANCE%" -E -b -i "%~dp0LocalDB_CreateDatabase.sql"
if errorlevel 1 goto :fail

echo.
echo [2/4] Core schema ^(Script.sql^)...
sqlcmd -S "%INSTANCE%" -E -d "%DB%" -b -i "%~dp0Script.sql"
if errorlevel 1 goto :fail

echo.
echo [3/4] User security tables...
sqlcmd -S "%INSTANCE%" -E -d "%DB%" -b -i "%~dp0UserSecurity_Script.sql"
if errorlevel 1 goto :fail

echo.
echo [4/4] Missing / inventory / leave / position tables...
sqlcmd -S "%INSTANCE%" -E -d "%DB%" -b -i "%~dp0LocalDB_MissingTables.sql"
if errorlevel 1 goto :fail

echo.
echo  ========================================
echo   SUCCESS – HRMSDB is ready on LocalDB
echo  ========================================
echo  Connection string ^(appsettings.json^):
echo  Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=HRMSDB;Integrated Security=True;TrustServerCertificate=True
echo.
echo  Next:
echo   1. Start the app: D:\Project\HRMS\RUN.bat
echo   2. Admin is created on startup: admin / Admin@123
echo   3. Or open: http://localhost:5080/ResetAdmin?run=1
echo.
pause
exit /b 0

:fail
echo.
echo SETUP FAILED – see messages above.
pause
exit /b 1
