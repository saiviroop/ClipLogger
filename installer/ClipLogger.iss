#define MyAppName "Clip Logger"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "saiviroop"
#define MyAppExe "ClipLogger.App.exe"

[Setup]
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\ClipLogger
DefaultGroupName=Clip Logger
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=ClipLogger-Setup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern

[Tasks]
Name: "startuplogon"; Description: "Start Clip Logger when I log in"; GroupDescription: "Startup options:"

[Files]
Source: "app\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\Clip Logger"; Filename: "{app}\{#MyAppExe}"

[Registry]
; Per-user auto-start; matches AutoStartManager (value name "ClipLogger", quoted exe path).
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "ClipLogger"; ValueData: """{app}\{#MyAppExe}"""; \
  Tasks: startuplogon; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExe}"; Description: "Launch Clip Logger now"; Flags: nowait postinstall skipifsilent
