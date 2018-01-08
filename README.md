## Welcome!

The Microsoft Xbox Live Tooling API provides a way to:

* Reset a player's data in test sandboxes. Data includes achievements, leaderboards, stats and title history.
* Manage title's global storage in test sandbox.

To get access to the Xbox Live service you must be a managed developer, enrolled in the [ID@Xbox](http://www.xbox.com/Developers/id) program or participating in the [Xbox Live Creators Program](https://aka.ms/xblcp). To learn more about these programs, please refer to the [developer program overview](https://docs.microsoft.com/windows/uwp/xbox-live/developer-program-overview).



## Repo Structure
* [/Microsoft.Xbox.Service.Tool/](Microsoft.Xbox.Service.Tool): Xbox Live tooling dll, contains code for talking to xbl service tooling endpoints. Being consumed by cmdlets.
* [/CommandLine/](CommandLine): Command line executables for Xbox Live tooling.
* [/Test/](Test): Test code.

## Command Line Exe Usage:
### XblDevAccount.exe
The exe is used to signin/out dev account and save the credntial to be used across other xbl exes requires dev credential. 

#### Usage:
***signin:*** This command will pop up UI if needed, last used account info will be saved for further use across all other executables.
``` 
XblDevAccount.exe signin --userName xxx --accountSource XDP|WindowsDevCenter 
```

***success output example:***
```
Developer account {Name} has successfully signed in. 
    ID: {id}
    AccountID: {accountId}
    AccountType: {accountType}
    AccountMoniker: {accountMoniker}
    AccountSource: {accountSource}
```

***signout*** : This command will call delete last signed in account info, and clear up cached tokens.

```
XblDevAccount.exe signout
```

***success output example:***
```
Developer account {Name} has successfully signed out. 
```

--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

### PlayerReset.exe 
PlayerReset is used to reset a player's data in test sandboxes. Data includes achievements, leaderboards, stats and title history. XblDevAccount.exe signin is required to call at least once before first use. 

#### Usage:
```PlayerReset.exe –scid xxx --sandbox xxx --xuid xxxx ```

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
GlobalStorage.exe is used to manage title global storage in test sandboxes, before publish to RETAIL. XblDevAccount.exe signin is required to call at least once before first use.

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


## Contribute Back!

Is there a feature missing that you'd like to see, or found a bug that you have a fix for? Or do you have an idea or just interest in helping out in building the library? Let us know and we'd love to work with you. For a good starting point on where we are headed and feature ideas, take a look at our [requested features and bugs](../../issues).  

[Contrubute guidance](CONTRIBUTING.md)

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
