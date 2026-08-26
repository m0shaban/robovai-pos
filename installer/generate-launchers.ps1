# Helper: generates launch.bat and RoboVAI-Launch.vbs in $args[0]
param([string]$publishDir)

# launch.bat - portable admin launcher
$bat = [System.Text.StringBuilder]::new()
[void]$bat.AppendLine('@echo off')
[void]$bat.AppendLine('title RoboVAI PRO POS v6.0')
[void]$bat.AppendLine('cd /d "%~dp0"')
[void]$bat.AppendLine('net session >nul 2>&1')
[void]$bat.AppendLine('if %errorlevel% neq 0 (')
[void]$bat.AppendLine('    powershell -Command "Start-Process ''%~f0'' -Verb RunAs"')
[void]$bat.AppendLine('    exit /b')
[void]$bat.AppendLine(')')
[void]$bat.AppendLine('start "" "SmartPOS.WPF.exe"')
[void]$bat.Append('exit')
[System.IO.File]::WriteAllText(
    (Join-Path $publishDir 'launch.bat'),
    $bat.ToString(),
    [System.Text.Encoding]::ASCII
)
Write-Host "   [OK] launch.bat generated" -ForegroundColor Green

# RoboVAI-Launch.vbs - silent admin launcher (no console window)
$vbs = [System.Text.StringBuilder]::new()
[void]$vbs.AppendLine('Set oShell = CreateObject("Shell.Application")')
[void]$vbs.AppendLine('strPath = CreateObject("Scripting.FileSystemObject").GetParentFolderName(WScript.ScriptFullName)')
[void]$vbs.Append('oShell.ShellExecute strPath & "\SmartPOS.WPF.exe", "", strPath, "runas", 1')
[System.IO.File]::WriteAllText(
    (Join-Path $publishDir 'RoboVAI-Launch.vbs'),
    $vbs.ToString(),
    [System.Text.Encoding]::ASCII
)
Write-Host "   [OK] RoboVAI-Launch.vbs generated" -ForegroundColor Green

# Remove any old .bat files except launch.bat
Get-ChildItem $publishDir -Filter '*.bat' |
    Where-Object { $_.Name -ne 'launch.bat' } |
    Remove-Item -Force -ErrorAction SilentlyContinue
