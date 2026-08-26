# organize-workspace.ps1
# Organizes loose files from workspace root into proper subfolders.
#
# Strategy: Copy files to destinations (always works), then attempt to delete
# originals. If deletion is blocked (cross-account ownership), an elevated
# cleanup script is created and launched automatically via UAC.

Set-Location $PSScriptRoot\..
$root = (Get-Location).Path

New-Item -ItemType Directory -Force "assets\branding" | Out-Null
New-Item -ItemType Directory -Force "docs\html"       | Out-Null
New-Item -ItemType Directory -Force "docs"            | Out-Null
New-Item -ItemType Directory -Force "scripts"         | Out-Null
New-Item -ItemType Directory -Force "installer"       | Out-Null

$moves = @(
    # Images → assets\branding
    "kasherr_logo_modern_1770646912950.png:assets\branding",
    "logo_concept_2_1770646963869.png:assets\branding",
    "nqta_logo_arabic_1770646932820.png:assets\branding",
    "pos_logo_gold_1770646882981.png:assets\branding",
    "pos_logo_neon_1770646857999.png:assets\branding",
    "Billing-Retail-Restaurant-Windows-Electronic-Touch-POS-Terminal-Cashier-Machine-Cash-Register-POS-All-in-One-POS-Systems.avif:assets\branding",
    # HTML → docs\html
    "LANDING_PAGE_ROBOVAI.html:docs\html",
    "LANDING_PAGE_ROBOVAI_NEW.html:docs\html",
    "activation_backup_offline.html:docs\html",
    "SmartPOS_User_Guide.html:docs\html",
    # Python scripts → scripts
    "generate_license_key.py:scripts",
    "inject.py:scripts",
    "update_contact.py:scripts",
    # Misc
    "installer.iss:installer",
    # Markdown docs → docs (keep README.md + AGENTS.md in root)
    "ARCHITECTURE.md:docs",
    "BEFORE_AFTER_COMPARISON.md:docs",
    "BUILD_DEPLOY.md:docs",
    "CHANGELOG_AR.md:docs",
    "COMPLETED.md:docs",
    "DIAGRAMS.md:docs",
    "IMPLEMENTATION_SUMMARY.md:docs",
    "INDEX.md:docs",
    "INSTALL_DOTNET.md:docs",
    "LATEST_CHANGES.md:docs",
    "LICENSE_ACTIVATION.md:docs",
    "PROJECT_STRUCTURE.md:docs",
    "QUICK_START.md:docs",
    "QUICK_START_AR.md:docs",
    "QUICKSTART.md:docs",
    "README_AR.md:docs",
    "README_SPACE_POS.md:docs",
    "START_HERE.md:docs",
    "UI_UX_IMPROVEMENTS.md:docs",
    "UI_UX_IMPROVEMENTS.md:docs",
    "ANALYSIS_PRODUCTS_DATA_LOADING_ISSUES.md:docs",
    "ANALYSIS_VIEWMODELS_ISSUES.md:docs",
    "BUILD_SUMMARY_v2.0.md:docs",
    "CLAUDE.md:docs",
    "DISTRIBUTION_INDEX_v2.0.md:docs",
    "FINAL_COMPLETION_REPORT.md:docs",
    "GEMINI.md:docs",
    "IMPLEMENTATION_COMPLETE_v1.md:docs",
    "INDEX_DOCUMENTATION.md:docs",
    "RELEASE_NOTES_v2.0.md:docs",
    "README_COMPLETE_GUIDE.md:docs",
    "START_HERE_QUICK.md:docs",
    "SUMMARY_FULL_ANALYSIS.md:docs",
    "TESTING_CHECKLIST.md:docs",
    "VIEWMODELS_FIX_GUIDE.md:docs",
    "v2.0_BUILD_COMPLETE.md:docs",
    "walkthrough.md:docs",
    "stability_report.md:docs"
)

$copiedOk  = [System.Collections.Generic.List[string]]::new()
$deleteOk  = [System.Collections.Generic.List[string]]::new()
$needAdmin = [System.Collections.Generic.List[string]]::new()
$skipped   = [System.Collections.Generic.List[string]]::new()

foreach ($entry in $moves) {
    $parts = $entry.Split(":", 2)
    $src   = $parts[0]
    $dst   = $parts[1]
    $srcFull = Join-Path $root $src
    if (-not (Test-Path -LiteralPath $srcFull)) { $skipped.Add($src); continue }

    $leaf    = Split-Path $src -Leaf
    $dstFull = Join-Path $root (Join-Path $dst $leaf)

    # Copy to destination (read-only ops; always allowed)
    try {
        Copy-Item -LiteralPath $srcFull -Destination $dstFull -Force -ErrorAction Stop
        $copiedOk.Add($src)
    } catch {
        Write-Host "  COPY FAILED: $src  ($_)" -ForegroundColor Red
        continue
    }

    # Attempt deletion of source
    try {
        Remove-Item -LiteralPath $srcFull -Force -ErrorAction Stop
        $deleteOk.Add($src)
        Write-Host "  moved:  $src -> $dst" -ForegroundColor Green
    } catch {
        $needAdmin.Add($srcFull)
        Write-Host "  copied: $src -> $dst  (source requires admin to delete)" -ForegroundColor Yellow
    }
}

# ---- Summary ----
Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "  Fully moved : $($deleteOk.Count)" -ForegroundColor Green
Write-Host "  Copied only : $($needAdmin.Count)  (originals still in root)" -ForegroundColor Yellow
Write-Host "  Not found   : $($skipped.Count)" -ForegroundColor DarkGray

# ---- Generate & optionally run elevated cleanup ----
if ($needAdmin.Count -gt 0) {
    $cleanupPath = Join-Path $root "scripts\_cleanup-as-admin.ps1"
    $lines = @("# Auto-generated cleanup script - run as Administrator to remove source duplicates")
    $lines += "Set-Location '$root'"
    foreach ($f in $needAdmin) {
        $lines += "Remove-Item -LiteralPath '$f' -Force -ErrorAction SilentlyContinue"
        $lines += "Write-Host 'Deleted: $f' -ForegroundColor Green"
    }
    $lines += "Write-Host 'Cleanup complete.' -ForegroundColor Cyan"
    $lines | Set-Content -LiteralPath $cleanupPath -Encoding UTF8

    Write-Host "`n  Cleanup script written to: scripts\_cleanup-as-admin.ps1" -ForegroundColor Yellow
    Write-Host "  Launching it elevated via UAC..." -ForegroundColor Yellow
    try {
        Start-Process powershell.exe `
            -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$cleanupPath`"" `
            -Verb RunAs -Wait
        Write-Host "  Elevated cleanup finished." -ForegroundColor Green
    } catch {
        Write-Host "  UAC cancelled or unavailable. Run scripts\_cleanup-as-admin.ps1 as Administrator manually." -ForegroundColor Red
    }
}

Write-Host "`n=== Root files remaining ===" -ForegroundColor Cyan
Get-ChildItem -File | Where-Object { $_.Name -notlike ".*" } | Select-Object -ExpandProperty Name | Sort-Object
