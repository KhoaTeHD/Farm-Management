@echo off
setlocal
title SSL - quanly.thienmyfarm.com
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Enable-Ssl.ps1"
set "RESULT=%ERRORLEVEL%"
echo.
if not "%RESULT%"=="0" echo SSL setup failed. Check DNS and inbound TCP 80/443, then retry this file.
pause
exit /b %RESULT%
