// -----------------------------------------------------------------------
//  <copyright file="WebPageResponse.cs" company="Microsoft">
//      Copyright (c) Microsoft. All rights reserved.
//      Internal use only.
//  </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.XboxTest.Xdts
{
    using System;
    using System.Net;

    using HtmlAgilityPack;

    public class WebPageResponse
    {
        public HtmlDocument Document { get; set; }

        public Uri WebPageRequestUri { get; set; }

        public CookieContainer ResponseCookies { get; set; }
    }
}
