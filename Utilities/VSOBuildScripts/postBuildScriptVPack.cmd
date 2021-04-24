echo Running postBuildScriptVPack.cmd
echo on

set

set TOOLS_DROP_LOCATION=%TOOLS_BINARIESDIRECTORY%\XboxLiveDeveloperTools

REM ------------------- TOOLS BEGIN -------------------
set TOOLS_DROP_LOCATION_VPACK=%TOOLS_DROP_LOCATION%\Tools-VPack

copy %XES_VPACKMANIFESTDIRECTORY%\%XES_VPACKMANIFESTNAME% %TOOLS_DROP_LOCATION_VPACK%

echo.
echo Done postBuildScriptVPack.cmd
echo.
endlocal

exit /b
