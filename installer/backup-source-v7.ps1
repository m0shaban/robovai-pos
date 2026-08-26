#Requires -Version 5.1
<#
.SYNOPSIS
    RoboVAI PRO POS - Source Code Backup Script
    Creates a compressed ZIP of source code (excludes build artifacts)
.PARAMETER OutDir
    Destination folder for the ZIP. Default: installer\Output on F drive.
    If F is full, pass -OutDir D:\RoboVAI_Backups
.EXAMPLE
    .\backup-source-v7.ps1
    .\backup-source-v7.ps1 -OutDir D:\RoboVAI_Backups
#>
param([string]$OutDir = "")

$ErrorActionPreference = 'Stop'

$src     = Split-Path $PSScriptRoot -Parent
$exclude = @('publish', 'Output', 'obj', 'bin', '.git', '.vs', '.tempobj',
             'node_modules', '.code-review-graph', 'SourceBackup', 'RoboVAI_SourceBackup')

# Auto-pick destination: prefer F installer\Output, fallback to D:\RoboVAI_Backups
if ([string]::IsNullOrWhiteSpace($OutDir)) {
    $fDrive  = Get-PSDrive F -ErrorAction SilentlyContinue
    $fFreeGB = if ($fDrive) { [math]::Round($fDrive.Free / 1GB, 1) } else { 0 }
    if ($fFreeGB -ge 2) {
        $OutDir = Join-Path $PSScriptRoot 'Output'
    } else {
        $OutDir = 'D:\RoboVAI_Backups'
        Write-Host "[WARN] F drive low (${fFreeGB} GB) - redirecting to D drive" -ForegroundColor Yellow
    }
}

$zipName = 'RoboVAI_SourceBackup_v7_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.zip'
$zipPath = Join-Path $OutDir $zipName

if (-not (Test-Path $OutDir)) { New-Item $OutDir -ItemType Directory -Force | Out-Null }

$targetDrive = Split-Path $OutDir -Qualifier
$freeGB = [math]::Round((Get-PSDrive ($targetDrive.TrimEnd(':'))).Free / 1GB, 1)

Write-Host ""
Write-Host "=========================================================" -ForegroundColor Cyan
Write-Host "   RoboVAI PRO POS - Source Backup v7" -ForegroundColor Cyan
Write-Host "=========================================================" -ForegroundColor Cyan
Write-Host "Source      : $src"
Write-Host "Destination : $zipPath"
Write-Host "Free Space  : ${freeGB} GB on $targetDrive"
Write-Host ""

if ($freeGB -lt 1) {
    Write-Host "[ERROR] Not enough free space on $targetDrive (${freeGB} GB). Use -OutDir to specify another drive." -ForegroundColor Red
    exit 1
}

Write-Host "Scanning files..." -ForegroundColor Yellow
Add-Type -AssemblyName System.IO.Compression.FileSystem

$files = Get-ChildItem -Path $src -Recurse -File -ErrorAction SilentlyContinue | Where-Object {
    $p = $_.FullName
    $skip = $false
    foreach ($ex in $exclude) {
        if ($p -match [regex]::Escape($ex)) { $skip = $true; break }
    }
    -not $skip
}

Write-Host "Files to backup: $($files.Count)" -ForegroundColor Yellow
Write-Host "Compressing..." -ForegroundColor Yellow

$zipStream = [System.IO.Compression.ZipFile]::Open($zipPath, 'Create')
$count = 0; $errors = 0

foreach ($file in $files) {
    $relative = $file.FullName.Substring($src.Length + 1)
    try {
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $zipStream, $file.FullName, $relative,
            [System.IO.Compression.CompressionLevel]::Optimal
        ) | Out-Null
        $count++
        if ($count % 500 -eq 0) { Write-Host "  Packed $count / $($files.Count) files..." -ForegroundColor DarkGray }
    } catch {
        $errors++
    }
}
$zipStream.Dispose()

$sizeMB = [math]::Round((Get-Item $zipPath).Length / 1MB, 1)

Write-Host ""
Write-Host "=========================================================" -ForegroundColor Green
Write-Host "   BACKUP COMPLETE!" -ForegroundColor Green
Write-Host "=========================================================" -ForegroundColor Green
Write-Host "  File    : $zipName"       -ForegroundColor White
Write-Host "  Size    : ${sizeMB} MB"   -ForegroundColor White
Write-Host "  Packed  : $count files"   -ForegroundColor White
Write-Host "  Skipped : $errors files"  -ForegroundColor DarkGray
Write-Host "  Saved in: $OutDir"        -ForegroundColor White
Write-Host "=========================================================" -ForegroundColor Green
Write-Host ""
