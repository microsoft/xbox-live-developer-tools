//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

namespace Microsoft.Xbox.Services.Tool
{
    using System.Threading.Tasks;

    internal interface IAuthContext
    {
        string XtdsEndpoint { get; set; }

        bool HasCredential { get; }

        Task<string> AcquireTokenSilentAsync();

        Task<string> AcquireTokenAsync(string userName);

        DevAccountSource AccountSource { get; }
    }
}
