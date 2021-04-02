if "%1" == "local" goto testlocal
goto start

:start
:testlocal
set TOOLS_SOURCEDIRECTORY=%CD%\..\..

:serializeForPostbuild

echo Running postBuildScript.cmd
echo on

set TOOLS_DROP_LOCATION=%TOOLS_SOURCEDIRECTORY%\XboxLiveDeveloperTools
rmdir /s /q %TOOLS_DROP_LOCATION%
mkdir %TOOLS_DROP_LOCATION%
mkdir %TOOLS_DROP_LOCATION%\ToolZip

setlocal
call %CD%\setBuildVersion.cmd

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
copy %TOOLS_SOURCEDIRECTORY%\CommandLine\XblConfig\bin\Release\XblConfig.exe                     %TOOLS_DROP_LOCATION%\ToolZip
copy %TOOLS_SOURCEDIRECTORY%\CommandLine\GlobalStorage\bin\Release\GlobalStorage.exe             %TOOLS_DROP_LOCATION%\ToolZip
copy %TOOLS_SOURCEDIRECTORY%\CommandLine\XblDevAccount\bin\Release\XblDevAccount.exe             %TOOLS_DROP_LOCATION%\ToolZip
copy %TOOLS_SOURCEDIRECTORY%\CommandLine\XblPlayerDataReset\bin\Release\XblPlayerDataReset.exe   %TOOLS_DROP_LOCATION%\ToolZip
copy %TOOLS_SOURCEDIRECTORY%\CommandLine\XblConnectedStorage\bin\Release\XblConnectedStorage.exe %TOOLS_DROP_LOCATION%\ToolZip

copy %TOOLS_SOURCEDIRECTORY%\Tools\MultiplayerSessionHistoryViewer\bin\Release\MultiplayerSessionHistoryViewer.exe %TOOLS_DROP_LOCATION%\ToolZip

%CD%\vZip.exe /FOLDER:%TOOLS_DROP_LOCATION%\ToolZip /OUTPUTNAME:%TOOLS_DROP_LOCATION%\XboxLiveTools-%LONG_SDK_RELEASE_NAME%.zip
rmdir /s /q %TOOLS_DROP_LOCATION%\ToolZip

echo.
echo Done postBuildScript.cmd
echo.
endlocal

:done
