#define MyAppName "RobovAI PRO POS"
#define MyAppVersion "5.0"
#define MyAppPublisher "RoboVAI Solutions"
#define MyAppURL "https://robovai.tech"
#define MyAppExeName "SmartPOS.WPF.exe"
#define MyAppContact "RoboVAISolutions@gmail.com"

; Dynamic config included from powershell build script
#include "config.iss"

[Setup]
; App Metadata
AppId={{9F8D7E6C-5B4A-3210-A1B2-C3D4E5F6A7B8}
AppName={#MyAppName}
AppVerName={#MyAppName} {#MyAppVersion}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppContact={#MyAppContact}
AppComments=Enterprise offline-ready POS installer
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName} {#MyAppVersion}
OutputDir=Output
OutputBaseFilename={#MyOutputBaseFilename}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DirExistsWarning=yes
UsePreviousAppDir=yes
UsePreviousGroup=yes
SetupLogging=yes

; Installer appearance
WizardStyle=modern
WizardSizePercent=115,115
WizardImageStretch=yes
WizardImageBackColor=$1c1c1c
WizardImageFile=wizard.bmp
WizardSmallImageFile={#MySmallImageFile}
SetupIconFile=..\assets\app_icon.ico
LicenseFile=EULA.txt

; UI Options
DisableDirPage=auto
DisableProgramGroupPage=no
DisableReadyMemo=no
DisableWelcomePage=no
ShowLanguageDialog=yes

; Compression
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes

; Version metadata & Code Signing info display
VersionInfoVersion={#MyAppVersion}.0.0
VersionInfoTextVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoProductName={#MyAppName} {#EditionTitle}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoDescription={#MyAppName} Platinum — 10-Year Enterprise License
VersionInfoCopyright=Copyright © 2026 RoboVAI Solutions - Platinum Certified
;SignTool=signtool

PrivilegesRequired=admin
CloseApplications=yes

[Languages]

Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Types]
Name: "full"; Description: "تثبيت كامل (ينصح به)"
Name: "compact"; Description: "تثبيت أساسي"
Name: "custom"; Description: "تثبيت مخصص"; Flags: iscustom

[Components]
Name: "main"; Description: "ملفات النظام الأساسية (إلزامي)"; Types: full compact custom; Flags: fixed
Name: "docs"; Description: "ملفات المساعدة وأدلة التشغيل"; Types: full
Name: "assets"; Description: "الصور والملحقات الإضافية للبرنامج"; Types: full

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "اختصارات سطح المكتب:"; Flags: unchecked
Name: "quickstart"; Description: "إنشاء اختصار للتشغيل السريع بصلاحيات المسؤول (موصى به)"; GroupDescription: "اختصارات سطح المكتب:";
Name: "autostart"; Description: "تشغيل البرنامج تلقائياً عند إقلاع الويندوز (Auto-Start)"; GroupDescription: "إعدادات التشغيل:"; Flags: unchecked

[Files]
; Main Executable and dependencies from publish folder
Source: "..\publish\final-exe\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion; Components: main
Source: "..\publish\final-exe\README.txt"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist; Components: docs
Source: "..\publish\final-exe\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist; Components: assets
Source: "..\publish\final-exe\*"; DestDir: "{app}"; Excludes: "README.txt, Assets\*"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: main

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Components: main
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{group}\تشغيل الكاشير السريع"; Filename: "{app}\تشغيل البرنامج.bat"; IconFilename: "{app}\{#MyAppExeName}"; Components: main
Name: "{autodesktop}\تشغيل الكاشير السريع"; Filename: "{app}\تشغيل البرنامج.bat"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: quickstart

[Registry]
; Auto-start setting using Windows Run registry key
Root: HKA; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\تشغيل البرنامج.bat"""; Tasks: autostart; Flags: uninsdeletevalue

[Run]
Filename: "{app}\تشغيل البرنامج.bat"; Description: "تشغيل النظام الآن (صلاحيات المسؤول)"; Flags: nowait postinstall skipifsilent

[Code]
procedure InitializeWizard();
begin
  WizardForm.Color := clWhite;

  WizardForm.PageNameLabel.Font.Color := clBlack;
  WizardForm.PageNameLabel.Font.Size := 13;
  WizardForm.PageNameLabel.Font.Style := [fsBold];

  WizardForm.PageDescriptionLabel.Font.Color := clBlack;
  WizardForm.PageDescriptionLabel.Font.Size := 9;

  WizardForm.WelcomeLabel1.Font.Color := $7A4B00;
  WizardForm.WelcomeLabel1.Font.Size := 16;
  WizardForm.WelcomeLabel1.Font.Style := [fsBold];

  WizardForm.WelcomeLabel2.Font.Color := $333333;
  WizardForm.WelcomeLabel2.Font.Size := 10;

  WizardForm.FinishedHeadingLabel.Font.Color := $7A4B00;
  WizardForm.FinishedHeadingLabel.Font.Size := 16;
  WizardForm.FinishedHeadingLabel.Font.Style := [fsBold];

  WizardForm.FinishedLabel.Font.Color := $333333;
  WizardForm.FinishedLabel.Font.Size := 10;

  WizardForm.WizardBitmapImage.Width := 200;
  WizardForm.WizardBitmapImage2.Width := 200;

  WizardForm.WelcomeLabel1.Left := 220;
  WizardForm.WelcomeLabel2.Left := 220;
  WizardForm.FinishedHeadingLabel.Left := 220;
  WizardForm.FinishedLabel.Left := 220;
  WizardForm.PageDescriptionLabel.Caption := 'حزمة تثبيت مؤسسية مهيأة للتشغيل المحلي والربط ببيانات الفرع.';
  WizardForm.WelcomeLabel1.Caption := '{#MyAppName} - {#EditionTitle}';
  WizardForm.FinishedLabel.Caption :=
    'اكتمل التثبيت بنجاح.' + #13#10 +
    'ينصح بمراجعة الإعدادات وكلمة مرور المدير ومسار النسخ الاحتياطي قبل التشغيل التجاري.';

  WizardForm.WelcomeLabel2.Caption :=
     '{#WelcomeMsg}' + #13#10 + #13#10 +
     '{#MarketingLine1}' + #13#10 +
     '{#MarketingLine2}' + #13#10 +
     '{#MarketingLine3}' + #13#10 + #13#10 +
     '{#SupportInfo}' + #13#10 +
     '{#MyAppContact}' + #13#10 +
     '{#MyAppURL}' + #13#10 + #13#10 +
    'ملاحظة تشغيل: بيانات التطبيق المحلية تحفظ داخل %LocalAppData%\SmartPOS\' + #13#10 +
    'ينصح بأخذ نسخة احتياطية قبل بدء الاستخدام التجاري.';
end;
