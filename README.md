## Welcome!

The Microsoft Xbox Live Tooling API provides a way to:

* Reset a player's data in test sandboxes. Data includes achievements, leaderboards, stats and title history.
* Manage a title's global storage in test sandboxes.
* Manage a title's Xbox Live configuration.

To get access to Xbox Live services you must be a managed developer, enrolled in the [ID@Xbox](http://www.xbox.com/Developers/id) program or participating in the [Xbox Live Creators Program](https://aka.ms/xblcp). To learn more about these programs, please refer to the [developer program overview](https://docs.microsoft.com/windows/uwp/xbox-live/developer-program-overview).



## Repo Structure
* [/Microsoft.Xbox.Service.Tool/](Microsoft.Xbox.Service.Tool): Xbox Live tooling dll, contains code for talking to Xbox Live service tooling endpoints.
* [/CommandLine/](CommandLine): Command line executables for Xbox Live tooling.
* [/Test/](Test): Test code.

## Command Line Executable Usage:
### XblDevAccount.exe
This executable is used to signin/out dev accounts and to save the credentials to be used across other Xbox Live executables that require dev credentials. 

#### Usage:
***signin:*** This command will pop up UI if needed. The last used account information will be saved for further use across all other executables.
``` 
XblDevAccount.exe signin --userName xxx --accountSource XDP|WindowsDevCenter 
```

***Success output example:***
```
Developer account {Name} has successfully signed in. 
    ID: {id}
    AccountID: {accountId}
    AccountType: {accountType}
    AccountMoniker: {accountMoniker}
    AccountSource: {accountSource}
```

***signout:*** This command will delete the last signed in account information, and clear up cached tokens.

```
XblDevAccount.exe signout
```

***Success output example:***
```
Developer account {Name} has successfully signed out.
```

--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

### PlayerReset.exe 
PlayerReset is used to reset a player's data in test sandboxes. Data includes achievements, leaderboards, stats and title history. XblDevAccount.exe signin is required to be called at least once before first use. 

#### Usage:
```
PlayerReset.exe –scid xxx --sandbox xxx --xuid xxxx
```

***Success output example:*** 
```
Player {email} data reset has completed successfully.
```

***Error output example:***
```
Player {email} data reset has completed with errors:
    Leaderboard reset contains error: {errorMessage}
```


--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

### GlobalStorage.exe 
GlobalStorage.exe is used to manage title global storage in test sandboxes, before publish to RETAIL. XblDevAccount.exe signin is required to be called at least once before first use.

#### Usage:
***quota:*** Get title global storage quota information.
```
GlobalStorage.exe quota –scid xxx --sandbox xxx
```


Success output:
```
Your global storage quota: used bytes {usedBytes}, total bytes {totalBytes}
```

***list:*** Gets a list of blob meta-data under a given path for the title global storage.
```
GlobalStorage list --scid xxx --max-items 10 --path path --sandbox xxx
```
Success output:
```
Total 12 items found, Displaying item 0 to 12
        test.txt,       Config,         2
        ...
        tool.zip,       Binary,         1874772
```

***delete:*** Deletes a blob from title storage.
```
GlobalStorage delete --scid xxx --blob-path foo\bar\blob.txt --sandbox xxx --type Json
```

***download:*** Downloads blob data from title storage.
```
GlobalStorage download --scid xxx --output c:\test.txt --blob-path \text.txt --sandbox xxx --type Json
```

***upload:*** Uploads blob data to title storage.
```
GlobalStorage upload --scid xxx --file c:\test.txt --blob-path \text.txt --sandbox xxx --type Json
```


--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

### XblConfig.exe 
XblConfig.exe is used to manage Xbox Live configuration data for games developed in Windows Dev Center. XblDevAccount.exe signin is required to be called at least once before first use.

#### Usage:
***get-documents:*** Get all Xbox Live configuration documents for a given title.
This can get two types of documents. Sandbox documents which are stored in specific sandboxes for a given title, or Account documents which are stored for a specific account.

****Sandbox Documents:****
```
XblConfig get-documents --scid xxx --sandbox xxx -destination path --type Sandbox [--view Working|Published] [--configset xxx]
```

****Account Documents:****
```
XblConfig get-documents --accountId xxx -destination path --type Account [--view Working|Published] [--configset xxx]
```

***commit:*** Commit configuration documents back to Xbox Live.

****Sandbox Documents****
```
XblConfig commit --scid xxx --sandbox xxx --files path --type Sandbox [--validateOnly] [--eTag xxx] [--force] [--message xxx]
```

****Account Documents:****
```
XblConfig commit --accountId xxx --files path --type Account [--validateOnly] [--eTag xxx] [--force] [--message xxx]
```

***get-schemas:*** Gets the document XSD schemas. Passing no arguments results in a list of possible document types. Passing just the type results in a list of available document versions.
```
XblConfig get-schemas --type xxx --version xxx --destination path
```

***get-products:*** Gets a list of products available for a given account.
```
XblConfig get-products [--accountId xxx]
```

***get-product:*** Gets the details of a specific product
```
XblConfig get-product --productId xxx
```

***get-sandboxes:*** Gets a list of sandboxes for a given account.
```
XblConfig get-sandboxes [--accountId xxx]
```

***get-achievement-image:*** Gets the details of an achievement image by its asset ID.
```
XblConfig get-achievement-image --scid xxx --assetId xxx
```

***upload-achievement-image:*** Uploads an achievement image to a specific SCID.
```
XblConfig upload-achievement-image --scid xxx --file path
```

***get-relying-parties:*** Gets relying parties for a given account.
```
XblConfig get-relying-parties [--accountId xxx]
```

***get-relying-party-document:*** Gets a specific relying party document.
```
XblConfig get-relying-party-document --filename xxx [--destination path]
```

***get-web-services:*** Gets a list of web services for a given account.
```
XblConfig get-web-services [--accountId xxx]
```

***create-web-service:*** Creates a new web service.
```
XblConfig create-web-service [--accountId xxx] --name xxx [--telemetryAccess] [--appChannelAccess]
```

***update-web-service:*** Updates an existing web service.
```
XblConfig update-web-service --serviceId xxx [--accountId xxx] --name xxx [--telemetryAccess] [--appChannelAccess]
```

***delete-web-service:*** Deletes a web service.
```
XblConfig delete-web-service --serviceId xxx [--accountId xxx]
```

***generate-web-service-cert:*** Generates a web service certificate. Note: This command must be run as an administrator.
```
XblConfig generate-web-service-cert [--accountId xxx] --serviceId xxx --destination path
```

***publish:*** Publishes a set of documents for use by Xbox Live services.
```
XblConfig publish --scid xxx --from xxx --to xxx [--validateOnly] [--configset]
```

***get-publish-status:*** Gets the publish status.
```
XblConfig get-publish-status --scid xxx --sandbox xxx
```

--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------


## Contribute Back!

Is there a feature missing that you'd like to see, or found a bug that you have a fix for? Or do you have an idea or just interest in helping out in building the library? Let us know and we'd love to work with you. For a good starting point on where we are headed and feature ideas, take a look at our [requested features and bugs](../../issues).  

[Contribute guidance](CONTRIBUTING.md)

Big or small we'd like to take your contributions back to help improve the Xbox Live PowerShell Module for everyone. 

## Having Trouble?

We'd love to get your review score, whether good or bad, but even more than that, we want to fix your problem. If you submit your issue as a Review, we won't be able to respond to your problem and ask any follow-up questions that may be necessary. The most efficient way to do that is to open a an issue in our [issue tracker](../../issues).  

### Xbox Live GitHub projects
*   [Xbox Live Service API for C++](https://github.com/Microsoft/xbox-live-api)
*   [Xbox Live Samples](https://github.com/Microsoft/xbox-live-samples)
*   [Xbox Live Unity Plugin](https://github.com/Microsoft/xbox-live-unity-plugin)
*   [Xbox Live Resiliency Fiddler Plugin](https://github.com/Microsoft/xbox-live-resiliency-fiddler-plugin)
*   [Xbox Live Trace Analyzer](https://github.com/Microsoft/xbox-live-trace-analyzer)
*   [Xbox Live Developer Tools](https://github.com/Microsoft/xbox-live-developer-tools)

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
