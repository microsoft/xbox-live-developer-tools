---
external help file: XboxLiveCmdlet.dll-Help.xml
online version: 
schema: 2.0.0
---

# Get-XblGlobalStorageBlob

## SYNOPSIS
Download a global storage blob file from service.

## SYNTAX

```
Get-XblGlobalStorageBlob [-ServiceConfigurationId] <String> [-Sandbox] <String> [-PathAndFileName] <String>
 [-FileType] <String> [-OutFile] <String> [-ForceOverwrite] [<CommonParameters>]
```

## DESCRIPTION
Download a global storage blob file for the given service cofiguration ID and sandbox from service.

## EXAMPLES

### Example 1
```
PS C:\> Get-XblGlobalStoragBlob -ServiceConfigurationId 00000000-0000-0000-0000-00006afd5719 -Sandbox CSEJOT.0 -PathAndFileName test.txt -FileType Config
```

Download test.txt as a config file to the service the paticular scid and sadbox.

## PARAMETERS

### -FileType
File type to download, accecpt value: Binary, Json, Config

```yaml
Type: String
Parameter Sets: (All)
Aliases: 
Accepted values: Binary, Json, Config

Required: True
Position: 3
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ForceOverwrite
If want to overwrite the existing file.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: 

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -OutFile
The destination file want to download as.

```yaml
Type: String
Parameter Sets: (All)
Aliases: 

Required: True
Position: 4
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PathAndFileName
The path and file name want to download.

```yaml
Type: String
Parameter Sets: (All)
Aliases: 

Required: True
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Sandbox
The sandbox ID for the global storage.

```yaml
Type: String
Parameter Sets: (All)
Aliases: 

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ServiceConfigurationId
{{Fill ServiceConfigurationId Description}}

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

## OUTPUTS

### System.Object

## NOTES

## RELATED LINKS

