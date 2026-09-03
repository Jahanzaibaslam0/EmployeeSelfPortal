@echo off
title HRMS - Restore packages (no nuget.exe in project folder)
setlocal EnableExtensions
cd /d "%~dp0"

echo.
echo  Restoring ClosedXML and dependencies into .\packages
echo  Using TEMP folder (avoids Access Denied in project folder)
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0RestorePackages.ps1"
set ERR=%ERRORLEVEL%

echo.
if %ERR% NEQ 0 (
  echo RESTORE FAILED.
) else (
  echo Done. Open HRMS.sln in Visual Studio and Rebuild.
)
pause
exit /b %ERR%
