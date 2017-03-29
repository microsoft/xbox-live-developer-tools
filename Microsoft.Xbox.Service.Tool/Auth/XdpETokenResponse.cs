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
    internal class XdpETokenResponse
    {
        public string Message { get; set; }

        public bool IsError { get; set; }

        public XdtsTokenResponse Data { get; set; }

        public string CorrelationId { get; set; }
    }
}
