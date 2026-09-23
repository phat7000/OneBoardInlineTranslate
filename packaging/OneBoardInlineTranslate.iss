#define AppName "OneBoard Inline Translate"
#define AppVersion "1.1.0"
#define AppPublisher "OneBoard"
#define AppUrl "https://github.com/phat7000/OneBoardInlineTranslate"
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish"
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts"
#endif

[Setup]
AppId={{8A2DE6D2-8064-4A4F-9743-33B8EF64F17B}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
DefaultDirName={localappdata}\Programs\OneBoard Inline Translate
DefaultGroupName=OneBoard Inline Translate
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename=OneBoardInlineTranslate-Setup-1.1.0-win-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\OneBoardInlineTranslate.exe
VersionInfoVersion=1.1.0.0
VersionInfoCompany=OneBoard
VersionInfoDescription=OneBoard Inline Translate Setup
VersionInfoProductName=OneBoard Inline Translate

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\OneBoard Inline Translate"; Filename: "{app}\OneBoardInlineTranslate.exe"
Name: "{userdesktop}\OneBoard Inline Translate"; Filename: "{app}\OneBoardInlineTranslate.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\OneBoardInlineTranslate.exe"; Description: "Launch OneBoard Inline Translate"; Flags: nowait postinstall skipifsilent
