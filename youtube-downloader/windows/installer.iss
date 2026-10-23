; Instalador de Windows (Inno Setup 6):  iscc /DAppVersion=1.0.0 windows\installer.iss
; Necesita antes dist\Descargador\ (pyinstaller windows\descargador.spec).

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
AppId={{6F2B8C1E-4A7D-4E3B-9C55-2D8A1F7E0B34}
AppName=Descargador
AppVersion={#AppVersion}
AppPublisher=ivanlruiz
AppPublisherURL=https://github.com/ivanlruiz/Apps
DefaultDirName={localappdata}\Programs\Descargador
DisableProgramGroupPage=yes
DisableDirPage=yes
PrivilegesRequired=lowest
OutputDir=..\dist
OutputBaseFilename=DescargadorSetup
SetupIconFile=icon.ico
UninstallDisplayIcon={app}\Descargador.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[InstallDelete]
; Al actualizar, quitar las bibliotecas de la versión anterior.
Type: filesandordirs; Name: "{app}\_internal"

[Files]
Source: "..\dist\Descargador\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Descargador"; Filename: "{app}\Descargador.exe"
Name: "{autodesktop}\Descargador"; Filename: "{app}\Descargador.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Descargador.exe"; Description: "{cm:LaunchProgram,Descargador}"; Flags: nowait postinstall skipifsilent
