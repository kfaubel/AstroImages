; Inno Setup Script for AstroImages
; https://jrsoftware.org/isinfo.php

#define MyAppName "AstroImages"
#define MyAppVersion "1.5.0"
#define MyAppPublisher "Ken Faubel"
#define MyAppURL "https://github.com/kfaubel/AstroImages"
#define MyAppExeName "AstroImages.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
AppId={{8F7A2E1C-9B3D-4F5E-A6C8-1D2E3F4A5B6C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={localappdata}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; Show release notes before installation
InfoBeforeFile=RELEASE_NOTES.txt
; Output directory for the installer
OutputDir=installer-output
OutputBaseFilename=AstroImages-Setup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
SetupIconFile=AstroImages.Wpf\icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
; Privileges
PrivilegesRequired=lowest
; Architecture
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Version Info
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Installer
VersionInfoCopyright=Copyright (C) 2025-2026 {#MyAppPublisher}
; Modern look
WizardStyle=modern
DisableWelcomePage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startmenu"; Description: "Create Start Menu shortcut"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Main application files from publish directory
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
; Start Menu shortcut (always created unless user unchecks)
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startmenu
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"; Tasks: startmenu
; Desktop shortcut (optional)
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Option to launch the application after installation
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Check if .NET 8 Desktop Runtime is installed
function IsDotNet8Installed: Boolean;
var
  ResultCode: Integer;
  Output: AnsiString;
begin
  // Check using dotnet --list-runtimes command
  if Exec('cmd.exe', '/c dotnet --list-runtimes | findstr "Microsoft.WindowsDesktop.App 8."', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    Result := ResultCode = 0;
  end
  else
  begin
    Result := False;
  end;
end;

function InitializeSetup: Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  
  // Check for .NET 8 Desktop Runtime
  if not IsDotNet8Installed then
  begin
    if MsgBox('.NET 8 Desktop Runtime is required but not installed.' + #13#10#13#10 + 
              'Would you like to download it now?' + #13#10#13#10 +
              'The installer will open the download page in your browser.',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOW, ewNoWait, ResultCode);
      Result := False; // Cancel installation
    end
    else
    begin
      Result := False; // Cancel installation
    end;
  end;
end;

[UninstallDelete]
; Clean up any user-generated files
Type: filesandordirs; Name: "{app}\logs"
Type: files; Name: "{app}\appconfig.json"
