// -----------------------------------------------------------------------
//  <copyright file="XdpETokenResponse.cs" company="Microsoft">
//      Copyright (c) Microsoft. All rights reserved.
//      Internal use only.
//  </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.XboxTest.Xdts
{
    public class XdpETokenResponse
    {
        public string Message { get; set; }

        public bool IsError { get; set; }

        public XdtsTokenResponse Data { get; set; }

        public string CorrelationId { get; set; }
    }
}
