## Welcome!

The Microsoft Xbox Live PowerShell Module provides a way to:

* Get/Set sandboxes on PC and Xbox console.

To get access to the Xbox Live service you must be a managed developer, enrolled in the [ID@Xbox](http://www.xbox.com/Developers/id) program or participating in the [Xbox Live Creators Program](https://aka.ms/xblcp). To learn more about these programs, please refer to the [developer program overview](https://docs.microsoft.com/windows/uwp/xbox-live/developer-program-overview).


## Getting Started using Xbox Live PowerShell Module

* Install Xbox Live PowerShell module from the [PowerShell Gallery](https://powershellgallery.com):

```powershell
Install-Module XboxlivePSModule -Scope CurrentUser
Import-Module XboxlivePSModule
```

* Update your exsiting Xbox Live PS Module 
```powershell
Update-Module XboxlivePSModule
```

* Usage

    * Get and set sandbox on your PC or Xbox One Console with [Get-XblSandbox](docs/Get-XblSandbox.md) and [Set-XblSandbox](docs/Set-XblSandbox.md )

## Repo Structure
* [/docs/](docs): Cmdlets documents.
* [/Microsoft.Xbox.Service.Tool/](Microsoft.Xbox.Service.Tool): XBL cloud tooling dll, contains code for talking to xbl service tooling endpoints. Being consumed by cmdlets.
* [/XboxLiveCmdlets/](XboxLiveCmdlets): Code for xbl cmdlets, warpper for consuming Microsoft.Xbox.Service.Tool.dll, also contains client only code for tooling like sandbox utilities.  
* [/XboxlivePSModule/](XboxlivePSModule): Manifest and building script for Xbl PS module.

## Documentation
You can also learn how to use Xbox Live PowerShell module by reading our documentation:

- [Cmdlet Documentation](docs/XboxLivePsModule.md)


## Contribute Back!

Is there a feature missing that you'd like to see, or found a bug that you have a fix for? Or do you have an idea or just interest in helping out in building the library? Let us know and we'd love to work with you. For a good starting point on where we are headed and feature ideas, take a look at our [requested features and bugs](../../issues).  

[Contrubute guidance](CONTRIBUTING.md)
[Develop guidance](DEVELOP.md)

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
