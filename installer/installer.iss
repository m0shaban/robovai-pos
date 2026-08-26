[Setup]
; Basic Information
AppName=RobovAI PRO POS
AppVersion=5.0.0
AppPublisher=RobovAI Solutions Inc.
AppPublisherURL=https://robovai.tech/
AppSupportURL=https://robovai.tech/
AppUpdatesURL=https://robovai.tech/
AppCopyright=Copyright (c) 2026 RobovAI Solutions Inc. All rights reserved.

; Output settings
DefaultDirName={autopf}\RobovAI PRO POS
DefaultGroupName=RobovAI PRO POS
AllowNoIcons=yes
OutputDir=f:\Raw\kasher\kasher\Output
OutputBaseFilename=RobovAI_PRO_POS_Setup_v5.0
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

; Icons and Wizard Branding (uses promo images)
SetupIconFile=f:\Raw\kasher\kasher\favicon.ico
UninstallDisplayIcon={app}\SmartPOS.WPF.exe
WizardStyle=modern
WizardImageFile=f:\Raw\kasher\kasher\installer\wizard.bmp
WizardSmallImageFile=f:\Raw\kasher\kasher\installer\wizard_small.bmp

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; The main executable
Source: "f:\Raw\kasher\kasher\publish\SmartPOS.WPF.exe"; DestDir: "{app}"; Flags: ignoreversion
; Settings file (preserve user settings during upgrades)
Source: "f:\Raw\kasher\kasher\publish\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion onlyifdoesntexist
; All other publish files
Source: "f:\Raw\kasher\kasher\publish\*"; DestDir: "{app}"; Excludes: "SmartPOS.WPF.exe,appsettings.json"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\RobovAI PRO POS"; Filename: "{app}\SmartPOS.WPF.exe"
Name: "{group}\{cm:UninstallProgram,RobovAI PRO POS}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\RobovAI PRO POS"; Filename: "{app}\SmartPOS.WPF.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\SmartPOS.WPF.exe"; Description: "{cm:LaunchProgram,RobovAI PRO POS}"; Flags: nowait postinstall skipifsilent

[Dirs]
; Ensure app data directory exists for SQLite DB
Name: "{localappdata}\SmartPOS"; Permissions: users-modify
