#define MyAppName       "Лотерея — сумма на руки"
#define MyAppVersion    "1.0.0"
#define MyAppPublisher  "CoScore"
#define MyAppExeName    "LotteryTax.exe"
#define MyAppGUID       "6B7C8D9E-1234-5678-9ABC-DEF012345678"

[Setup]
AppId={{{#MyAppGUID}}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\LotteryTax
DisableDirPage=auto
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=output
OutputBaseFilename=LotteryTax-Setup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupLogging=yes

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; \
      GroupDescription: "Дополнительно"; Flags: unchecked

[Files]
; Само приложение
Source: "..\publish\*"; DestDir: "{app}"; \
        Excludes: "*.pdb"; \
        Flags: ignoreversion recursesubdirs createallsubdirs

; .NET-рантайм кладётся «внутрь» установщика, но НЕ устанавливается на диск
; (dontcopy = доступен только через ExtractTemporaryFile)
Source: "prereq\windowsdesktop-runtime-10.0-win-x64.exe"; Flags: dontcopy

[Icons]
Name: "{group}\{#MyAppName}";                Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Удалить {#MyAppName}";        Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";          Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; \
  Description: "Запустить {#MyAppName}"; \
  Flags: nowait postinstall skipifsilent

[Code]
const
  DotNetRegKey = 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App';
  RuntimeFile  = 'windowsdesktop-runtime-10.0-win-x64.exe';

function IsDotNet10DesktopInstalled: Boolean;
var
  Names: TArrayOfString;
  I: Integer;
  VersionDir: String;
  FindRec: TFindRec;
begin
  Result := False;

  // 1. Проверяем стандартный путь в 64-битном реестре
  if RegGetValueNames(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App', Names) then
    for I := 0 to GetArrayLength(Names) - 1 do
      if Pos('10.', Names[I]) = 1 then
      begin
        Result := True;
        Exit;
      end;

  // 2. Проверяем путь в WOW6432Node (часто используется для .NET Desktop Runtime)
  if RegGetValueNames(HKLM, 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App', Names) then
    for I := 0 to GetArrayLength(Names) - 1 do
      if Pos('10.', Names[I]) = 1 then
      begin
        Result := True;
        Exit;
      end;

  // 3. Проверяем ключ sharedhost (в некоторых версиях .NET 10 информация может быть здесь)
  if RegGetValueNames(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost', Names) then
    for I := 0 to GetArrayLength(Names) - 1 do
      if Pos('10.', Names[I]) = 1 then
      begin
        Result := True;
        Exit;
      end;

  // 4. Резервная проверка: наличие папки с версией 10.x в файловой системе
  VersionDir := ExpandConstant('{pf}\dotnet\shared\Microsoft.WindowsDesktop.App');
  if FindFirst(VersionDir + '\10.*', FindRec) then
  begin
    Result := True;
    FindClose(FindRec);
    Exit;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
  RuntimePath: String;
begin
  Result := '';
  NeedsRestart := False;

  if IsDotNet10DesktopInstalled then
  begin
    Log('.NET 10 Desktop Runtime уже установлен — пропускаем.');
    Exit;
  end;

  Log('Устанавливаем .NET 10 Desktop Runtime...');
  ExtractTemporaryFile(RuntimeFile);
  RuntimePath := ExpandConstant('{tmp}\' + RuntimeFile);

  if not Exec(RuntimePath, '/install /quiet /norestart', '',
              SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    Result := 'Не удалось запустить установщик .NET 10 Desktop Runtime.';
    Exit;
  end;

  if (ResultCode <> 0) and (ResultCode <> 3010) then   // 3010 = требуется перезагрузка
  begin
    Result := Format('Установка .NET 10 Desktop Runtime завершилась с кодом %d. ' +
                     'Установите вручную: https://dotnet.microsoft.com/download/dotnet/10.0',[ResultCode]);
    Exit;
  end;

  if ResultCode = 3010 then
    NeedsRestart := True;  // .NET установлен, но требуется перезагрузка

  if not IsDotNet10DesktopInstalled then
  begin
    Result := 'Установка .NET 10 Desktop Runtime не подтверждена. ' +
              'Попробуйте установить вручную и запустить установщик снова.';
    Exit;
  end;

  Log('.NET 10 Desktop Runtime успешно установлен.');
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    Log('Установка приложения завершена.');
end;