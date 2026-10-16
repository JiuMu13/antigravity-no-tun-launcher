@echo off
chcp 65001 >nul
setlocal

echo 正在创建 Antigravity (No-TUN) 桌面快捷方式...

powershell -NoProfile -ExecutionPolicy Bypass -Command "& '%~dp0launch.ps1' -InstallShortcut"

pause
