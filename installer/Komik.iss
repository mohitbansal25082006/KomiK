; Script generated for Komik Windows Installer
#define MyAppName "Komik"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "Mohit Bansal"
#define MyAppURL "https://github.com/mohitbansal25082006/KomiK"
#define MyAppExeName "Komik.exe"

[Setup]
AppId={{8B3B8A5A-61E4-4D47-9BD0-13D9416D41A2}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
; Version details shown in the setup file's Properties -> Details tab
VersionInfoVersion={#MyAppVersion}.0
VersionInfoProductVersion={#MyAppVersion}.0
VersionInfoProductTextVersion={#MyAppVersion}
VersionInfoTextVersion={#MyAppVersion}
VersionInfoProductName={#MyAppName}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} {#MyAppVersion} Setup
VersionInfoCopyright=Copyright (C) 2026 {#MyAppPublisher}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\shipping
OutputBaseFilename=Komik-Setup
SetupIconFile=..\Assets\AppIcon.ico
UninstallDisplayIcon={app}\Assets\AppIcon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
ChangesAssociations=yes
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "fileassoc"; Description: "Associate Komik with comic book and document files (.cbz, .cbr, .cb7, .zip, .rar, .7z, .pdf)"; GroupDescription: "File Associations:"

[Files]
Source: "..\publish_selfcontained\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\AppIcon.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\AppIcon.ico"; Tasks: desktopicon

[Registry]
; ProgID definition
Root: HKA; Subkey: "Software\Classes\Komik.ComicArchive"; ValueType: string; ValueName: ""; ValueData: "Comic Book Archive"; Flags: uninsdeletekey; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\Komik.ComicArchive\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\Komik.ComicArchive\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: fileassoc

; File extension associations
Root: HKA; Subkey: "Software\Classes\.cbz"; ValueType: string; ValueName: ""; ValueData: "Komik.ComicArchive"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\.cbz\OpenWithProgids"; ValueType: string; ValueName: "Komik.ComicArchive"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc

Root: HKA; Subkey: "Software\Classes\.cbr"; ValueType: string; ValueName: ""; ValueData: "Komik.ComicArchive"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\.cbr\OpenWithProgids"; ValueType: string; ValueName: "Komik.ComicArchive"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc

Root: HKA; Subkey: "Software\Classes\.cb7"; ValueType: string; ValueName: ""; ValueData: "Komik.ComicArchive"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\.cb7\OpenWithProgids"; ValueType: string; ValueName: "Komik.ComicArchive"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc

Root: HKA; Subkey: "Software\Classes\.zip"; ValueType: string; ValueName: ""; ValueData: "Komik.ComicArchive"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\.zip\OpenWithProgids"; ValueType: string; ValueName: "Komik.ComicArchive"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc

Root: HKA; Subkey: "Software\Classes\.rar"; ValueType: string; ValueName: ""; ValueData: "Komik.ComicArchive"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\.rar\OpenWithProgids"; ValueType: string; ValueName: "Komik.ComicArchive"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc

Root: HKA; Subkey: "Software\Classes\.7z"; ValueType: string; ValueName: ""; ValueData: "Komik.ComicArchive"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\.7z\OpenWithProgids"; ValueType: string; ValueName: "Komik.ComicArchive"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc

Root: HKA; Subkey: "Software\Classes\.pdf"; ValueType: string; ValueName: ""; ValueData: "Komik.ComicArchive"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\.pdf\OpenWithProgids"; ValueType: string; ValueName: "Komik.ComicArchive"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc

; Applications registration for "Open with" and Windows Default Apps
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".cbz"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".cbr"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".cb7"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".zip"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".rar"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".7z"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\SupportedTypes"; ValueType: string; ValueName: ".pdf"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKA; Subkey: "Software\Classes\Applications\{#MyAppExeName}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Flags: uninsdeletekey; Tasks: fileassoc

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
