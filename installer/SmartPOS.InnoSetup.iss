; RoboVAI POS Installer (Inno Setup)
; Requires Inno Setup (https://jrsoftware.org/isinfo.php)

#define MyAppName "RobovAI PRO POS"
#define MyAppVersion "6.0"
#define MyAppPublisher "RoboVAI Solutions"
#define MyAppURL "https://robovai.tech"
#define MyAppExeName "SmartPOS.WPF.exe"

[Setup]
AppId={{4C204C16-7CCF-4F70-9E7C-4E87C566E9B6}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppCopyright=Copyright (c) 2026 RoboVAI. All rights reserved.
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=RobovAI-PRO-POS-Setup-v6.0
UninstallDisplayIcon={app}\{#MyAppExeName}
WizardSmallImageFile=..\RobovAI PRO POS small.png
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
VersionInfoVersion=6.0.0.0
VersionInfoCompany=RoboVAI Solutions
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion=6.0
VersionInfoDescription={#MyAppName} Setup v6.0 - Professional Edition
VersionInfoCopyright=Copyright (c) 2026 RoboVAI Solutions

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; Ship only the published app folder contents
Source: "..\publish\final-exe\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[InstallDelete]
; Cleanup legacy shortcuts from the old branding so users don't launch an old install.
Type: filesandordirs; Name: "{autoprograms}\SmartPOS"
Type: files; Name: "{autodesktop}\SmartPOS.lnk"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Optional: clean local app data on uninstall? (disabled by default)
; Type: filesandordirs; Name: "{localappdata}\SmartPOS"
