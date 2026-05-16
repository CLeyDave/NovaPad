; NovaPad Installer - Inno Setup Script
; Requires Inno Setup 6+

#define MyAppName "NovaPad"
#define MyAppVersion "3.2.4"
#define MyAppPublisher "CLeyDave"
#define MyAppURL "https://github.com/CLeyDave/NovaPad"
#define MyAppExeName "NovaPad.WPF.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={userappdata}\NovaPad
DefaultGroupName=NovaPad
DisableProgramGroupPage=yes
OutputDir=C:\Users\USUARIO\AppData\Local\Temp\opencode
OutputBaseFilename=NovaPad-Setup-v{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
DisableWelcomePage=yes
DisableReadyPage=yes
DisableFinishedPage=yes
PrivilegesRequired=lowest
CloseApplications=force

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el escritorio"; GroupDescription: "Accesos directos:"

[Files]
Source: "C:\Users\USUARIO\AppData\Local\Temp\opencode\novapad-publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\NovaPad"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Desinstalar NovaPad"; Filename: "{uninstallexe}"
Name: "{userdesktop}\NovaPad"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Ejecutar NovaPad"; Flags: postinstall nowait shellexec

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\NovaPad"

[UninstallRun]
Filename: "{app}\NovaPad.WPF.exe"; Parameters: "--uninstall"; Flags: runhidden
