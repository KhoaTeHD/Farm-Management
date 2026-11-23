@echo off
setlocal
title Deploy Thien My Farm - quanly.thienmyfarm.com
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Iis.ps1"
set "RESULT=%ERRORLEVEL%"
echo.
if "%RESULT%"=="3010" (echo Restart Windows Server, then run this file again.) else if not "%RESULT%"=="0" (echo Deployment failed. Read the error above before retrying.)
pause
exit /b %RESULT%
