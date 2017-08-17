////*********************************************************
////
//// Copyright (c) Microsoft. All rights reserved.
//// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
////
////*********************************************************

namespace Microsoft.Xbox.Services.Tool
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
