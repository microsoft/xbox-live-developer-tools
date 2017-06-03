# Develop Guide
#### Getting start with Powershell Cmdlet and Module
For people who is not familiar with Powershell cmdlets, here is some helpful link:
* [Writing a Windows PowerShell Cmdlet](https://msdn.microsoft.com/en-us/library/dd878294(v=vs.85).aspx)
* [Writing a Windows PowerShell Module](https://msdn.microsoft.com/en-us/library/dd878310(v=vs.85).aspx)

#### Running on the dev machine and debugging
There are two ways to load XboxlivePsModule: 1. Load the XboxLiveCmdlet.dll directly. 2. Load via the manifest XboxLivePsModule.psd1. Loading from dll directly are recommend on debugging as it will export public cmdlet automatically. You can also check which modules are imported my running cmdlet Get-Module.

Debugging in VS:
On properties->debug, set Start action with Start external program: `"c:\windows\System32\WindowsPowerShell\v1.0\powershell.exe"`. Set Command line arguments: `-noexit -command "&{import-module .\xboxlivecmdlet.dll -verbose}"`

#### Add help information for cmdlets
We use [platyPS](https://github.com/PowerShell/platyPS) to create markdowns, then generate the xml doc can be consumed by powershell. 

Usage for adding or updating cmdlet: 
* Finish coding new cmdlet, export it in XboxLivePsModule.psd1 if it's a new cmdlet.
* Run powershell from root folder, load the lastest XboxLivePsModule locally: 
```powershell
Import-Module .\XboxLiveCmdlets\bin\Debug\XboxLivePsModule.psd1.
```
* From root folder, run 
```powershell
Update-MarkdownHelpModule -Path ".\docs"
```
* Generate into a xml file to be shipped with module: 
```powershell
New-ExternalHelp -Path ".\docs" -OutputPath ".\docs\en-US" -f
```