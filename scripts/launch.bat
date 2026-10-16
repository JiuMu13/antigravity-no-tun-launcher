@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ========================================================
echo       Antigravity No-TUN Launcher (Batch 版)
echo ========================================================

set "TARGET_DIR="

:: 1. 检查默认安装目录
if exist "%LOCALAPPDATA%\Programs\antigravity\Antigravity.exe" (
    set "TARGET_DIR=%LOCALAPPDATA%\Programs\antigravity"
    goto :FOUND
)

if exist "%ProgramFiles%\antigravity\Antigravity.exe" (
    set "TARGET_DIR=%ProgramFiles%\antigravity"
    goto :FOUND
)

:: 2. 从注册表中查找
for /f "tokens=2*" %%a in ('reg query "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall" /s /f "antigravity" 2^>nul ^| findstr /i "DisplayIcon"') do (
    for /f "tokens=1 delims=," %%i in ("%%b") do (
        set "POTENTIAL_EXE=%%~dpi"
        set "POTENTIAL_EXE=!POTENTIAL_EXE:~0,-1!"
        if exist "!POTENTIAL_EXE!\Antigravity.exe" (
            set "TARGET_DIR=!POTENTIAL_EXE!"
            goto :FOUND
        )
    )
)

:FOUND
if not defined TARGET_DIR (
    echo [ERROR] 未能自动定位 Antigravity 安装目录！
    echo 请确认 Antigravity 是否正常安装。
    pause
    exit /b 1
)

echo [OK] 找到 Antigravity 目录: %TARGET_DIR%

:: 资源定位
set "SRC_DIR=%~dp0assets"
if not exist "%SRC_DIR%" set "SRC_DIR=%~dp0..\assets"
if not exist "%SRC_DIR%" set "SRC_DIR=%~dp0"

:: 检查并补齐 version.dll
if not exist "%TARGET_DIR%\version.dll" (
    if exist "%SRC_DIR%\version.dll" (
        copy /y "%SRC_DIR%\version.dll" "%TARGET_DIR%\version.dll" >nul
        echo [OK] 已自动补齐: version.dll
    )
)

:: 检查并补齐 config.json
if not exist "%TARGET_DIR%\config.json" (
    if exist "%SRC_DIR%\config.json" (
        copy /y "%SRC_DIR%\config.json" "%TARGET_DIR%\config.json" >nul
        echo [OK] 已自动补齐: config.json
    )
)

echo [OK] 正在启动 Antigravity...
start "" /d "%TARGET_DIR%" "%TARGET_DIR%\Antigravity.exe" %*
