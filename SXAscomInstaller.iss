;
; Script generated by the ASCOM Driver Installer Script Generator 1.3.0.0
; Generated by Bret McKee on 5/7/2010 (UTC)
;

#define BUILD_TYPE "Debug"
#define APP_VERSION "1.5.6"
#define ASCOM_VERSION_REQUIRED  "5.5"
#define DRIVER_EXE_NAME "ASCOM.SXCamera.exe"

[Setup]
AppName=ASCOM SX Camera Driver
AppVerName=ASCOM SX Camera Driver {#APP_VERSION}
AppPublisher=Bret McKee <bretm@daddog.com>
AppPublisherURL=http://www.daddog.com/ascom/sx/index.html
AppVersion={#APP_VERSION}
AppSupportURL=http://tech.groups.yahoo.com/group/ASCOM-Talk/
AppUpdatesURL=http://ascom-standards.org/
VersionInfoVersion="{#APP_VERSION}"
MinVersion=0,5.0.2195sp4
DefaultDirName="{cf}\ASCOM\Camera"
DisableDirPage=yes
DisableProgramGroupPage=yes
OutputDir="."
OutputBaseFilename="SXAscomInstaller-v{#APP_VERSION}"
Compression=lzma
SolidCompression=yes
; Put there by Platform if Driver Installer Support selected
WizardImageFile="C:\Program Files (x86)\ASCOM\InstallGen\Resources\WizardImage.bmp"
LicenseFile="C:\Users\bretm\Astronomy\src\sxASCOM\CreativeCommons.txt"

; {cf}\ASCOM\Uninstall\Camera folder created by Platform, always
UninstallFilesDir="{cf}\ASCOM\Uninstall\Camera\sxASCOM"

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Dirs]
Name: "{cf}\ASCOM\Uninstall\Camera\sxASCOM"
Name: "{app}"

[Files]
Source: "SXCamera\bin\x86\{#BUILD_TYPE}\{#DRIVER_EXE_NAME}"; DestDir: "{app}"
; Require a read-me HTML to appear after installation, maybe driver's Help doc
Source: "SXCamera.Readme.txt"; DestDir: "{app}"; Flags: isreadme
; TODO: Add other files needed by your driver here (add subfolders above)

; Only if driver is .NET
[Run]
; Only for .NET local-server drivers
Filename: "{app}\ASCOM.SXCamera.exe"; Parameters: "{code:RegistrationArgs}"

; Only if driver is .NET
[UninstallRun]
; Only for .NET local-server drivers
Filename: "{app}\ASCOM.SXCamera.exe"; Parameters: "/unregister"


[CODE]
// Global Variables
var
  LodeStarPage: TInputOptionWizardPage;
  MainCameraPage: TInputOptionWizardPage;
  GuideCameraPage: TInputOptionWizardPage;

//
// Before the installer UI appears, verify that the (prerequisite)
// ASCOM Platform 5 or greater is installed, including both Helper
// components. Helper is required for all types (COM and .NET)!
//
function InitializeSetup(): Boolean;
var
   H : Variant;
   H2 : Variant;
   P  : Variant;
begin
    Result := FALSE;  // Assume failure
    try               // Will catch all errors including missing reg data
        H := CreateOLEObject('DriverHelper.Util');  // Assure both are available
        H2 := CreateOleObject('DriverHelper2.Util');

        if (H2.PlatformVersion >= {#ASCOM_VERSION_REQUIRED})
        then
            begin
            P := CreateOLEObject('ASCOM.Utilities.Profile');
            P.DeviceType := 'Camera';

            if P.IsRegistered('ASCOM.SXMain0.Camera') or 
               P.IsRegistered('ASCOM.SXMain1.Camera') or
               P.IsRegistered('ASCOM.SXGuide.Camera')
            then
                begin
                    MsgBox('A previous version of this driver was detected. You must uninstall the previous version (from the control panel) before installation can proceed.', mbInformation, MB_OK);
                end
            else
                begin
                    Result := TRUE;
                end
            end
        else
            begin
                MsgBox('The ASCOM Platform {#ASCOM_VERSION_REQUIRED} or greater is required for this driver.', mbInformation, MB_OK);
            end
    except
        MsgBox('Caught an exception', mbInformation, MB_OK);
    end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
    Result := FALSE;

    if PageID = GuideCameraPage.ID then
        Result := MainCameraPage.Values[1];
end;

procedure InitializeWizard();
begin

    LodeStarPage := CreateInputOptionPage(wpLicense,
      'Lodestar Configuration', 'Do you have a Starlight Xpress LodeStar USB Guide Camera?',
      'This is a small, eyepiece sized guide camera that connets to your PC via USB.',
      True, False);
    LodeStarPage.Add('Yes');
    LodeStarPage.Add('No');

    LodeStarPage.Values[0] := False;
    LodeStarPage.Values[1] := True;

    MainCameraPage := CreateInputOptionPage(LodeStarPage.ID,
      'Main Camera Configuration', 'Do you have a Starlight Xpress Imaging Camera?',
      'This is a camera which connects to to your PC via USB.',
      True, False);
    MainCameraPage.Add('Yes');
    MainCameraPage.Add('No');

    MainCameraPage.Values[0] := False;
    MainCameraPage.Values[1] := True;

    GuideCameraPage := CreateInputOptionPage(MainCameraPage.ID,
      'Autoguide Camera Configuration', 'Do you have a Starlight Xpress SXV or ExView Autoguide Camera?',
      'This is a small, eyepiece sized guide camera that plugs into the back of an imaging camera.',
      True, False);
    GuideCameraPage.Add('Yes');
    GuideCameraPage.Add('No');

    GuideCameraPage.Values[0] := False;
    GuideCameraPage.Values[1] := True;

end;

function RegistrationArgs(Param : String) : String;
begin
    Result := '/register';

    if (LodeStarPage.Values[0]) then
        Result := Result + ' /lodestar';

    if (MainCameraPage.Values[0]) then
        Result := Result + ' /main';

    if (GuideCameraPage.Values[0]) then
        Result := Result + ' /autoguide ';
end;
