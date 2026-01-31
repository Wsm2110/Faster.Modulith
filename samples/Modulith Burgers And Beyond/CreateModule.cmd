@echo off
setlocal enabledelayedexpansion

:START
cls
echo ========================================
echo    Module Scaffolding Tool
echo ========================================
echo.

REM Ask for module name
set MODULE_NAME=
set /p MODULE_NAME="Enter Module Name (or 'exit' to quit): "

REM Check for exit command
if /i "%MODULE_NAME%"=="exit" (
    echo.
    echo Goodbye!
    exit /b 0
)

REM Validate module name
if "%MODULE_NAME%"=="" (
    echo.
    echo ERROR: Module name cannot be empty!
    echo.
    pause
    goto START
)

echo.
echo Available .NET versions:
echo   1. net10.0 (default)
echo   2. net9.0
echo   3. net8.0
echo   4. Custom
echo.

set VERSION_CHOICE=
set /p VERSION_CHOICE="Select .NET version (1-4) [1]: "

REM Set default if empty
if "%VERSION_CHOICE%"=="" set VERSION_CHOICE=1

REM Map choice to framework version
set FRAMEWORK_VERSION=
if "%VERSION_CHOICE%"=="1" set FRAMEWORK_VERSION=net10.0
if "%VERSION_CHOICE%"=="2" set FRAMEWORK_VERSION=net9.0
if "%VERSION_CHOICE%"=="3" set FRAMEWORK_VERSION=net8.0
if "%VERSION_CHOICE%"=="4" (
    set /p FRAMEWORK_VERSION="Enter custom .NET version (e.g., net8.0): "
)

REM Validate framework version
if "%FRAMEWORK_VERSION%"=="" (
    echo.
    echo ERROR: Framework version cannot be empty!
    echo.
    pause
    goto START
)

echo.
echo ========================================
echo Creating module: %MODULE_NAME%
echo Framework: %FRAMEWORK_VERSION%
echo ========================================
echo.

REM Execute PowerShell script
powershell.exe -ExecutionPolicy Bypass -File "%~dp0/assets/scripts/create-module.ps1" -Name "%MODULE_NAME%" -FrameworkVersion "%FRAMEWORK_VERSION%"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Module creation failed!
    echo.
    set /p RETRY="Would you like to try again? (Y/N): "
    if /i "!RETRY!"=="Y" goto START
    exit /b %ERRORLEVEL%
)

echo.
echo ========================================
echo Module created successfully!
echo ========================================
echo.
set /p CREATE_ANOTHER="Create another module? (Y/N): "
if /i "%CREATE_ANOTHER%"=="Y" goto START

echo.
echo Goodbye!
pause