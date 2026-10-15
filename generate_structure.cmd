@echo off
chcp 65001 >nul
echo Generating clean project structure...
powershell -NoProfile -ExecutionPolicy Bypass -File ".\generate_structure.ps1"
echo.
pause