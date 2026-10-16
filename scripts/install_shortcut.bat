@echo off
setlocal enabledelayedexpansion

set "ROOT_DIR=%~dp0.."
set "EXE=%ROOT_DIR%\AntigravityLauncher.exe"

if exist "%ROOT_DIR%\dist\AntigravityLauncher.exe" (
    set "EXE=%ROOT_DIR%\dist\AntigravityLauncher.exe"
)

if not exist "!EXE!" (
    set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    if exist "!CSC!" (
        echo [INFO] Compiling lightweight AntigravityLauncher.exe...
        "!CSC!" /target:winexe /optimize+ /win32icon:"%ROOT_DIR%\assets\app.ico" /resource:"%ROOT_DIR%\assets\version.dll",version.dll /resource:"%ROOT_DIR%\assets\config.json",config.json /out:"%ROOT_DIR%\AntigravityLauncher.exe" /reference:System.dll,System.Windows.Forms.dll,System.Drawing.dll "%ROOT_DIR%\src\*.cs" >nul 2>&1
        if exist "%ROOT_DIR%\AntigravityLauncher.exe" (
            set "EXE=%ROOT_DIR%\AntigravityLauncher.exe"
        )
    )
)

if exist "!EXE!" (
    echo [INFO] Creating desktop shortcut...
    "!EXE!" --install-shortcut
    echo [OK] Done!
    exit /b 0
)

echo [INFO] Falling back to PowerShell...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0launch.ps1" -InstallShortcut