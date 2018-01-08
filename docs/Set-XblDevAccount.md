---
external help file: XboxLiveCmdlet.dll-Help.xml
online version: 
schema: 2.0.0
---

# Set-XblDevAccount

## SYNOPSIS
Sign in XboxLive dev account.

## SYNTAX

```
Set-XblDevAccount [-AccountSource] <String> [[-UserName] <String>] [<CommonParameters>]
```

## DESCRIPTION
Sign in XboxLive dev account. The dev account has to be registered on Universal Developer Center or Xbox Developer Portal. 

You may visit [Universal Developer Center](http://developer.microsoft.com) to resigter a Windows dev account; if you're a managed title developer, please check with your account manager to setup your Xbox Dev Portal account.

## EXAMPLES

### Example 1
```
PS C:\> Set-XblDevAccount XDP
```

Sign in the dev account with a xbox developer portal account.

## PARAMETERS

### -AccountSource
Where the account is registered. It could be of the following value: XboxDeveloperPortal, UniversalDeveloperCenter, XDP, UDC. 

XDP is short for XboxDeveloperPortal; UDC is short for UniversalDeveloperCenter

```yaml
Type: String
Parameter Sets: (All)
Aliases: 
Accepted values: XboxDeveloperPortal, UniversalDeveloperCenter, XDP, UDC

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -UserName
User name of the account. 

Because of a exisinng [issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/456) of AAD auth library. This parameter doesn't work with Because of a exisinng [issue](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/456) of AAD auth library. This parameter doesn't work with XboxDeveloperPortal accounts.


```yaml
Type: String
Parameter Sets: (All)
Aliases: 

Required: False
Position: 1
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

