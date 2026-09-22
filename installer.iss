; Script de Inno Setup para Sistema de Celulares
; Configurado para .NET 8 Windows Forms con base de datos segura en ProgramData

#define MyAppName "Sistema de Celulares"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Jose BDO"
#define MyAppExeName "SistemaCelulares.App.exe"

[Setup]
AppId={{E83B4A9C-821A-4C4A-9A4B-3D28E7C41099}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\SistemaCelulares
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=.\installer-output
OutputBaseFilename=SistemaCelulares_Setup_v1.0.0
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile=app_icon.ico

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Todos los binarios y dependencias autocontenidas de .NET 8
Source: "publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\{#MyAppExeName}"

[Run]
Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Filename: "{app}\{#MyAppExeName}"; Flags: nowait postinstall skipifsilent

[Dirs]
; Crear la carpeta de datos común en ProgramData con permisos de lectura/escritura completos
Name: "{commonappdata}\SistemaCelulares"; Permissions: users-full
Name: "{commonappdata}\SistemaCelulares\Backups"; Permissions: users-full
