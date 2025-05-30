; Script Inno Setup avec installation conditionnelle de CryptoSoft

#define MyAppName "EasySave"
#define MyAppVersion "3.0.0"
#define MyAppPublisher "Prosoft, Inc."
#define MyAppURL "https://www.prosoft.com/"
#define MyAppExeName "easysave.exe"
#define MyAppAssocName MyAppName + " File"
#define MyAppAssocExt ".myp"
#define MyAppAssocKey StringChange(MyAppAssocName, " ", "") + MyAppAssocExt

[Setup]
AppId={{0AEADF13-C221-4954-A198-AFC6B7D084E5}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
ChangesAssociations=yes
DisableProgramGroupPage=yes
PrivilegesRequiredOverridesAllowed=dialog
OutputBaseFilename=EasySave_setup
SolidCompression=yes
WizardStyle=modern
DisableWelcomePage=yes
DisableDirPage=no
DisableReadyPage=no
DisableFinishedPage=no


[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Components]
Name: "cryptosoft"; Description: "CryptoSoft (installé uniquement si requis)"; Types: full

[Files]
; EasySave principal
Source: "C:\Users\lecle\source\repos\loloxd11\EasySave\easysave\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Config par défaut
Source: "C:\Users\lecle\AppData\Roaming\EasySave\default_config.json"; DestDir: "{userappdata}\EasySave"; DestName: "config.json"; Flags: onlyifdoesntexist

; CryptoSoft - installé uniquement si composant activé
Source: "C:\Users\lecle\source\repos\loloxd11\EasySave\CryptoSoft\bin\Release\net8.0\win-x64\publish\CryptoSoft.exe"; DestDir: "{pf}\CryptoSoft"; Flags: ignoreversion; Components: cryptosoft

[Registry]
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocExt}\OpenWithProgids"; ValueType: string; ValueName: "{#MyAppAssocKey}"; ValueData: ""; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}"; ValueType: string; ValueName: ""; ValueData: "{#MyAppAssocName}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  CryptoPath: string;
begin
  if CurStep = ssInstall then
  begin
    CryptoPath := ExpandConstant('{pf}\CryptoSoft\CryptoSoft.exe');
    if not FileExists(CryptoPath) then
    begin
      if MsgBox('CryptoSoft n''est pas installé. Voulez-vous l''installer maintenant ?', mbConfirmation, MB_YESNO) = IDYES then
      begin
        WizardSelectComponents('cryptosoft');
      end
      else
      begin
        MsgBox('CryptoSoft n''a pas été installé. Cela peut nuire au bon fonctionnement de EasySave.', mbInformation ,MB_OK);
      end;
    end;
  end;
end;
