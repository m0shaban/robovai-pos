# backup-source.ps1
# Creates a timestamped snapshot of src\ into _Backups\
# Usage: .\scripts\backup-source.ps1
#        .\scripts\backup-source.ps1 -BackupRoot "D:\MyBackups"

param(
    [string]$BackupRoot = "$PSScriptRoot\..\_Backups"
)

$repoRoot   = Resolve-Path "$PSScriptRoot\.."
$srcPath    = Join-Path $repoRoot "src"
$timestamp  = Get-Date -Format "yyyy-MM-dd_HH-mm"
$backupPath = Join-Path $BackupRoot "src-backup-$timestamp"

if (-not (Test-Path $srcPath)) {
    Write-Host "ERROR: src\ not found at $srcPath" -ForegroundColor Red
    exit 1
}

Write-Host "Source  : $srcPath" -ForegroundColor Cyan
Write-Host "Dest    : $backupPath" -ForegroundColor Cyan

New-Item -ItemType Directory -Force $backupPath | Out-Null
Copy-Item -Path $srcPath -Destination $backupPath -Recurse -Force

$size = (Get-ChildItem $backupPath -Recurse -File | Measure-Object -Property Length -Sum).Sum
$sizeMB = [math]::Round($size / 1MB, 1)

Write-Host "`nBackup created: $backupPath  ($sizeMB MB)" -ForegroundColor Green
