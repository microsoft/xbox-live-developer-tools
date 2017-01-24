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
    using HtmlAgilityPack;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Security;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;

    public class XdpAuthClient
    {
        private const string XdpAuthorizeUserPath = "/User/Authorize";
        private ConcurrentDictionary<string, XdtsTokenResponse> cachedTokens = new ConcurrentDictionary<string, XdtsTokenResponse>();
        private CookieContainer authCookies = null;

        public XdpAuthClient()
        {
        }

        public bool HasAuthCookie()
        {
            return this.authCookies != null;
        }

        public async Task<string> GetETokenSilentlyAsync(string sandbox = "")
        {
            XdtsTokenResponse cachedToken;

            // return cachaed token if we have one and didn't expire
            if (this.cachedTokens.TryGetValue(sandbox, out cachedToken)
                && (cachedToken != null && !string.IsNullOrEmpty(cachedToken.Token) && cachedToken.NotAfter >= DateTime.UtcNow))
            {
                return cachedToken.Token;
            }
            else if (this.authCookies != null)
            {
                return await GetETokenInternalAsync(this.authCookies, sandbox);
            }

            throw new XboxLiveException("No auth info");
        }

        public async Task<string> GetETokenAsync(string emailaddress, SecureString password, string sandboxId = null)
        {
            CookieContainer xdpAuthenticationCookies = await this.GetXdpAuthenticationCookies(emailaddress, password);

            return await GetETokenInternalAsync(xdpAuthenticationCookies, sandboxId);
        }

        public async Task<string> GetETokenInternalAsync(CookieContainer authCookie, string sandboxId)
        {
            string xdpETokenPath = string.IsNullOrWhiteSpace(sandboxId) ?
                XdpAuthorizeUserPath :
                string.Format("{0}?sandboxId={1}", XdpAuthorizeUserPath, sandboxId);

            Uri xdpAuthorizeUri = new Uri(ClientSettings.Singleton.XdpBaseUri, xdpETokenPath);
            using (HttpResponseMessage response = await SendRequestAsync(HttpMethod.Get, xdpAuthorizeUri, null, authCookie))
            {
                if (response.Content == null)
                {
                    throw new XboxLiveException("XdpETokenResponse was null.", response, null);
                }

                try
                {
                    string json = await response.Content.ReadAsStringAsync();
                    XdpETokenResponse xdpETokenResponse = JsonConvert.DeserializeObject<XdpETokenResponse>(json);
                    if (xdpETokenResponse.Data != null && xdpETokenResponse.Data.Token != null)
                    {
                        // save all cached data
                        this.authCookies = authCookie;
                        this.cachedTokens[sandboxId] = xdpETokenResponse.Data;
                        return xdpETokenResponse.Data.Token;
                    }
                    else
                    {
                        throw new XboxLiveException("Invalid etoken format", response, null);
                    }

                }
                catch (JsonException ex)
                {
                    throw new XboxLiveException("Failed to deserialize eToken response.", response, ex);
                }
            }
        }

        private async Task<CookieContainer> GetXdpAuthenticationCookies(string emailaddress, SecureString password)
        {
            if (emailaddress.IndexOf("@microsoft.com", StringComparison.OrdinalIgnoreCase) > 0)
            {
                throw new XboxLiveException("Microsoft corporate accounts are not supported.");
            }

            WebPageResponse stsSignInPage = await GetStsSignInPageAsync(ClientSettings.Singleton.XdpBaseUri);
            NetworkCredential credentials = new NetworkCredential(emailaddress, password);

            FormUrlEncodedContent stsSignInContent = new FormUrlEncodedContent(
                new[]
                {
                    new KeyValuePair<string, string>("__VIEWSTATE", stsSignInPage.Document.GetElementbyId("__VIEWSTATE").Attributes["value"].Value),
                    new KeyValuePair<string, string>("__EVENTVALIDATION", stsSignInPage.Document.GetElementbyId("__EVENTVALIDATION").Attributes["value"].Value),
                    new KeyValuePair<string, string>("__db", stsSignInPage.Document.DocumentNode.SelectSingleNode("//input[@name='__db']").Attributes["value"].Value),
                    new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$PassiveIdentityProvidersDropDownList", ClientSettings.Singleton.WindowsLiveAuthenticationType),
                    new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$PassiveSignInButton", "Continue to Sign In")
                });
            
            WebPageResponse windowsLiveSignInResponse = await this.SignInToWindowsLiveAsync(credentials, stsSignInPage.WebPageRequestUri, stsSignInContent);
            HttpResponseMessage adfsAuthenticatedResponse = await this.GetStsAdfsAuthenticatedResponseAsync(ClientSettings.Singleton.StsAdfsAuthenticationUri, windowsLiveSignInResponse.Document, windowsLiveSignInResponse.ResponseCookies);
            return await this.ExtractAuthenticationCookiesFromXdpResponseAsync(adfsAuthenticatedResponse);
        }

        private async Task<CookieContainer> ExtractAuthenticationCookiesFromXdpResponseAsync(HttpResponseMessage adfsAuthenticatedResponse)
        {
            using (adfsAuthenticatedResponse)
            {
                HtmlDocument adfsDocument = await GetHtmlDocumentAsync(adfsAuthenticatedResponse);
                FormUrlEncodedContent authorizedXdpContent = new FormUrlEncodedContent(new[]
                   {
                        new KeyValuePair<string, string>("wa", adfsDocument.DocumentNode.SelectSingleNode("//input[@name='wa']").Attributes["value"].Value),
                        new KeyValuePair<string, string>("wresult", HttpUtility.HtmlDecode(adfsDocument.DocumentNode.SelectSingleNode("//input[@name='wresult']").Attributes["value"].Value)),
                        new KeyValuePair<string, string>("wctx", "rm=0&id=passive&ru=/")
                    });

                CookieContainer xdpAuthenticationCookies = new CookieContainer();
                await SendRequestAsync(HttpMethod.Post, ClientSettings.Singleton.XdpBaseUri, authorizedXdpContent, xdpAuthenticationCookies);
                return xdpAuthenticationCookies;
            }
        }

        private async Task<WebPageResponse> SignInToWindowsLiveAsync(NetworkCredential credentials, Uri xdpBaseUri, FormUrlEncodedContent signInContent)
        {
            CookieContainer signInCookies = new CookieContainer();

            using (HttpResponseMessage response = await SendRequestAsync(HttpMethod.Post, xdpBaseUri, signInContent, signInCookies))
            {
                CookieContainer windowsLiveCookies = ExtractSetCookies(response);
                windowsLiveCookies.Add(signInCookies.GetCookies(new Uri(xdpBaseUri.AbsoluteUri.Substring(0, xdpBaseUri.AbsoluteUri.IndexOf(xdpBaseUri.Query, StringComparison.Ordinal)))));
                HtmlDocument windowsLiveDocument = await GetHtmlDocumentAsync(response);

                using (HttpResponseMessage signInResponse = await this.DoSignInToWindowsLiveAsync(credentials, response.RequestMessage.RequestUri, windowsLiveDocument, windowsLiveCookies))
                {
                    return new WebPageResponse
                    {
                        Document = await GetHtmlDocumentAsync(signInResponse),
                        WebPageRequestUri = signInResponse.RequestMessage.RequestUri,
                        ResponseCookies = windowsLiveCookies
                    };
                }
            }
        }

        public async Task<HttpResponseMessage> GetStsAdfsAuthenticatedResponseAsync(Uri adfsUri, HtmlDocument signInDocument, CookieContainer cookies)
        {
            var node = signInDocument.GetElementbyId("wa");

            if (node == null)
            {
                throw new XboxLiveException("STS authentication failed. Check the password of the account.");
            }

            string wa = HttpUtility.UrlEncode(node.Attributes["value"].Value);
            var content = new FormUrlEncodedContent(
                new[]
                {
                    new KeyValuePair<string, string>("wctx", signInDocument.GetElementbyId("wctx").Attributes["value"].Value),
                    new KeyValuePair<string, string>("NAP", signInDocument.GetElementbyId("NAP").Attributes["value"].Value),
                    new KeyValuePair<string, string>("wresult", HttpUtility.HtmlDecode(signInDocument.GetElementbyId("wresult").Attributes["value"].Value)),
                    new KeyValuePair<string, string>("wa", wa),
                    new KeyValuePair<string, string>("ANON", signInDocument.GetElementbyId("ANON").Attributes["value"].Value)
                });

            return await SendRequestAsync(HttpMethod.Post, adfsUri, content, cookies);
        }

        private async Task<HttpResponseMessage> DoSignInToWindowsLiveAsync(NetworkCredential credentials, Uri baseUri, HtmlDocument windowsLiveDocument, CookieContainer windowsLiveCookies)
        {
            string body = windowsLiveDocument.DocumentNode.InnerText;
            const string StartString = "value=\"";
            int indexOfStartString = body.IndexOf(
                StartString, 
                body.IndexOf("<input type=\"hidden\" name=\"PPFT\"", StringComparison.Ordinal),
                StringComparison.Ordinal) + StartString.Length;

            int length = body.IndexOf("\"", indexOfStartString, StringComparison.Ordinal) - indexOfStartString;
            string hiddenValue = body.Substring(indexOfStartString, length);

            const string SearchValue = "&bk=";
            int searchValueStart = body.IndexOf(SearchValue, StringComparison.Ordinal) + SearchValue.Length;
            int searchValueEnd = body.IndexOf(@"'", searchValueStart, StringComparison.Ordinal);
            string queryStringValue = body.Substring(searchValueStart, searchValueEnd - searchValueStart);

            // Siging into Windows Live
            UriBuilder uriBuilder = new UriBuilder(ClientSettings.Singleton.WindowsLiveUri)
            {
                Path = "/ppsecure/post.srf",
                Query = baseUri.Query.Substring(1) + SearchValue + queryStringValue
            };

            FormUrlEncodedContent content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("login", credentials.UserName),
                    new KeyValuePair<string, string>("passwd", credentials.Password),
                    new KeyValuePair<string, string>("type", "11"),
                    new KeyValuePair<string, string>("PPSX", "PassportRN"),
                    ////new KeyValuePair<string, string>("PPSX", "Passpor"),
                    new KeyValuePair<string, string>("PPFT", hiddenValue),
                    new KeyValuePair<string, string>("idsbho", "1"),
                    new KeyValuePair<string, string>("sso", "0"),
                    new KeyValuePair<string, string>("NewUser", "1"),
                    new KeyValuePair<string, string>("LoginOptions", "3"),
                    new KeyValuePair<string, string>("i1", "0"),
                    new KeyValuePair<string, string>("i2", "1"),
                    new KeyValuePair<string, string>("i3", "13622"),
                    new KeyValuePair<string, string>("i4", "0"),
                    new KeyValuePair<string, string>("i7", "0"),
                    new KeyValuePair<string, string>("i12", "1"),
                    new KeyValuePair<string, string>("i13", "0"),
                    new KeyValuePair<string, string>("i14", "70"),
                    new KeyValuePair<string, string>("i15", "100"),
                    new KeyValuePair<string, string>("i16", "244"),
                    new KeyValuePair<string, string>("i17", "0"),
                    new KeyValuePair<string, string>("i18", "__Login_Strings|1,__Login_Core|1,")
                });

            return await SendRequestAsync(HttpMethod.Post, uriBuilder.Uri, content, windowsLiveCookies);
        }

        private static async Task<WebPageResponse> GetStsSignInPageAsync(Uri xdpBaseUrl)
        {
            using (HttpResponseMessage response = await SendRequestAsync(HttpMethod.Get, xdpBaseUrl))
            {
                return new WebPageResponse
                {
                    Document = await GetHtmlDocumentAsync(response),
                    WebPageRequestUri = response.RequestMessage.RequestUri,
                    ResponseCookies = null
                };
            }
        }

        private static CookieContainer ExtractSetCookies(HttpResponseMessage response)
        {
            IEnumerable<string> setCookieValues;
            CookieContainer cookies = new CookieContainer();
            if (response.Headers.TryGetValues("Set-Cookie", out setCookieValues))
            {
                var cookieValues = string.Empty;
                foreach (var strValue in setCookieValues)
                {
                    cookieValues += strValue.Substring(0, strValue.IndexOf(';')) + ",";
                }

                cookies.SetCookies(
                    new Uri(
                        string.Format("{0}://{1}/", response.RequestMessage.RequestUri.Scheme, response.RequestMessage.RequestUri.Host)), 
                        cookieValues);
            }

            return cookies;
        }

        private static async Task<HtmlDocument> GetHtmlDocumentAsync(HttpResponseMessage response)
        {
            Stream stream = await response.Content.ReadAsStreamAsync();
            if (stream == null)
            {
                throw new XboxLiveException(
                    string.Format(
                        "The response stream from: {0} was null.",
                        response.RequestMessage.RequestUri), response, null);
            }

            HtmlDocument document = new HtmlDocument();
            document.Load(stream, Encoding.Default);
            return document;
        }

        private static async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, Uri uri, HttpContent content = null, CookieContainer cookies = null)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.AllowAutoRedirect = true;

            if (cookies != null)
            {
                handler.UseCookies = true;
                handler.CookieContainer = cookies;
            }

            using (HttpClient client = new HttpClient(handler))
            using (HttpRequestMessage request = new HttpRequestMessage(method, uri))
            {
                if (content != null)
                {
                    request.Content = content;
                }

                HttpResponseMessage response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new XboxLiveException(
                        string.Format(
                            "Request to {0} failed. StatusCode: {1} ReasonPhrase: {2}",
                            response.RequestMessage.RequestUri,
                            response.StatusCode,
                            response.ReasonPhrase), response, null);
                }

                    return response;
            }
        }
    }
}
