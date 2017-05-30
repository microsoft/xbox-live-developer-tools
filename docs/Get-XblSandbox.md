---
external help file: XboxLiveCmdlet.dll-Help.xml
online version: 
schema: 2.0.0
---

# Get-XblSandbox

## SYNOPSIS
Get sandbox for current machine or xbox console

## SYNTAX

```
Get-XblSandbox [-ConsoleName <String>] [-UserName <String>] [-Password <String>] [<CommonParameters>]
```

## DESCRIPTION
The Get-XblSandbox cmdlet let you get the sandbox for current machine or a remote xbox console.

## EXAMPLES

### -- Example 1: Get Sandbox for current machine runs cmdlet. --
```
Get-XblSandbox
```

Thie command returns the sandbox for current machine runs cmdlet.

### -- Example 2: Get Sandbox for remote Xbox One console. --
```
Get-XblSandbox -ConsoleName console -UserName username -Password password
```

Thie command returns the sandbox for remote Xbox One console.
Please make sure the console is in Dev mode and Xbox Device Portal is enabled. 
If Xbox Device Portal does not require authentication, then don't need to pass in Username and Passeword

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

### -UserName
Username for Xbox Device Portal authentication, if required.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

## NOTES

## RELATED LINKS

