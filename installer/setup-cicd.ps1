#Requires -Version 5.1
<#
.SYNOPSIS
    RoboVAI PRO POS v6 - CI/CD Nightly Build Scheduler
    Registers a Windows Task Scheduler job that runs build-v6.ps1 every night at 2:00 AM.
.PARAMETER Unregister
    Remove the scheduled task instead of creating it.
.PARAMETER BuildTime
    Time for the nightly build. Default: 02:00AM
.EXAMPLE
    .\setup-cicd.ps1              # Register nightly task
    .\setup-cicd.ps1 -Unregister  # Remove nightly task
#>
param(
    [switch]$Unregister,
    [string]$BuildTime = "02:00AM"
)

$ErrorActionPreference = 'Stop'
$taskName   = "RoboVAI_PRO_NightlyBuild_v6"
$buildScript = Join-Path $PSScriptRoot "build-v6.ps1"
$psExe       = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"

Write-Host ""
Write-Host "=========================================================" -ForegroundColor Cyan
Write-Host "   RoboVAI PRO POS v6 - CI/CD Task Scheduler" -ForegroundColor Cyan
Write-Host "=========================================================" -ForegroundColor Cyan

# ── Unregister mode ─────────────────────────────────────────────────────────
if ($Unregister) {
    $existing = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
    if ($existing) {
        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
        Write-Host "  [OK] Task removed: $taskName" -ForegroundColor Green
    } else {
        Write-Host "  [INFO] Task not found: $taskName" -ForegroundColor Yellow
    }
    exit 0
}

# ── Register mode ────────────────────────────────────────────────────────────
if (-not (Test-Path $buildScript)) {
    Write-Host "  [ERROR] build-v6.ps1 not found: $buildScript" -ForegroundColor Red
    exit 1
}

$existing = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "  [INFO] Task already exists: $taskName" -ForegroundColor Yellow
    Write-Host "  Run with -Unregister to remove it first." -ForegroundColor Yellow
    exit 0
}

$argument  = "-ExecutionPolicy Bypass -NonInteractive -File `"$buildScript`" -SkipPublish -SkipSigning"
$action    = New-ScheduledTaskAction -Execute $psExe -Argument $argument -WorkingDirectory $PSScriptRoot
$trigger   = New-ScheduledTaskTrigger -Daily -At $BuildTime
$settings  = New-ScheduledTaskSettingsSet `
    -RunOnlyIfNetworkAvailable:$false `
    -StartWhenAvailable `
    -ExecutionTimeLimit (New-TimeSpan -Hours 2) `
    -MultipleInstances IgnoreNew
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest

Register-ScheduledTask `
    -TaskName    $taskName `
    -Action      $action `
    -Trigger     $trigger `
    -Settings    $settings `
    -Principal   $principal `
    -Description "RoboVAI PRO POS v6 - Automated nightly installer build (SkipPublish + SkipSigning)" `
    -Force | Out-Null

Write-Host ""
Write-Host "  [OK] Task registered successfully!" -ForegroundColor Green
Write-Host "       Name    : $taskName"           -ForegroundColor White
Write-Host "       Time    : Daily at $BuildTime" -ForegroundColor White
Write-Host "       Script  : $buildScript"        -ForegroundColor White
Write-Host "       Flags   : -SkipPublish -SkipSigning (fast mode)" -ForegroundColor White
Write-Host ""
Write-Host "  TIP: To remove: .\setup-cicd.ps1 -Unregister" -ForegroundColor DarkCyan
Write-Host "  TIP: To test now: Start-ScheduledTask -TaskName '$taskName'" -ForegroundColor DarkCyan
Write-Host "=========================================================" -ForegroundColor Green
Write-Host ""
