#Requires -Version 5.1
param(
    [string]$Configuration  = "Release",
    [string]$Runtime        = "win-x64",
    [string]$IsccPath       = "",
    [string]$CertThumbprint = "",
    [string]$CertPassword   = "",
    [switch]$SkipSigning,
    [switch]$SkipInstaller,
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot    = Split-Path -Parent $PSScriptRoot
$publishDir  = Join-Path $repoRoot "publish\final-exe"
$projectPath = Join-Path $repoRoot "src\SmartPOS.WPF\SmartPOS.WPF.csproj"
$issPath     = Join-Path $repoRoot "installer\SmartPOS.InnoSetup.v6.iss"
$certDir     = Join-Path $repoRoot "installer\cert"
$appExe      = Join-Path $publishDir "SmartPOS.WPF.exe"
$outputDir   = Join-Path $repoRoot "installer\Output"

function Write-Step { param([string]$msg) Write-Host "`n>> $msg" -ForegroundColor Cyan }
function Write-OK   { param([string]$msg) Write-Host "   [OK] $msg" -ForegroundColor Green }
function Write-Warn { param([string]$msg) Write-Host "   [WARN] $msg" -ForegroundColor Yellow }

function Invoke-Checked {
    param([string]$Tool, [string[]]$Params)
    Write-Host "   Running: $Tool $($Params -join ' ')" -ForegroundColor Gray
    & $Tool @Params
    if ($LASTEXITCODE -ne 0) { throw "FAILED: $Tool $($Params -join ' ')" }
}

function Find-ISCC {
    if ($IsccPath -and (Test-Path $IsccPath)) { return $IsccPath }
    $paths = @("C:\Program Files (x86)\Inno Setup 6\ISCC.exe","C:\Program Files\Inno Setup 6\ISCC.exe")
    foreach ($p in $paths) { if (Test-Path $p) { return $p } }
    return ""
}

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "   RoboVAI PRO POS v6.0 - AI Edition Build Pipeline      " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# Step 2 - Publish
if (-not $SkipPublish) {
    Write-Step "Step 2: dotnet publish (Release, win-x64, self-contained)"
    if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue }
    $publishArgs = @($projectPath,'-c',$Configuration,'-r',$Runtime,'--self-contained','true','-p:PublishSingleFile=false','-p:UseSharedCompilation=false','-p:AssemblyVersion=6.0.0.0','-p:FileVersion=6.0.0.0','-o',$publishDir)
    Invoke-Checked -Tool 'dotnet' -Params (@('publish') + $publishArgs)
    Write-OK "Published to: $publishDir"

    # Cleanup PDBs
    $pdbCount = (Get-ChildItem $publishDir -Filter "*.pdb" -Recurse).Count
    Get-ChildItem $publishDir -Filter "*.pdb" -Recurse | Remove-Item -Force -ErrorAction SilentlyContinue
    Write-OK "Removed $pdbCount PDB files"

    # Cleanup Language DLLs
    $keepLangs = @('ar', 'en', 'ar-SA', 'en-US', 'fr', 'es', 'tr', 'ur')
    Get-ChildItem $publishDir -Directory | Where-Object { $_.Name.Length -le 7 -and $_.Name -notin $keepLangs -and $_.Name -match '^[a-z]{2}(-[A-Z]{2})?$' } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    Write-OK "Removed unused language packs"

    # Cleanup Debug DLLs
    $debugDlls = @('mscordaccore_amd64_*.dll', 'mscordbi.dll', 'mscordaccore.dll')
    foreach ($pattern in $debugDlls) {
        Get-ChildItem $publishDir -Filter $pattern -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    }
    Write-OK "Removed debug diagnostics DLLs"

    # Cleanup XML Docs
    Get-ChildItem $publishDir -Filter "*.xml" -Recurse | Where-Object { $_.Name -notlike 'appsettings*' } | Remove-Item -Force -ErrorAction SilentlyContinue
    Write-OK "Removed XML doc files"
} else {
    Write-Warn "SkipPublish - using existing: $publishDir"
}

# Assets
Write-Step "Step 2.5: Generate Bundled Assets"
$readmeLines = @("RoboVAI PRO POS v6.0", "Welcome to RoboVAI PRO POS.")
Set-Content -Path (Join-Path $publishDir "README.txt") -Value $readmeLines -Encoding UTF8

# Ensure WMS folder is bundled into publish output
$wmsSource = Join-Path (Get-Item $PSScriptRoot).Parent.FullName "LandingPage\wms"
$wmsDest = Join-Path $publishDir "wms"
if (Test-Path $wmsSource) {
    Copy-Item -Path $wmsSource -Destination $wmsDest -Recurse -Force
    Write-OK "Bundled WMS web app to: $wmsDest"
}

& (Join-Path $PSScriptRoot 'generate-launchers.ps1') -publishDir $publishDir

$eulaLines = @(
    "End User License Agreement (EULA)",
    "===========================================================",
    "Software: RoboVAI PRO POS - AI Edition v6.0",
    "Publisher: RoboVAI Solutions",
    "",
    "1. This software is licensed, not sold.",
    "2. Reverse engineering or decompilation is strictly prohibited.",
    "3. The user is responsible for maintaining regular backups.",
    "",
    "Contact: robovaisolutions@gmail.com | +20 112 189 1913"
)
$eulaText = $eulaLines -join "`r`n"
Set-Content -Path (Join-Path $repoRoot "installer\EULA.txt") -Value $eulaText -Encoding UTF8

# Installers
if (-not $SkipInstaller) {
    Write-Step "Step 4: Build Installers via Inno Setup"
    $iscc = Find-ISCC
    if (-not $iscc) { Write-Warn "ISCC.exe not found. Skipping."; return }

    if (-not (Test-Path $outputDir)) { New-Item $outputDir -ItemType Directory | Out-Null }

    $editions = @(
        @{ 
            Name="Complete Edition"; 
            BaseFilename="RobovAI-PRO-POS-Setup-v6.0_Final"; 
            SmallImage="wizard_small.bmp"
        }
    )

    foreach ($ed in $editions) {
        Write-Host "   -- Building: $($ed.Name) -----------------------------" -ForegroundColor DarkCyan
        $configText = @"
#define MyOutputBaseFilename "$($ed.BaseFilename)"
#define MySmallImageFile "$($ed.SmallImage)"
#define EditionName "$($ed.Name)"
"@
        Set-Content -Path (Join-Path $repoRoot "installer\config.iss") -Value $configText -Encoding Ascii
        Invoke-Checked -Tool $iscc -Params @($issPath)
        Write-OK "Built: $($ed.BaseFilename).exe"
    }

    # Copy Complete Final installer to dist folder
    $distDir = Join-Path $repoRoot "dist"
    if (-not (Test-Path $distDir)) { New-Item -ItemType Directory -Path $distDir -Force | Out-Null }
    $finalSource = Join-Path $outputDir "RobovAI-PRO-POS-Setup-v6.0_Final.exe"
    $finalDest = Join-Path $distDir "RobovAI-PRO-POS-Setup-v6.0_Final.exe"
    if (Test-Path $finalSource) {
        Copy-Item -Path $finalSource -Destination $finalDest -Force
        Write-OK "Copied Final Complete installer to: $finalDest"
    }

    # Changelog
    & (Join-Path $PSScriptRoot 'generate-changelog.ps1')
}

Write-Host "=== BUILD COMPLETE: RoboVAI PRO POS v6.0 ===" -ForegroundColor Green
