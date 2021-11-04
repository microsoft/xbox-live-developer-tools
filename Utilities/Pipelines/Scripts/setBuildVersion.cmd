set SDK_RELEASE_YEAR=2020
set SDK_RELEASE_MONTH=09

set /p buildVersionTxt= </BuildVersion.txt
if "%buildVersionTxt%"=="" set /p buildVersionTxt= <../BuildVersion.txt
if "%buildVersionTxt%"=="" set /p buildVersionTxt= <../../BuildVersion.txt
if "%buildVersionTxt%"=="" goto err

echo %buildVersionTxt%

set SDK_RELEASE_YEAR=%buildVersionTxt:~0,4%
set SDK_RELEASE_MONTH=%buildVersionTxt:~5,7%

goto done
:err
echo Couldn't find BuildVersion.txt

:done
echo %SDK_RELEASE_YEAR% %SDK_RELEASE_MONTH%