// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    using System.Threading.Tasks;
    using Microsoft.Identity.Client;

    internal interface IAuthContext
    {
        string XtdsEndpoint { get; set; }

        bool HasCredential { get; }

        DevAccountSource AccountSource { get; }

        string UserName { get; }

        string Tenant { get; }

        Task<string> AcquireTokenSilentAsync();

        Task<string> AcquireTokenAsync();

        Task<bool> HasCredentialAsync();

        Task<IAccount> SearchAccounts();
    }
}
