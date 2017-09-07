---
external help file: XboxLiveCmdlet.dll-Help.xml
online version: 
schema: 2.0.0
---

# Set-XblSandbox

## SYNOPSIS
Set sandbox for current machine or Xbox console.

## SYNTAX

```
Set-XblSandbox [-SandboxId] <String> [-ConsoleName <String>] [-UserName <String>] [-Password <String>]
 [<CommonParameters>]
```

## DESCRIPTION
The cmdlet lets you set the sandbox for current machine or a remote Xbox console.
If no ConsoleName parameter is specified, it sets the sandbox for local machine.

## EXAMPLES

### -- Example 1: Get Sandbox for current machine runs cmdlet. --
```
Set-XblSandbox XDKS.1
```

Set current machine&#39;s sandbox to XDKS.1

### -- Example 2: Get Sandbox for remote Xbox One console. --
```
Set-XblSandbox XDKS.1 -ConsoleName console -UserName username -Password password
```

Set the remote Xbox console sandbox id to XDKS.1 via Windows Device Portal . 
Please make sure the console is in Dev mode and Xbox Device Portal is enabled. 
If Xbox Device Portal does not require authentication, then don't pass in the username or password.

## PARAMETERS

### -ConsoleName
The machine name of the console, could be an IP address as well.

```yaml
Type: String
Parameter Sets: (All)
Aliases: 

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Password
Password for Xbox Device Portal authentication, if required. 

```yaml
Type: String
Parameter Sets: (All)
Aliases: 

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SandboxId
The sandbox Id want to set.

```yaml
Type: String
Parameter Sets: (All)
Aliases: 

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -UserName
UserName for Xbox Device Portal authentication, if required. 

```yaml
Type: String
Parameter Sets: (All)
Aliases: 

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### Common Parameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

## OUTPUTS

### System.Object

## NOTES

## RELATED LINKS

