param([string]$binarySrcRoot, [string]$sourceSrcRoot, [string]$destRoot)

#only do the job for release binaries
if($env:BUILDCONFIGURATION -ine "release") 
{
    return
}

if ([string]::IsNullOrEmpty($binarySrcRoot))
{
    $binarySrcRoot = $env:XES_OUTDIR
}

if ([string]::IsNullOrEmpty($destRoot))
{
    $destRoot = $env:XES_DFSDROP
}

if ([string]::IsNullOrEmpty($sourceSrcRoot))
{
    $sourceSrcRoot = $env:BUILD_SOURCESDIRECTORY
}

$src = Join-Path $binarySrcRoot "bin\XboxLiveCmdlets\*"
$dest = Join-Path $destRoot "ModulePackage"

#create a package folder
New-Item $dest -type directory

Write-Output "copying $src => $dest"
Copy-Item  $src $dest -Force 
$psdSrcFolder = Join-Path $sourceSrcRoot "XboxlivePSModule\"
$psdSrcPath = Join-Path $psdSrcFolder "XboxLivePsModule.psd1"
$helpSrcPath = Join-Path $sourceSrcRoot "docs\en-US\XboxLiveCmdlet.dll-help.xml"
Copy-Item $psdSrcPath $dest
Copy-Item $helpSrcPath $dest

Import-module PowerShellGet
Update-ModuleManifest "$dest\XboxLivePsModule.psd1" -ModuleVersion $env:XES_NUGETPACKVERSIONNOBETA