#
# This is a PowerShell Unit Test file.
# You need a unit test framework such as Pester to run PowerShell Unit tests. 
# You can download Pester from http://go.microsoft.com/fwlink/?LinkID=534084
#

#force update module
Import-Module F:\git\github\jicailiu\xbox-live-powershell-module\XboxLivePsModule\XboxLivePsModule.psd1 -Force

Get-XblSandbox
Get-XblSandbox -XboxName jicailiupv
Set-XblSandbox XDK.3
