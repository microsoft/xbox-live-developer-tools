## Welcome

Please refer to the official [Xbox Live Tools documentation](https://docs.microsoft.com/gaming/xbox-live/test-release/tools/live-tools) site for further information.

The Microsoft Xbox Live Tooling API provides a way to:

* Reset a player's data in test sandboxes. Data includes achievements, leaderboards, stats and title history.
* Manage a title's global storage in test sandboxes.
* Manage a title's Xbox Live configuration.
* Sign in Xbox Live test accounts and manage their privileges and privacy settings.

To get access to Xbox Live services you must be a managed developer, enrolled in the [ID@Xbox](http://www.xbox.com/Developers/id) program or participating in the [Xbox Live Creators Program](https://aka.ms/xblcp). To learn more about these programs, please refer to the [developer program overview](https://docs.microsoft.com/windows/uwp/xbox-live/developer-program-overview).

## Repo Structure

* [/Microsoft.Xbox.Service.DevTools/](Microsoft.Xbox.Service.DevTools): Xbox Live tooling dll, contains code for talking to Xbox Live service tooling endpoints.
* [/CommandLine/](CommandLine): Command line executables for Xbox Live tooling.
* [/Tests/](Tests): Test code.

## Command Line Executable Usage:

### XblDevAccount.exe

This executable is used to signin/out dev accounts and to save the credentials to be used across other Xbox Live executables that require dev credentials. 

#### Usage

***signin:*** This command will pop up UI if needed. The last used account information will be saved for further use across all other executables.

```DOS
XblDevAccount.exe signin --name xxx 
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

### XblTestAccount.exe

XblTestAccount signs in an Xbox Live test account and caches the credential, so that later runs obtain a user token
without showing any UI. It also reads and changes the privileges and privacy settings of the signed in account, which is
the command line equivalent of the Privacy and Privilege tabs in XblTestAccountGui.

#### Usage

***signin:*** Signs in a test account and caches the credential. UI is only shown when there is no usable cached
credential, or when `--force` is passed.

```
XblTestAccount.exe signin --name xxx@xboxtest.com --sandbox XXXXXX.0 [--force]
```

***Success output example:***

```
Test account {UserName} has successfully signed in to sandbox {Sandbox}.
    Gamertag : {gamertag}
    XUID : {xuid}
    Sandbox : {sandbox}
    Age Group : {ageGroup}

Run "XblTestAccount show" for its privileges and privacy settings.
```

***show:*** Displays everything known about the signed in test account: who it is, the state of every privilege on its
token, and the value of every privacy setting the service exposes. Privileges are claims on the token, so `show` reports
them as they were at sign in; add `--refresh` to mint a new token and see them as they are now.

```
XblTestAccount.exe show [--refresh] [--sandbox XXXXXX.0]
```

***Success output example:***

```
> XblTestAccount.exe show --refresh
Test account {UserName} is currently signed in.
    Gamertag : {gamertag}
    XUID : {xuid}
    Sandbox : {sandbox}
    Age Group : Adult

Privileges:
    185  Cross Network Play        Granted     (editable)
    189  Non-interactive Sessions  Restricted
    252  Comms (text and voice)    Restricted  (set with: privacy set CommunicateUsingTextAndVoice)
    254  Multiplayer Sessions      Restricted  (editable)
    ...

Privacy settings:
    AllowUserCreatedContentViewing     Everyone  (controls privilege 247)
    CommunicateDuringCrossNetworkPlay  Everyone
    CommunicateUsingTextAndVoice       Blocked   (controls privilege 252)
    ...
```

A privilege whose id this tool does not have a name for is reported as `(unknown)`, because the service mints new ids
from time to time.

***signout:*** Deletes the last signed in test account information, and clears up cached test account tokens.

```
XblTestAccount.exe signout
```

***privilege:*** Lists the known privileges, or blocks and allows them on the signed in test account. The action is
required, and the sandbox defaults to the one the account signed in to.

```
XblTestAccount.exe privilege listall
XblTestAccount.exe privilege block <ids> [--sandbox XXXXXX.0]
XblTestAccount.exe privilege allow <ids> [--sandbox XXXXXX.0]
```

***Success output example:***

```
> XblTestAccount.exe privilege block 185
Restricting 185 (Cross Network Play) on {gamertag} ({xuid}) in sandbox {sandbox}.
Privileges now restricted by the parental service for {gamertag} ({xuid}):
    185  Cross Network Play

This is only what the parental service holds. Run "XblTestAccount show --refresh" for the effective set.
```

`block` and `allow` report only what the parental service holds. A privilege the service derives from a privacy setting
is enforced through the token claims and never appears there, which is why `show --refresh` is the way to read the
effective set.

Only **185 (Cross Network Play)** and **254 (Multiplayer Sessions)** can be blocked and allowed, matching what
XblTestAccountGui exposes; the tool refuses other ids up front rather than letting the service reject them with a bare
HTTP 400. Privileges that the service derives from a privacy setting are changed through that setting instead:

| Privilege | Controlled by |
|---|---|
| 247 (User Generated Content) | `privacy set AllowUserCreatedContentViewing Blocked` |
| 252 (Comms (text and voice)) | `privacy set CommunicateUsingTextAndVoice Blocked` |

The claims settle a little after a change, so a `show --refresh` issued immediately afterwards may still report the old
value; repeat it after a moment.

***privacy:*** Lists the privacy settings of the signed in test account, or changes one of them. The action is required.
`listall` reports every setting the service exposes with its current value, so it is also the way to discover the setting
names that `set` accepts. Setting names are matched without regard to case.

```
XblTestAccount.exe privacy listall [--sandbox XXXXXX.0]
XblTestAccount.exe privacy set <setting> <Everyone|PeopleOnMyList|Blocked> [--sandbox XXXXXX.0]
```

***Success output example:***

```
> XblTestAccount.exe privacy set CommunicateDuringCrossNetworkPlay Blocked
Setting CommunicateDuringCrossNetworkPlay to Blocked on {gamertag} ({xuid}) in sandbox {sandbox}.
CommunicateDuringCrossNetworkPlay is now Blocked.
```

A privacy setting is the account's own choice about who it shares with, whereas a privilege is a restriction the parental
service applies. A write is not immediately visible to a read, so the tool reads the value back until the change appears
and warns if it never does.

--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

### XblPlayerDataReset.exe 

XblPlayerDataReset is used to reset a player's data in test sandboxes. Data includes achievements, leaderboards, stats and title history. An individual or group of accounts can be reset by its
email address, or to reset an account by XUID, first run XblDevAccount.exe to log in with a Partner Center account.

Resetting by email signs in each test account. Sign in UI is only shown for an account that has no cached credential yet,
so running `XblTestAccount.exe signin` once per account beforehand lets this run unattended.

#### Usage:
```
XblPlayerDataReset.exe --scid xxx --sandbox xxx [--xuid xxxx] [--user XXX@xboxtest.com] [--file path/to/file] [--delimiter ,]
```

***Success output example:***
```
Player data has been reset successfully.
```

***Error output example:***
```
An error occurred while resetting player data:
    Leaderboard reset contains error: {errorMessage}
```

### GlobalStorage.exe 
GlobalStorage.exe is used to manage title global storage in test sandboxes, before publish to RETAIL. XblDevAccount.exe signin is required to be called at least once before first use.

#### Usage:

***quota:*** Get title global storage quota information.

```
GlobalStorage.exe quota –scid xxx --sandbox xxx
```

***Success output example:***

```
Your global storage quota: used bytes {usedBytes}, total bytes {totalBytes}
```

***list:*** Gets a list of blob meta-data under a given path for the title global storage.

```
GlobalStorage list --scid xxx --max-items 10 --path path --sandbox xxx
```

***Success output example:***

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

### XblConfig.exe

XblConfig.exe is used to manage Xbox Live configuration data for games developed in Windows Dev Center, also known as Config as Source. See the [documentation](CONFIGASSOURCE.md) for usage guidelines.

## Contribute Back

Is there a feature missing that you'd like to see, or found a bug that you have a fix for? Or do you have an idea or just interest in helping out in building the library? Let us know and we'd love to work with you. For a good starting point on where we are headed and feature ideas, take a look at our [requested features and bugs](../../issues).  

[Contribute guidance](CONTRIBUTING.md)

Big or small we'd like to take your contributions back to help improve the Xbox Live PowerShell Module for everyone. 

## Having Trouble?

We'd love to get your review score, whether good or bad, but even more than that, we want to fix your problem. If you submit your issue as a Review, we won't be able to respond to your problem and ask any follow-up questions that may be necessary. The most efficient way to do that is to open a an issue in our [issue tracker](../../issues).  

### Xbox Live GitHub projects

* [Xbox Live Service API for C++](https://github.com/Microsoft/xbox-live-api)
* [Xbox Live Samples](https://github.com/Microsoft/xbox-live-samples)
* [Xbox Live Resiliency Fiddler Plugin](https://github.com/Microsoft/xbox-live-resiliency-fiddler-plugin)
* [Xbox Live Trace Analyzer](https://github.com/Microsoft/xbox-live-trace-analyzer)
* [Xbox Live Developer Tools](https://github.com/Microsoft/xbox-live-developer-tools)

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
