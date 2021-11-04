echo Running postBuildScript.cmd
echo on

set

dir /s /b %BUILD_BINARIESDIRECTORY%
dir /s /b %BUILD_SOURCESDIRECTORY%

set TOOLS_BINARIESDIRECTORY=%BUILD_BINARIESDIRECTORY%
set TOOLS_SOURCEDIRECTORY=%BUILD_SOURCESDIRECTORY%

set TOOLS_DROP_LOCATION=%TOOLS_BINARIESDIRECTORY%\XboxLiveDeveloperTools
echo %TOOLS_DROP_LOCATION%
dir /s /b %TOOLS_DROP_LOCATION%
rmdir /s /q %TOOLS_DROP_LOCATION%
dir /s /b %TOOLS_DROP_LOCATION%

set TOOLS_DROP_LOCATION_VPACK=%TOOLS_DROP_LOCATION%\Tools-VPack
echo %TOOLS_DROP_LOCATION_VPACK%

mkdir %TOOLS_DROP_LOCATION%
mkdir %TOOLS_DROP_LOCATION_VPACK%
mkdir %TOOLS_DROP_LOCATION%\ToolZip
dir /s /b %TOOLS_DROP_LOCATION%
dir /s /b %TOOLS_DROP_LOCATION%\ToolZip

setlocal
call %TOOLS_SOURCEDIRECTORY%\Utilities\Pipelines\Scripts\setBuildVersion.cmd


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
set TOOLS_RELEASEDIRECTORY=%TOOLS_BINARIESDIRECTORY%\%CONFIGURATION%\AnyCPU\bin

copy %TOOLS_SOURCEDIRECTORY%\Tools\ThirdPartyNotices.txt %TOOLS_DROP_LOCATION%\ToolZip
copy %TOOLS_SOURCEDIRECTORY%\Tools\WinHttpFiddlerOff.cmd %TOOLS_DROP_LOCATION%\ToolZip
copy %TOOLS_SOURCEDIRECTORY%\Tools\WinHttpFiddlerOn.cmd  %TOOLS_DROP_LOCATION%\ToolZip

copy %TOOLS_RELEASEDIRECTORY%\XblPCSandbox\XblPCSandbox.exe            %TOOLS_DROP_LOCATION%\ToolZip
copy %TOOLS_RELEASEDIRECTORY%\XblTestAccountGui\XblTestAccountGui.exe  %TOOLS_DROP_LOCATION%\ToolZip
copy %TOOLS_RELEASEDIRECTORY%\XblTestAccountGui\XblTestAccountGui.docx %TOOLS_DROP_LOCATION%\ToolZip

robocopy /NJS /NJH /MT:16 /S /NP %TOOLS_SOURCEDIRECTORY%\Tools\XceTool         %TOOLS_DROP_LOCATION%\ToolZip\XceTool
robocopy /NJS /NJH /MT:16 /S /NP %TOOLS_SOURCEDIRECTORY%\Tools\MatchSimTool    %TOOLS_DROP_LOCATION%\ToolZip\MatchSimTool
robocopy /NJS /NJH /MT:16 /S /NP %TOOLS_SOURCEDIRECTORY%\Tools\XboxLiveCompute %TOOLS_DROP_LOCATION%\ToolZip\XboxLiveCompute

REM ------------------- OS VPACK BEGIN -------------------
copy %TOOLS_RELEASEDIRECTORY%\XblPCSandbox\XblPCSandbox.exe           %TOOLS_DROP_LOCATION_VPACK%
copy %TOOLS_RELEASEDIRECTORY%\XblTestAccountGui\XblTestAccountGui.exe %TOOLS_DROP_LOCATION_VPACK%


%TOOLS_SOURCEDIRECTORY%\Utilities\Pipelines\Scripts\vZip.exe /FOLDER:%TOOLS_DROP_LOCATION%\ToolZip /OUTPUTNAME:%TOOLS_DROP_LOCATION%\XboxLiveTools-%LONG_SDK_RELEASE_NAME%.zip
rmdir /s /q %TOOLS_DROP_LOCATION%\ToolZip


echo.
echo Done postBuildScript.cmd
echo.
endlocal
