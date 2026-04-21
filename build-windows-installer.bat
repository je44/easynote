@echo off
setlocal
powershell -ExecutionPolicy Bypass -File "%~dp0build-windows-installer.ps1" %*
endlocal
