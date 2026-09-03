@echo off
title HRMSDB Full Regeneration
setlocal EnableExtensions
cd /d "%~dp0"

echo.
echo  ========================================
echo   HRMSDB Full Database Regeneration
echo  ========================================
echo.

if "%~1"=="" (
  set /p SERVER=SQL Server instance [localhost]: 
  if "%SERVER%"=="" set "SERVER=localhost"
) else (
  set "SERVER=%~1"
)

where sqlcmd >nul 2>&1
if errorlevel 1 (
  echo ERROR: sqlcmd not found.
  echo Install SQL Server Command Line Utilities, or open HRMSDB_FullRegeneration.sql
  echo in SSMS with Query -^> SQLCMD Mode enabled.
  pause
  exit /b 1
)

echo Building fully inlined standalone script...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build_HRMSDB_FullRegeneration.ps1"
if errorlevel 1 (
  echo Build failed.
  pause
  exit /b 1
)

echo.
echo Running against %SERVER% ...
sqlcmd -S "%SERVER%" -E -b -i "%~dp0HRMSDB_FullRegeneration_Standalone.sql"
if errorlevel 1 goto :fail

echo.
echo SUCCESS – HRMSDB regenerated on %SERVER%
echo Update Web.config HRMSConnection, start the app once, login admin / Admin@123
echo.
pause
exit /b 0

:fail
echo.
echo FAILED – see messages above.
pause
exit /b 1
