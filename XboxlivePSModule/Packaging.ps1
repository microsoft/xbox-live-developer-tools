#only do the job for release binaries
if($env:BUILDCONFIGURATION -ine "release") 
{
    return
}

#create a package folder
$src = Join-Path $env:XES_OUTDIR "bin\XboxLiveCmdlets\*"
$dest = Join-Path $env:XES_DFSDROP "ModulePackage"

New-Item $dest -type directory

Write-Output "copying $src => $dest"
Copy-Item  $src $dest -Force 
$psdSrcFolder = Join-Path $env:BUILD_SOURCESDIRECTORY "XboxlivePSModule\"
$psdSrcPath = Join-Path $psdSrcFolder "XboxLivePsModule.psd1"
Copy-Item $psdSrcPath $dest

Import-module PowerShellGet
Update-ModuleManifest "$dest\XboxLivePsModule.psd1" -ModuleVersion $env:XES_NUGETPACKVERSIONNOBETA