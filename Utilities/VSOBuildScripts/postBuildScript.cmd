echo Running postBuildScript.cmd
echo on

set

set TOOLS_BINARIESDIRECTORY=%BUILD_BINARIESDIRECTORY%
set TOOLS_SOURCEDIRECTORY=%BUILD_SOURCESDIRECTORY%


set TOOLS_DROP_LOCATION=%TOOLS_BINARIESDIRECTORY%\XboxLiveDeveloperTools
set TOOLS_DROP_LOCATION_VPACK=%TOOLS_DROP_LOCATION%\Tools-VPack
rmdir /s /q %TOOLS_DROP_LOCATION%
mkdir %TOOLS_DROP_LOCATION%
mkdir %TOOLS_DROP_LOCATION_VPACK%
mkdir %TOOLS_DROP_LOCATION%\ToolZip

setlocal
call %TOOLS_SOURCEDIRECTORY%\Utilities\VSOBuildScripts\setBuildVersion.cmd


REM ------------------- VERSION SETUP BEGIN -------------------
for /f "tokens=2 delims==" %%G in ('wmic os get localdatetime /value') do set datetime=%%G
set DATETIME_YEAR=%datetime:~0,4%
set DATETIME_MONTH=%datetime:~4,2%
set DATETIME_DAY=%datetime:~6,2%

set SDK_POINT_NAME_YEAR=%DATETIME_YEAR%
set SDK_POINT_NAME_MONTH=%DATETIME_MONTH%
set SDK_POINT_NAME_DAY=%DATETIME_DAY%
set SDK_RELEASE_NAME=%SDK_RELEASE_YEAR:~2,2%%SDK_RELEASE_MONTH%
set LONG_SDK_RELEASE_NAME=%SDK_RELEASE_NAME%-%SDK_POINT_NAME_YEAR%%SDK_POINT_NAME_MONTH%%SDK_POINT_NAME_DAY%


REM ------------------- TOOLS BEGIN -------------------
set TOOLS_RELEASEDIRECTORY=%TOOLS_BINARIESDIRECTORY%\Release\anycpu\bin

copy %TOOLS_RELEASEDIRECTORY%\XblConfig\XblConfig.exe                                  %TOOLS_DROP_LOCATION%\ToolZip
copy %TOOLS_RELEASEDIRECTORY%\GlobalStorage\GlobalStorage.exe                          %TOOLS_DROP_LOCATION%\ToolZip
copy %TOOLS_RELEASEDIRECTORY%\XblDevAccount\XblDevAccount.exe                          %TOOLS_DROP_LOCATION%\ToolZip
copy %TOOLS_RELEASEDIRECTORY%\XblPlayerDataReset\XblPlayerDataReset.exe                %TOOLS_DROP_LOCATION%\ToolZip
copy %TOOLS_RELEASEDIRECTORY%\XblConnectedStorage\XblConnectedStorage.exe              %TOOLS_DROP_LOCATION%\ToolZip
copy %TOOLS_RELEASEDIRECTORY%\SessionHistoryViewer\MultiplayerSessionHistoryViewer.exe %TOOLS_DROP_LOCATION%\ToolZip

REM ------------------- OS VPACK BEGIN -------------------
copy %TOOLS_RELEASEDIRECTORY%\XblConfig\XblConfig.exe                   %TOOLS_DROP_LOCATION_VPACK%
copy %TOOLS_RELEASEDIRECTORY%\XblDevAccount\XblDevAccount.exe           %TOOLS_DROP_LOCATION_VPACK%
copy %TOOLS_RELEASEDIRECTORY%\XblPlayerDataReset\XblPlayerDataReset.exe %TOOLS_DROP_LOCATION_VPACK%


%TOOLS_SOURCEDIRECTORY%\Utilities\VSOBuildScripts\vZip.exe /FOLDER:%TOOLS_DROP_LOCATION%\ToolZip /OUTPUTNAME:%TOOLS_DROP_LOCATION%\XboxLiveDeveloperTools-%LONG_SDK_RELEASE_NAME%.zip
rmdir /s /q %TOOLS_DROP_LOCATION%\ToolZip

echo.
echo Done postBuildScript.cmd
echo.
endlocal
