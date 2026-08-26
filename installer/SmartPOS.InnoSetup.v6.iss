#define MyAppName "RobovAI PRO POS"
#define MyAppVersion "6.0"
#define MyAppPublisher "RoboVAI Solutions"
#define MyAppURL "https://robovai.tech"
#define MyAppExeName "SmartPOS.WPF.exe"
#define MyAppContact "info@robovai.tech"

; Dynamic config included from powershell build script
#include "config.iss"

[Setup]
AppId={{9F8D7E6C-5B4A-3210-A1B2-C3D4E5F6A7B9}
AppName={#MyAppName}
AppVerName={#MyAppName} {#MyAppVersion}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppContact={#MyAppContact}
AppComments=Enterprise offline-ready POS installer - v6.0 AI Edition
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
WizardImageBackColor=$0d1117
WizardImageFile=wizard.bmp
WizardSmallImageFile={#MySmallImageFile}
SetupIconFile=..\assets\app_icon.ico
LicenseFile=EULA.txt

; UI Options
DisableDirPage=auto
DisableProgramGroupPage=no
DisableReadyMemo=no
DisableWelcomePage=no
ShowLanguageDialog=auto

; Compression - Maximum
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes

; Version metadata
VersionInfoVersion={#MyAppVersion}.0.0
VersionInfoTextVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoDescription={#MyAppName} v6.0 - AI-Powered Enterprise POS
VersionInfoCopyright=Copyright (c) 2026 RoboVAI Solutions
;SignTool=signtool

PrivilegesRequired=admin
CloseApplications=yes

[Languages]
Name: "arabic";  MessagesFile: "compiler:Languages\Arabic.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

; ─── Bilingual Custom Messages ───────────────────────────────────────────────
[CustomMessages]
arabic.cm_FullInstall=تثبيت كامل (ينصح به)
arabic.cm_BasicInstall=تثبيت أساسي
arabic.cm_CustomInstall=تثبيت مخصص
arabic.cm_MainFiles=ملفات النظام الأساسية (إلزامي)
arabic.cm_DocsFiles=ملفات المساعدة وأدلة التشغيل
arabic.cm_AssetsFiles=الصور والملحقات الإضافية للبرنامج
arabic.cm_DesktopGrp=اختصارات سطح المكتب:
arabic.cm_StartupGrp=إعدادات التشغيل:
arabic.cm_QuickStart=إنشاء اختصار للتشغيل السريع بصلاحيات المسؤول (موصى به)
arabic.cm_AutoStart=تشغيل البرنامج تلقائياً عند إقلاع الويندوز
arabic.cm_RunNow=تشغيل النظام الآن (صلاحيات المسؤول)
arabic.cm_ReleaseNotes=مميزات الإصدار 6.0:%n%n✓ ذكاء اصطناعي للتنبؤ بالمخزون%n✓ نظام تأجير الأجهزة%n✓ وجبات الموظفين%n✓ نقاط ولاء العملاء (Bronze/Silver/Gold)%n✓ صلاحيات متدرجة (RBAC) + Audit Log%n✓ تقارير PDF/Excel%n✓ تحسينات جوهرية في الأداء والاستقرار
arabic.cm_BackupDone=تم أخذ نسخة احتياطية من قاعدة البيانات السابقة تلقائياً.

english.cm_FullInstall=Full Install (Recommended)
english.cm_BasicInstall=Basic Install
english.cm_CustomInstall=Custom Install
english.cm_MainFiles=Core System Files (Required)
english.cm_DocsFiles=Help Files and User Guides
english.cm_AssetsFiles=Images and Additional Assets
english.cm_DesktopGrp=Desktop Shortcuts:
english.cm_StartupGrp=Startup Settings:
english.cm_QuickStart=Create Quick Launch shortcut with Admin privileges (Recommended)
english.cm_AutoStart=Auto-start program on Windows startup
english.cm_RunNow=Launch the system now (Admin privileges)
english.cm_ReleaseNotes=Version 6.0 Features:%n%n✓ AI Stock Predictor%n✓ Rental Device Management%n✓ Staff Meals System%n✓ Customer Loyalty (Bronze/Silver/Gold)%n✓ Role-Based Access Control + Audit Log%n✓ PDF/Excel Reports%n✓ Core Performance & Stability Improvements
english.cm_BackupDone=A backup of the previous database was automatically created.

arabic.Platinum_EditionTitle=الإصدار البلاتيني المتقدم v6.0
arabic.Platinum_Welcome=أهلاً بك في نظام الكاشير الذكي البلاتيني
arabic.Platinum_Mark1=نسخة المؤسسات مع تنبؤ المخزون بالذكاء الاصطناعي
arabic.Platinum_Mark2=أداء فائق واستقرار لا مثيل له
arabic.Platinum_Mark3=جاهز للتشغيل التجاري

english.Platinum_EditionTitle=Platinum Enterprise Edition v6.0
english.Platinum_Welcome=Welcome to RoboVAI PRO POS Platinum v6.0 AI Edition
english.Platinum_Mark1=Full enterprise with AI Stock Predictor
english.Platinum_Mark2=Latest stability and performance improvements
english.Platinum_Mark3=Ready for official client delivery

arabic.Standard_EditionTitle=الإصدار التجاري القياسي v6.0 (Commercial Standard)
arabic.Standard_Welcome=مرحباً بك في نظام RobovAI PRO POS المساعد الذكي لإدارة النشاط التجاري
arabic.Standard_Mark1=نظام نقاط البيع الذهبي المتكامل مع إدارة المخزن المحمول (WMS)
arabic.Standard_Mark2=دعم كامل للعمل أوفلاين 100% بدون إنترنت
arabic.Standard_Mark3=لوحة تحكم تفاعلية ومتوافقة مع جميع متطلبات التاجر العربي

english.Standard_EditionTitle=Commercial Standard Edition v6.0
english.Standard_Welcome=Welcome to RoboVAI PRO POS - Commercial General Edition
english.Standard_Mark1=Full POS system integrated with mobile WMS inventory
english.Standard_Mark2=100% offline-first architecture for ultimate reliability
english.Standard_Mark3=Interactive ERP dashboard and multi-currency support

arabic.Kaf5_EditionTitle=✦ الإصدار التكريمي الخاص — Kaf5 Edition ✦
arabic.Kaf5_Welcome=بسم الله الرحمن الرحيم
arabic.Kaf5_Verse="وَقُلِ اعْمَلُوا فَسَيَرَى اللَّهُ عَمَلَكُمْ وَرَسُولُهُ وَالْمُؤْمِنُونَ"
arabic.Kaf5_Dedication=إهداء فخر واعتزاز
arabic.Kaf5_Mark1=يُهدى هذا النظام التقني بكل تقدير واحترام إلى:
arabic.Kaf5_Mark2=السيد المقدم / هشام عطية حلمي
arabic.Kaf5_Mark3=قائد الكتيبة الخامسة مهندسي مطارات
arabic.Kaf5_Mark4=شكراً لإيمانكم بالتطوير والتحديث. هذا النظام هو انعكاس لثقتكم الغالية%nوتوجيهاتكم السديدة.
arabic.Kaf5_Mark5=مع خالص الأمنيات بدوام التوفيق ومزيد من النجاح والتميز.

english.Kaf5_EditionTitle=✦ Honorary Kaf5 Edition — Dedicated v6.0 ✦
english.Kaf5_Welcome=In the name of God, the Most Gracious, the Most Merciful
english.Kaf5_Dedication=A Token of Appreciation
english.Kaf5_Mark1=This system is proudly dedicated to:
english.Kaf5_Mark2=Major Hisham Attia Helmy
english.Kaf5_Mark3=Commander - 5th Airport Engineers Battalion
english.Kaf5_Mark4=Thank you for your trust and gracious collaboration
english.Kaf5_Mark5=May this system serve you well on your blessed journey

arabic.SupportInfo=الموقع الرسمي: https://robovai.tech  | الدعم الفني: +20 112 189 1913
english.SupportInfo=Website: https://robovai.tech  | Support: +20 112 189 1913
arabic.RoboData=ملاحظة: بيانات التطبيق تحفظ في %LocalAppData%\SmartPOS\%nينصح بأخذ نسخة احتياطية قبل بدء الاستخدام التجاري.
english.RoboData=Note: App data is stored in %LocalAppData%\SmartPOS\%nWe recommend a backup before going live.
arabic.SetupDesc=حزمة تثبيت مؤسسية مهيأة للتشغيل المحلي والربط ببيانات الفرع.
english.SetupDesc=Enterprise installation package configured for local operation and branch data connectivity.
arabic.FinDesc=تم التثبيت بنجاح.%n%n%nقم بزيارة موقعنا https://robovai.tech للاطلاع على آخر التحديثات والتوثيق.
english.FinDesc=Installation completed successfully.%n%n%nVisit https://robovai.tech for documentation and updates.


[Types]
Name: "full";    Description: "{cm:cm_FullInstall}"
Name: "compact"; Description: "{cm:cm_BasicInstall}"
Name: "custom";  Description: "{cm:cm_CustomInstall}"; Flags: iscustom

[Components]
Name: "main";   Description: "{cm:cm_MainFiles}";   Types: full compact custom; Flags: fixed
Name: "docs";   Description: "{cm:cm_DocsFiles}";   Types: full
Name: "assets"; Description: "{cm:cm_AssetsFiles}"; Types: full

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}";  GroupDescription: "{cm:cm_DesktopGrp}"; Flags: unchecked
Name: "quickstart";  Description: "{cm:cm_QuickStart}";      GroupDescription: "{cm:cm_DesktopGrp}"
Name: "autostart";   Description: "{cm:cm_AutoStart}";        GroupDescription: "{cm:cm_StartupGrp}"; Flags: unchecked

[Files]
Source: "..\publish\final-exe\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion; Components: main
Source: "..\publish\final-exe\README.txt";       DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist; Components: docs
Source: "..\publish\final-exe\Assets\*";         DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist; Components: assets
Source: "..\\publish\\final-exe\*"; DestDir: "{app}"; Excludes: "README.txt,Assets\*,*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs; Components: main
Source: "..\publish\final-exe\RoboVAI-Launch.vbs"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist; Components: main
Source: "SmartPOS_Demo.db"; DestDir: "{app}"; Flags: ignoreversion; Components: main

[Icons]
Name: "{group}\{#MyAppName}";                Filename: "{app}\{#MyAppExeName}"; Components: main
Name: "{autodesktop}\{#MyAppName}";          Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{group}\RoboVAI Quick Launch";        Filename: "{app}\RoboVAI-Launch.vbs"; IconFilename: "{app}\{#MyAppExeName}"; Components: main
Name: "{autodesktop}\RoboVAI Quick Launch";  Filename: "{app}\RoboVAI-Launch.vbs"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: quickstart

[Registry]
Root: HKA; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\RoboVAI-Launch.vbs"""; Tasks: autostart; Flags: uninsdeletevalue

[Run]
Filename: "netsh"; Parameters: "http add urlacl url=http://+:7890/ user=Everyone"; Flags: runhidden
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""RoboVAI POS LAN Server (7890)"" dir=in action=allow protocol=TCP localport=7890"; Flags: runhidden
Filename: "{app}\RoboVAI-Launch.vbs"; Description: "{cm:cm_RunNow}"; Flags: shellexec nowait postinstall skipifsilent
Filename: "https://drive.google.com/drive/folders/1qnQpUumI5gnv6muZLrPELVraOPGMDXlX?usp=sharing"; Description: "📁 فتح مجلد التحديثات والتحميلات على Google Drive"; Flags: shellexec runasoriginaluser postinstall unchecked
Filename: "https://robovai.tech"; Description: "🌐 زيارة موقعنا الرسمي (https://robovai.tech)"; Flags: shellexec runasoriginaluser postinstall unchecked
Filename: "{app}\wms\user-guide.html"; Description: "📖 فتح دليل التشغيل والاستخدام الشامل المدمج (Offline v6.0)"; Flags: shellexec runasoriginaluser postinstall unchecked

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  dbPath, backupPath, ts: String;
begin
  NeedsRestart := False;
  dbPath := ExpandConstant('{localappdata}') + '\RoboVAI\SmartPOS\smartpos.db';
  if FileExists(dbPath) then
  begin
    ts := GetDateTimeString('yyyymmdd_hhnnss', '-', '-');
    backupPath := dbPath + '.bak_' + ts;
    if FileCopy(dbPath, backupPath, False) then
    begin
      if ActiveLanguage = 'arabic' then
        MsgBox(CustomMessage('cm_BackupDone') + #13#10 + backupPath, mbInformation, MB_OK)
      else
        MsgBox(CustomMessage('cm_BackupDone') + #13#10 + backupPath, mbInformation, MB_OK);
    end;
  end;
  Result := '';
end;

function IsDotNet8Installed(): Boolean;
var
  key: String;
begin
  key := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost';
  Result := RegKeyExists(HKLM, key) or
            RegKeyExists(HKLM, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{BEB5F0CB-5A1A-4B8F-B9C3-5F2CDDA38A6D}');
end;

function InitializeSetup(): Boolean;
var
  IsAr: Boolean;
begin
  IsAr := (GetUILanguage = $0401); // Arabic locale code
  Result := True;

  // Require Windows 10 1809+ (build 17763)
  if not (GetWindowsVersion >= $0A000000) then
  begin
    if IsAr then
      MsgBox('هذا البرنامج يتطلب Windows 10 (1809) أو أحدث.', mbError, MB_OK)
    else
      MsgBox('This application requires Windows 10 (1809) or later.', mbError, MB_OK);
    Result := False;
    Exit;
  end;
end;


procedure InitializeWizard();
var
  IsAr: Boolean;
begin
  IsAr := (ActiveLanguage = 'arabic');

  WizardForm.Color := $F8F8F8;

  WizardForm.PageNameLabel.Font.Color := $1A1A2E;
  WizardForm.PageNameLabel.Font.Size  := 13;
  WizardForm.PageNameLabel.Font.Style := [fsBold];

  WizardForm.PageDescriptionLabel.Font.Color := $444444;
  WizardForm.PageDescriptionLabel.Font.Size  := 9;

  WizardForm.WelcomeLabel1.Font.Color := $C47000;  // Golden professional
  WizardForm.WelcomeLabel1.Font.Size  := 16;
  WizardForm.WelcomeLabel1.Font.Style := [fsBold];

  WizardForm.WelcomeLabel2.Font.Color := $2C2C2C;
  WizardForm.WelcomeLabel2.Font.Size  := 10;

  WizardForm.FinishedHeadingLabel.Font.Color := $C47000;
  WizardForm.FinishedHeadingLabel.Font.Size  := 16;
  WizardForm.FinishedHeadingLabel.Font.Style := [fsBold];

  WizardForm.FinishedLabel.Font.Color := $2C2C2C;
  WizardForm.FinishedLabel.Font.Size  := 10;

  WizardForm.WizardBitmapImage.Width  := 200;
  WizardForm.WizardBitmapImage2.Width := 200;

  WizardForm.WelcomeLabel1.Left        := 220;
  WizardForm.WelcomeLabel2.Left        := 220;
  WizardForm.FinishedHeadingLabel.Left := 220;
  WizardForm.FinishedLabel.Left        := 220;

  WizardForm.PageDescriptionLabel.Caption := CustomMessage('SetupDesc');
  WizardForm.FinishedLabel.Caption :=
    CustomMessage('FinDesc') + #13#10 + #13#10 +
    CustomMessage('cm_ReleaseNotes');

  if ('{#EditionName}' = 'Kaf5') or ('{#EditionName}' = '"Kaf5"') then
  begin
    WizardForm.WelcomeLabel1.Caption := '{#MyAppName} - ' + CustomMessage('Kaf5_EditionTitle');
    if IsAr then
    begin
      WizardForm.WelcomeLabel2.Caption :=
        CustomMessage('Kaf5_Welcome') + #13#10 +
        '  ' + CustomMessage('Kaf5_Verse') + #13#10 +
        '═══════════════════════════════' + #13#10 +
        '                 ' + CustomMessage('Kaf5_Dedication') + #13#10 +
        '═══════════════════════════════' + #13#10 + #13#10 +
        CustomMessage('Kaf5_Mark1') + #13#10 + #13#10 +
        '★ ' + CustomMessage('Kaf5_Mark2') + ' ★' + #13#10 +
        '         ' + CustomMessage('Kaf5_Mark3') + #13#10 + #13#10 +
        CustomMessage('Kaf5_Mark4') + #13#10 +
        '      ' + CustomMessage('Kaf5_Mark5') + #13#10 + #13#10 +
        '═══════════════════════════════';
    end
    else
    begin
      WizardForm.WelcomeLabel2.Caption :=
        CustomMessage('Kaf5_Welcome') + #13#10 + #13#10 +
        '═══════════════════════════' + #13#10 +
        '    ' + CustomMessage('Kaf5_Dedication') + #13#10 +
        '═══════════════════════════' + #13#10 + #13#10 +
        CustomMessage('Kaf5_Mark1') + #13#10 + #13#10 +
        '  ★  ' + CustomMessage('Kaf5_Mark2') + '  ★' + #13#10 +
        '       ' + CustomMessage('Kaf5_Mark3') + #13#10 + #13#10 +
        CustomMessage('Kaf5_Mark4') + #13#10 +
        CustomMessage('Kaf5_Mark5') + #13#10 + #13#10 +
        '═══════════════════════════';
    end;
  end
  else
  begin
    WizardForm.WelcomeLabel1.Caption := '{#MyAppName} - ' + CustomMessage('Standard_EditionTitle');
    WizardForm.WelcomeLabel2.Caption :=
      CustomMessage('Standard_Welcome') + #13#10 + #13#10 +
      CustomMessage('Standard_Mark1') + #13#10 +
      CustomMessage('Standard_Mark2') + #13#10 +
      CustomMessage('Standard_Mark3') + #13#10 + #13#10 +
      CustomMessage('SupportInfo') + #13#10 +
      '{#MyAppContact}' + #13#10 +
      '{#MyAppURL}' + #13#10 + #13#10 +
      CustomMessage('RoboData');
  end;
end;

procedure CurInstallProgressChanged(CurProgress, MaxProgress: Integer);
begin
  if MaxProgress > 0 then
    WizardForm.StatusLabel.Caption :=
      IntToStr(CurProgress * 100 div MaxProgress) + '%  -  RoboVAI PRO POS v6.0';
end;

