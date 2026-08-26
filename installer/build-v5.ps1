param(
    [string]$Configuration  = "Release",
    [string]$Runtime        = "win-x64",
    [string]$IsccPath       = "",
    [string]$CertThumbprint = "",
    [string]$CertPassword   = "",
    [switch]$SkipSigning,
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot    = Split-Path -Parent $PSScriptRoot
$publishDir  = Join-Path $repoRoot "publish\final-exe"
$projectPath = Join-Path $repoRoot "src\SmartPOS.WPF\SmartPOS.WPF.csproj"
$issPath     = Join-Path $repoRoot "installer\SmartPOS.InnoSetup.v5.iss"
$certDir     = Join-Path $repoRoot "installer\cert"
$cerPath     = Join-Path $certDir  "RoboVAI-CodeSign.cer"
$appExe      = Join-Path $publishDir "SmartPOS.WPF.exe"
$outputDir   = Join-Path $repoRoot "installer\Output"

function Write-Step { param([string]$msg) Write-Host "`n>> $msg" -ForegroundColor Cyan }
function Write-OK   { param([string]$msg) Write-Host "   [OK] $msg" -ForegroundColor Green }
function Write-Warn { param([string]$msg) Write-Host "   [WARN] $msg" -ForegroundColor Yellow }

function Invoke-Checked {
    param([string]$Tool, [string[]]$Params)
    $p = $Params -join ' '
    Write-Host "   Running: $Tool $p" -ForegroundColor Gray
    & $Tool @Params
    if ($LASTEXITCODE -ne 0) { throw "FAILED: $Tool $($Params -join ' ')" }
}

function Find-SignTool {
    $paths = @(
        "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe",
        "C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe",
        "C:\Program Files (x86)\Windows Kits\10\App Certification Kit\signtool.exe"
    )
    foreach ($p in $paths) { if (Test-Path $p) { return $p } }
    return ""
}

function Resolve-CertificatePath {
    $candidates = @(
        (Join-Path $certDir "RoboVAI-CodeSign-10Y.pfx"),
        (Join-Path $certDir "RoboVAI-CodeSign.pfx"),
        (Join-Path $repoRoot "installer\RoboVAI-CodeSign.pfx")
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    return ""
}

function Get-CertificatePassword {
    if (-not [string]::IsNullOrWhiteSpace($CertPassword)) {
        return $CertPassword
    }

    if (-not [string]::IsNullOrWhiteSpace($env:ROBOVAI_CERT_PASSWORD)) {
        return $env:ROBOVAI_CERT_PASSWORD
    }

    return ""
}

function Write-ReleaseManifest {
    param([hashtable[]]$BuiltInstallers)

    $manifestPath = Join-Path $outputDir "FINAL_RELEASE_MANIFEST_v5.0.txt"
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("RoboVAI PRO POS - Final Release Manifest v5.0")
    $lines.Add("Generated: $([DateTime]::Now.ToString('yyyy-MM-dd HH:mm:ss'))")
    $lines.Add("Configuration: $Configuration")
    $lines.Add("Runtime: $Runtime")
    $lines.Add("")
    $lines.Add("Published Application:")
    $lines.Add("- $appExe")
    $lines.Add("")
    $lines.Add("Installers:")

    foreach ($installer in $BuiltInstallers) {
        $lines.Add("- $($installer.Name): $($installer.Path)")
        if (Test-Path $installer.Path) {
            $hash = (Get-FileHash -Path $installer.Path -Algorithm SHA256).Hash
            $sizeMb = [math]::Round(((Get-Item $installer.Path).Length / 1MB), 2)
            $lines.Add("  SHA256: $hash")
            $lines.Add("  SizeMB: $sizeMb")
        }
    }

    Set-Content -Path $manifestPath -Value $lines -Encoding UTF8
    Write-OK "Release manifest written: $manifestPath"
}

function Find-ISCC {
    if ($IsccPath -and (Test-Path $IsccPath)) { return $IsccPath }
    $paths = @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    )
    foreach ($p in $paths) { if (Test-Path $p) { return $p } }
    return ""
}

function Remove-StaleInstallers {
    $patterns = @(
        "RobovAI-PRO-POS-Setup-v5.0-*.exe",
        "RobovAI-PRO-POS-Platinum-Setup-v5.0.exe",
        "FINAL_RELEASE_MANIFEST_v5.0.txt"
    )

    foreach ($pattern in $patterns) {
        Get-ChildItem -Path $outputDir -Filter $pattern -ErrorAction SilentlyContinue |
            Remove-Item -Force -ErrorAction SilentlyContinue
    }
}

function Remove-StaleRuntimeSurfaces {
    $legacyPaths = @(
        (Join-Path $repoRoot "publish\SmartPOS.WPF.deps.json"),
        (Join-Path $repoRoot "publish\SmartPOS.WPF.runtimeconfig.json"),
        (Join-Path $repoRoot "publish\SmartPOS.WPF.dll"),
        (Join-Path $repoRoot "publish\SmartPOS.WPF.exe"),
        (Join-Path $repoRoot "installer\SmartPOS-v5.0-Package"),
        (Join-Path $repoRoot "installer\SmartPOS-v2.0-Package")
    )

    foreach ($legacyPath in $legacyPaths) {
        if (Test-Path $legacyPath) {
            Remove-Item $legacyPath -Recurse -Force -ErrorAction SilentlyContinue
            Write-OK "Removed stale runtime surface: $legacyPath"
        }
    }
}

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Magenta
Write-Host "   RoboVAI PRO POS - Platinum Edition Build Script v5.0   " -ForegroundColor Magenta
Write-Host "   Enterprise Packaging Pipeline                          " -ForegroundColor Magenta
Write-Host "   Copyright 2026 RoboVAI Solutions | robovai.tech        " -ForegroundColor Magenta
Write-Host "==========================================================" -ForegroundColor Magenta
Write-Host ""

Write-Step "Step 1: Check Certificate"
$cert = $null
$pfxPath = Resolve-CertificatePath
$resolvedCertPassword = Get-CertificatePassword
if ($pfxPath) {
    try {
        $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($pfxPath, $resolvedCertPassword)
        Write-OK "Certificate loaded: $($cert.Subject)"
        Write-OK "Thumbprint: $($cert.Thumbprint)"
    } catch {
        Write-Warn "Found PFX but failed to load. Check password or certificate permissions."
    }
} else {
    Write-Warn "No PFX file found in installer certificate paths. Signing will be skipped unless -CertThumbprint is provided."
    if ($CertThumbprint) {
        $cert = Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.Thumbprint -eq $CertThumbprint }
        if (-not $cert) { throw "Cert with thumbprint $CertThumbprint not found in Personal store." }
        Write-OK "Using store certificate: $($cert.Subject)"
        Write-OK "Thumbprint: $($cert.Thumbprint)"
    }
}

Write-Step "Step 2: Publish app"
Remove-StaleRuntimeSurfaces
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue }
$publishArgs = @(
    $projectPath, '-c', $Configuration, '-r', $Runtime,
    '--self-contained', 'true',
    '-p:PublishSingleFile=false',
    '-p:UseSharedCompilation=false',
    '-p:AssemblyVersion=5.0.0.0',
    '-p:FileVersion=5.0.0.0',
    '-o', $publishDir
)
Invoke-Checked -Tool 'dotnet' -Params (@('publish') + $publishArgs)
Write-OK "Published to: $publishDir"

$missing = @($appExe) | Where-Object { -not (Test-Path $_) }
if (@($missing).Count -gt 0) { throw "Missing: $($missing -join ', ')" }

Write-Step "Step 2.5: Generate Assets & Copy Offline Activation"
# 1. README.txt
$readmePath = Join-Path -Path $publishDir -ChildPath "README.txt"
$readmeContent = @"
==========================================================
    RoboVAI PRO POS - Platinum Edition v5.0
==========================================================

شكراً لاختيارك RoboVAI PRO POS.
هذه الحزمة مخصصة للتشغيل المؤسسي على بيئة Windows 10 أو أحدث.

[ بيانات الدخول الافتراضية ]
اسم المستخدم: admin
كلمة المرور: admin@2026

[ ملاحظات تشغيل مهمة ]
- يفضل تغيير كلمة مرور المستخدم admin فور أول تشغيل.
- يتم حفظ بيانات التطبيق محلياً ضمن: %LocalAppData%\RoboVAI\SmartPOS\
- يوصى بتفعيل نسخ احتياطي يومي لقاعدة البيانات قبل التشغيل الفعلي.

[ دعم فني ]
الهاتف / واتساب: +20 112 189 1913
البريد الإلكتروني: RoboVAISolutions@gmail.com
الموقع الرسمي: https://robovai.tech

مع تحيات فريق RoboVAI Solutions
"@
Set-Content -Path $readmePath -Value $readmeContent -Encoding UTF8

# 2. تشغيل البرنامج.bat
$batName = "تشغيل البرنامج.bat"
$batPath = Join-Path -Path $publishDir -ChildPath $batName
$batContent = "@echo off`r`ntitle RoboVAI PRO POS Platinum`r`necho Starting System as Administrator...`r`ncd /d `"%~dp0`"`r`nstart `"`" `"SmartPOS.WPF.exe`"`r`nexit"
Set-Content -Path $batPath -Value $batContent -Encoding UTF8

# 3. Copy Assets if available
$assetSources = @(
    @{ Source = (Join-Path $repoRoot "assets"); Destination = (Join-Path $publishDir "assets") },
    @{ Source = (Join-Path $repoRoot "Assets\Images"); Destination = (Join-Path $publishDir "Assets\Images") }
)

foreach ($assetMap in $assetSources) {
    if (Test-Path $assetMap.Source) {
        if (-not (Test-Path $assetMap.Destination)) { New-Item $assetMap.Destination -ItemType Directory -Force | Out-Null }
        Copy-Item -Path (Join-Path $assetMap.Source "*") -Destination $assetMap.Destination -Recurse -Force
        Write-OK "Copied assets from: $($assetMap.Source)"
    }
}

# 4. Generate EULA for Installer
$eulaPath = Join-Path $repoRoot "installer\EULA.txt"
$eulaContent = @"
اتفاقية ترخيص الاستخدام (EULA)
=====================================
برنامج: RoboVAI PRO POS - Platinum Edition
النسخة: 5.0
الناشر: RoboVAI Solutions

1. شروط الاستخدام الأساسية:
يُسمح باستخدام هذه النسخة للعميل الذي يمتلك ترخيصاً سارياً (لمدة 10 سنوات).
غير مسموح بفك تشفير أو الهندسة العكسية لأي من ملفات النظام.

2. الدعم الفني:
يحق للعميل الحصول على الدعم الفني عبر القنوات الرسمية.

3. المسؤولية:
RoboVAI Solutions غير مسؤولة عن فقدان البيانات الناتج عن سوء استخدام النظام وتجاهل تعليمات النسخ الاحتياطي.

4. حماية البيانات:
العميل مسؤول عن تأمين النسخ الاحتياطية الدورية وإدارة صلاحيات المستخدمين داخل بيئة التشغيل.

بمجرد استمرارك في التثبيت، فأنت توافق تماماً على شروط هذه الاتفاقية.
"@
Set-Content -Path $eulaPath -Value $eulaContent -Encoding UTF8
Write-OK "Generated EULA.txt"

if (-not $SkipSigning -and $cert) {
    Write-Step "Step 3: Sign SmartPOS.WPF.exe"
    $st = Find-SignTool
    if ($st) {
        $args3 = @('sign','/sha1',$cert.Thumbprint,'/fd','sha256','/td','sha256',
                   '/tr','http://timestamp.digicert.com',
                   '/d','RoboVAI PRO POS Platinum','/du','https://robovai.tech',$appExe)
        Invoke-Checked -Tool $st -Params $args3
        Write-OK "Signed successfully"
    } else {
        Write-Warn "signtool.exe not found. Skipping signing."
    }
}

if (-not $SkipInstaller) {
    Write-Step "Step 4: Inno Setup (Building Final Variants)"
    $iscc = Find-ISCC
    if ($iscc) {
        if (-not (Test-Path $outputDir)) { New-Item $outputDir -ItemType Directory }
        Remove-StaleInstallers
        $builtInstallers = @()

        $versions = @(
            @{
                Name = "Platinum"
                BaseFilename = "RobovAI-PRO-POS-Platinum-Setup-v5.0"
                SmallImage = "wizard_small.bmp"
                WelcomeMsg = "أهلاً بك في منصة RoboVAI PRO POS البلاتينية المتطورة."
                EditionTitle = "Platinum Enterprise Edition"
                Mark1 = "الإصدار المؤسسي الكامل الجاهز للتسليم والتشغيل."
                Mark2 = "حزمة نهائية تحتوي على آخر إصلاحات الاستقرار."
                Mark3 = "مناسبة للتسليم الرسمي للعميل النهائي."
            },
            @{
                Name = "Standard"
                BaseFilename = "RobovAI-PRO-POS-Setup-v5.0-Standard"
                SmallImage = "wizard_small.bmp"
                WelcomeMsg = "أهلاً بك في منصة RoboVAI PRO POS المتطورة."
                EditionTitle = "Enterprise Standard Edition"
                Mark1 = "إدارة ذكية وشاملة للمبيعات والمخزون."
                Mark2 = "تقارير دقيقة ومتابعة لحظية للأرباح."
                Mark3 = "واجهة عملية وسريعة تناسب بيئات التشغيل اليومية."
            },
            @{
                Name = "Kaf5"
                BaseFilename = "RobovAI-PRO-POS-Setup-v5.0-Kaf5"
                SmallImage = "wizard_small_kaf5.bmp"
                WelcomeMsg = "أهلاً بك في منصة RoboVAI PRO POS المتطورة."
                EditionTitle = "Honorary Kaf5 Edition"
                Mark1 = "إهداء خاص وتكريم إلى:"
                Mark2 = "المقدم / هشام عطية حلمي"
                Mark3 = "قائد الكتيبة الخامسة مهندسي مطارات"
            }
        )

        foreach ($ver in $versions) {
            Write-OK "Building version: $($ver.Name)"
            $configContent = @"
#define MyOutputBaseFilename `"$($ver.BaseFilename)`"
#define MySmallImageFile `"$($ver.SmallImage)`"
#define WelcomeMsg `"$($ver.WelcomeMsg)`"
#define EditionTitle `"$($ver.EditionTitle)`"
#define MarketingLine1 `"$($ver.Mark1)`"
#define MarketingLine2 `"$($ver.Mark2)`"
#define MarketingLine3 `"$($ver.Mark3)`"
#define SupportInfo `"الدعم الفني: +20 112 189 1913`"
#define CompanyLegal `"RoboVAI Solutions - Enterprise Partner`"
"@
            Set-Content -Path (Join-Path $repoRoot "installer\config.iss") -Value $configContent -Encoding UTF8

            Invoke-Checked -Tool $iscc -Params @($issPath)

            $currentSetupExe = Join-Path $outputDir "$($ver.BaseFilename).exe"
            Write-OK "Installer created: $($currentSetupExe)"
            $builtInstallers += @{ Name = $ver.Name; Path = $currentSetupExe }

            if (-not $SkipSigning -and $cert -and (Test-Path $currentSetupExe)) {
                $st = Find-SignTool
                if ($st) {
                    $args5 = @('sign','/sha1',$cert.Thumbprint,'/fd','sha256','/td','sha256',
                               '/tr','http://timestamp.digicert.com',
                               '/d','RoboVAI PRO POS Platinum Setup','/du','https://robovai.tech',$currentSetupExe)
                    Invoke-Checked -Tool $st -Params $args5
                    Write-OK "Installer signed successfully ($($ver.Name))"
                }
            }
        }

        Write-ReleaseManifest -BuiltInstallers $builtInstallers
    } else {
        Write-Warn "ISCC.exe not found. Skipping installer generation."
    }
}

Write-Host "`n=== MULTI-VERSION BUILD COMPLETE - v5.0 ===" -ForegroundColor Green
Write-Host "`n   Check Output Folder for Setup files." -ForegroundColor Cyan
Write-Host "`n   RoboVAI Solutions | robovai.tech | +20 112 189 1913`n" -ForegroundColor Gray
