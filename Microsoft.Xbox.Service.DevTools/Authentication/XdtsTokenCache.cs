﻿// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Authentication
{
    internal class XdtsTokenCache : AuthTokenCache
    {
        public XdtsTokenCache() : base("xdts.cache")
        {
        }
    }
}
