// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Common
{
    using System.Diagnostics;

    internal class Log
    {
        internal static void WriteLog(string log)
        {
            Trace.WriteLine(log);
        }
    }
}
