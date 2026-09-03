@echo off
title HRMS Web Forms (.NET 4.8)
setlocal EnableExtensions
cd /d "%~dp0"

echo.
echo  ========================================
echo   HRMS - ASP.NET Web Forms (.NET 4.8)
echo  ========================================
echo  NOT using .NET 8 / Razor / dotnet run
echo.
echo  URL:   http://localhost:5080/Login.aspx
echo  Login: admin / Admin@123
echo  DB:    LocalDB HRMSDB  (run Database\SetupLocalDB.bat once)
echo.

REM Static assets
if not exist "%~dp0css" if exist "%~dp0wwwroot\css" xcopy /E /I /Y "%~dp0wwwroot\css" "%~dp0css" >nul
if not exist "%~dp0js" if exist "%~dp0wwwroot\js" xcopy /E /I /Y "%~dp0wwwroot\js" "%~dp0js" >nul
if not exist "%~dp0images" if exist "%~dp0wwwroot\images" xcopy /E /I /Y "%~dp0wwwroot\images" "%~dp0images" >nul

REM Prefer Visual Studio build + IIS Express
set "IISEXPRESS=%ProgramFiles%\IIS Express\iisexpress.exe"
if not exist "%IISEXPRESS%" set "IISEXPRESS=%ProgramFiles(x86)%\IIS Express\iisexpress.exe"

echo Open HRMS.csproj in Visual Studio and press F5 for best results.
echo.
if exist "%IISEXPRESS%" (
  echo Starting IIS Express on port 5080...
  echo NOTE: site must be built first ^(bin\HRMS.dll^). Use VS Build if this fails.
  start "" http://localhost:5080/Login.aspx
  "%IISEXPRESS%" /path:"%~dp0" /port:5080
) else (
  echo IIS Express not found.
  echo Install Visual Studio with ASP.NET workload, open HRMS.csproj, press F5.
  pause
)
