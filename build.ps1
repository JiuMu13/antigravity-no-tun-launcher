# Antigravity No-TUN Launcher 一键编译与打包脚本
param(
    [string]$Mode = "all" # "dotnet", "csc", or "all"
)

$ErrorActionPreference = "Stop"
$rootDir = $PSScriptRoot
$srcDir = Join-Path $rootDir "src"
$assetsDir = Join-Path $rootDir "assets"
$distDir = Join-Path $rootDir "dist"

if (Test-Path $distDir) {
    Remove-Item -Path $distDir -Recurse -Force
}
New-Item -ItemType Directory -Path $distDir -Force | Out-Null
New-Item -ItemType Directory -Path "$distDir\assets" -Force | Out-Null

Write-Host "===> 正在复制静态资源..." -ForegroundColor Cyan
Copy-Item "$assetsDir\*" -Destination "$distDir\assets\" -Force

# 1. 使用 Windows 自带的 csc.exe 编译超轻量免依赖版本 (约 15KB)
if ($Mode -eq "csc" -or $Mode -eq "all") {
    Write-Host "===> 正在通过 csc.exe 编译超轻量原生版本..." -ForegroundColor Cyan
    $cscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    if (Test-Path $cscPath) {
        $sourceFiles = Get-ChildItem -Path $srcDir -Filter "*.cs" | ForEach-Object { "`"$($_.FullName)`"" }
        $outExe = Join-Path $distDir "AntigravityLauncher.exe"
        $ico = Join-Path $assetsDir "app.ico"
        
        $args = @(
            "/target:winexe",
            "/optimize+",
            "/win32icon:`"$ico`"",
            "/out:`"$outExe`"",
            "/reference:System.dll",
            "/reference:System.Windows.Forms.dll",
            "/reference:System.Drawing.dll"
        ) + $sourceFiles

        & $cscPath $args | Out-Null
        if (Test-Path $outExe) {
            Write-Host "[OK] 原生轻量版编译成功: $outExe ($((Get-Item $outExe).Length) 字节)" -ForegroundColor Green
        }
    } else {
        Write-Host "[WARN] 未找到 csc.exe，跳过轻量版编译" -ForegroundColor Yellow
    }
}

# 2. 使用 dotnet publish 编译现代 .NET 版本
if ($Mode -eq "dotnet" -or $Mode -eq "all") {
    Write-Host "===> 正在通过 dotnet 发布现代 .NET 单文件版本..." -ForegroundColor Cyan
    $csproj = Join-Path $srcDir "AntigravityLauncher.csproj"
    $dotnetDist = Join-Path $distDir "net-version"
    New-Item -ItemType Directory -Path $dotnetDist -Force | Out-Null
    
    & dotnet publish $csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o $dotnetDist | Out-Null
    New-Item -ItemType Directory -Path "$dotnetDist\assets" -Force | Out-Null
    Copy-Item "$assetsDir\*" -Destination "$dotnetDist\assets\" -Force
    Write-Host "[OK] .NET 版本发布完成: $dotnetDist" -ForegroundColor Green
}

# 3. 复制辅助脚本到 dist
Copy-Item "$rootDir\scripts\*" -Destination $distDir -Force

Write-Host "`n===============================================" -ForegroundColor Green
Write-Host "打包成功！产物目录: $distDir" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green
