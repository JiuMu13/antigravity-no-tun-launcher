<#
.SYNOPSIS
    Antigravity No-TUN 启动与守护脚本 (PowerShell 版)
.DESCRIPTION
    自动探测 Antigravity 安装位置，检查补齐 version.dll 与 config.json，并在更新后自动保持代理配置。
#>

param(
    [string]$CustomPath,
    [switch]$InstallShortcut,
    [switch]$DetectOnly
)

$ErrorActionPreference = "Stop"

function Find-AntigravityPath {
    param([string]$explicit)
    if ($explicit -and (Test-Path (Join-Path $explicit "Antigravity.exe"))) {
        return $explicit
    }

    $configJson = Join-Path $PSScriptRoot "launcher_config.json"
    if (Test-Path $configJson) {
        try {
            $cfg = Get-Content $configJson -Raw | ConvertFrom-Json
            if ($cfg.target_path -and (Test-Path (Join-Path $cfg.target_path "Antigravity.exe"))) {
                return $cfg.target_path
            }
        } catch {}
    }

    # 1. 运行中进程
    $proc = Get-Process -Name "Antigravity" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($proc -and $proc.Path -and (Test-Path $proc.Path)) {
        return (Split-Path $proc.Path)
    }

    # 2. 注册表检测
    $regPaths = @(
        "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*"
    )
    foreach ($reg in $regPaths) {
        $items = Get-ItemProperty $reg -ErrorAction SilentlyContinue | Where-Object { $_.DisplayName -like "*antigravity*" }
        foreach ($item in $items) {
            if ($item.InstallLocation -and (Test-Path (Join-Path $item.InstallLocation "Antigravity.exe"))) {
                return $item.InstallLocation
            }
            if ($item.DisplayIcon) {
                $iconPath = ($item.DisplayIcon -split ",")[0].Trim('"', ' ')
                $dir = Split-Path $iconPath
                if (Test-Path (Join-Path $dir "Antigravity.exe")) {
                    return $dir
                }
            }
            if ($item.UninstallString) {
                $uninst = ($item.UninstallString -split '"')[1]
                if (-not $uninst) { $uninst = ($item.UninstallString -split ' ')[0] }
                $dir = Split-Path $uninst
                if (Test-Path (Join-Path $dir "Antigravity.exe")) {
                    return $dir
                }
            }
        }
    }

    # 3. 默认常见目录
    $candidates = @(
        "$env:LOCALAPPDATA\Programs\antigravity",
        "$env:LOCALAPPDATA\antigravity",
        "$env:ProgramFiles\antigravity",
        "${env:ProgramFiles(x86)}\antigravity"
    )
    foreach ($c in $candidates) {
        if ($c -and (Test-Path (Join-Path $c "Antigravity.exe"))) {
            return $c
        }
    }

    # 4. 快捷方式检查
    $lnkFolders = @(
        [System.Environment]::GetFolderPath('Desktop'),
        [System.Environment]::GetFolderPath('StartMenu')
    )
    $ws = New-Object -ComObject WScript.Shell
    foreach ($f in $lnkFolders) {
        if (Test-Path $f) {
            $lnks = Get-ChildItem -Path $f -Filter "*antigravity*.lnk" -Recurse -ErrorAction SilentlyContinue
            foreach ($lnk in $lnks) {
                try {
                    $tgt = $ws.CreateShortcut($lnk.FullName).TargetPath
                    if ($tgt -and (Test-Path $tgt) -and ($tgt -like "*Antigravity.exe*")) {
                        return (Split-Path $tgt)
                    }
                } catch {}
            }
        }
    }

    return $null
}

$targetDir = Find-AntigravityPath -explicit $CustomPath

if (-not $targetDir) {
    Write-Host "[ERROR] 未能自动定位到 Antigravity 安装目录，请通过 -CustomPath 指定。" -ForegroundColor Red
    exit 1
}

if ($DetectOnly) {
    Write-Host "[OK] 检测到 Antigravity 安装目录: $targetDir" -ForegroundColor Green
    exit 0
}

Write-Host "Antigravity 目录: $targetDir" -ForegroundColor Cyan

# 资源文件探测
function Resolve-Asset {
    param([string]$name)
    $p1 = Join-Path $PSScriptRoot "assets\$name"
    if (Test-Path $p1) { return $p1 }
    $p2 = Join-Path (Split-Path $PSScriptRoot) "assets\$name"
    if (Test-Path $p2) { return $p2 }
    $p3 = Join-Path $PSScriptRoot $name
    if (Test-Path $p3) { return $p3 }
    return $null
}

$srcDll = Resolve-Asset "version.dll"
$srcCfg = Resolve-Asset "config.json"

$tgtDll = Join-Path $targetDir "version.dll"
$tgtCfg = Join-Path $targetDir "config.json"

# 补齐 version.dll
if (-not (Test-Path $tgtDll) -and $srcDll) {
    Copy-Item $srcDll $tgtDll -Force
    Write-Host "[OK] 自动补齐 version.dll" -ForegroundColor Green
}

# 补齐或同步 config.json
if (-not (Test-Path $tgtCfg) -and $srcCfg) {
    Copy-Item $srcCfg $tgtCfg -Force
    Write-Host "[OK] 自动补齐 config.json" -ForegroundColor Green
} elseif ((Test-Path $tgtCfg) -and $srcCfg) {
    if ((Get-Item $tgtCfg).LastWriteTimeUtc -gt (Get-Item $srcCfg).LastWriteTimeUtc) {
        Copy-Item $tgtCfg $srcCfg -Force
        Write-Host "[OK] 本地配置较新，已同步备份 config.json" -ForegroundColor Gray
    }
}

if ($InstallShortcut) {
    $ws = New-Object -ComObject WScript.Shell
    $desktop = [System.Environment]::GetFolderPath('Desktop')
    $sc = $ws.CreateShortcut("$desktop\Antigravity (No-TUN).lnk")
    $sc.TargetPath = "powershell.exe"
    $sc.Arguments = "-WindowStyle Hidden -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    $sc.WorkingDirectory = $targetDir
    $sc.IconLocation = "$targetDir\Antigravity.exe,0"
    $sc.Save()
    Write-Host "[OK] 桌面快捷方式已创建: $desktop\Antigravity (No-TUN).lnk" -ForegroundColor Green
    exit 0
}

# 启动 Antigravity
$exe = Join-Path $targetDir "Antigravity.exe"
Write-Host "正在启动 Antigravity..." -ForegroundColor Green
Start-Process -FilePath $exe -WorkingDirectory $targetDir -ArgumentList $args
