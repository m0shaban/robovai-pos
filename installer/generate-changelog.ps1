#Requires -Version 5.1
<#
.SYNOPSIS
    RoboVAI PRO POS v6 - Changelog Generator
    Generates CHANGELOG_v6.0.txt from git log (last 90 days) or manual entries.
#>
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$outDir   = Join-Path $PSScriptRoot 'Output'

if (-not (Test-Path $outDir)) { New-Item $outDir -ItemType Directory -Force | Out-Null }

$changelogPath = Join-Path $outDir 'CHANGELOG_v6.0.txt'

Write-Host ""
Write-Host "=========================================================" -ForegroundColor Cyan
Write-Host "   RoboVAI PRO POS v6.0 - Changelog Generator" -ForegroundColor Cyan
Write-Host "=========================================================" -ForegroundColor Cyan

# Try git first
$gitOk = (Get-Command git -ErrorAction SilentlyContinue) -ne $null
$usedGit = $false

if ($gitOk) {
    $since  = (Get-Date).AddDays(-90).ToString('yyyy-MM-dd')
    try {
        $gitLog = & git -C $repoRoot log --no-merges "--since=$since" "--pretty=format:%ad | %s  [%an]" "--date=short" 2>$null
    } catch {
        $gitLog = $null
    }

    if ($gitLog -and $gitLog.Count -gt 0) {
        $header = @(
            "========================================================",
            "   RoboVAI PRO POS v6.0 - Changelog (Last 90 Days)",
            "========================================================",
            "Generated: $([DateTime]::Now.ToString('yyyy-MM-dd HH:mm:ss'))",
            ""
        )
        ($header + $gitLog) | Set-Content -Path $changelogPath -Encoding UTF8
        Write-Host "  [OK] Git changelog: $($gitLog.Count) commits -> $changelogPath" -ForegroundColor Green
        $usedGit = $true
    }
}

# Fallback: manual/curated changelog
if (-not $usedGit) {
    if ($gitOk) {
        Write-Host "  [WARN] No git commits found in last 90 days - using manual changelog" -ForegroundColor Yellow
    } else {
        Write-Host "  [INFO] Git not found - using manual changelog" -ForegroundColor Yellow
    }

    @(
        "========================================================",
        "   RoboVAI PRO POS v6.0 - Release Changelog",
        "========================================================",
        "Generated: $([DateTime]::Now.ToString('yyyy-MM-dd HH:mm:ss'))",
        "",
        "[ v6.0.0 ] - 2026-05-06  (Current Release)",
        "  * AI Stock Predictor",
        "  * Rental Device Management",
        "  * Staff Meals System",
        "  * Customer Loyalty (Bronze/Silver/Gold)",
        "  * Role-Based Access Control (RBAC) + Audit Log",
        "  * PDF/Excel report export",
        "  * Automatic DB backup on upgrade (Inno Setup)",
        "  * Bilingual installer Arabic/English",
        "  * Release Notes page in installer",
        "  * Nightly CI/CD build pipeline",
        "  * PDB/Language/XML cleanup (~86 MB reduction)",
        "  * Portable launcher",
        "  * Admin auto-elevation",
        "  * Version bump: 4.0.0 -> 6.0.0",
        "",
        "[ v5.x ] - Feb 2026 to May 2026",
        "  * Initial stable POS release",
        "  * Purchase Orders, Returns, Expenses, Shifts",
        "  * License system with 10-year validity",
        "  * ESC/POS thermal printer support",
        "  * Customer display window",
        "  * Barcode scanner integration",
        "  * Multi-user RBAC",
        "",
        "========================================================",
        "   Support: +20 112 189 1913",
        "   Email  : robovaisolutions@gmail.com",
        "   Website: https://robovai.tech",
        "   Made with pride in Egypt",
        "========================================================"
    ) | Set-Content -Path $changelogPath -Encoding UTF8

    Write-Host "  [OK] Manual changelog created -> $changelogPath" -ForegroundColor Green
}

Write-Host "=========================================================" -ForegroundColor Green
Write-Host ""
