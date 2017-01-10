#
# This is a PowerShell Unit Test file.
# You need a unit test framework such as Pester to run PowerShell Unit tests. 
# You can download Pester from http://go.microsoft.com/fwlink/?LinkID=534084
#

try{
Remove-Module XboxlivePSModule
}
catch{}

Import-Module .\XboxlivePSModule.psd1

Describe "Reset-XBLProgresss" {
	It "Verify output" {
		Reset-XBLProgresss -ServiceConfig 94730100-2018-46b8-900b-a41a0301d082 -Sandbox XDKS.1 -XboxUserIds 2814668800026588,2814668800026588,2814668800026588 -Environment dnet
    }
}