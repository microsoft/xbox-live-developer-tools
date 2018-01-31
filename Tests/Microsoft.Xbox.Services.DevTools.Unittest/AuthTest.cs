// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Unittest
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Xbox.Services.DevTools.Authentication;
    using Microsoft.Xbox.Services.DevTools.Common;
    using Moq;
    using Newtonsoft.Json;
    using RichardSzalay.MockHttp;

    [TestClass]
    public class AuthTest
    {
        private const string DefaultEToken = "defaultEToken";
        private const string DefaultScid = "scid";
        private const string DefaultSandbox = "sandbox";
        private const string DefaultXtdsEndpoint = "http://XtdsEndpoint.com";
        private const string DefaultId = "id";
        private const string DefaultName = "name";
        private const string DefaultAccountId = "accountid";
        private const string DefaultAccountType = "accounttype";
        private const string DefaultMoniker = "moniker";

        private Mock<IAuthContext> authMock;

        public void ComposeETokenPayload(TimeSpan expireTime, string scid, string sandbox, out string request, out string response)
        {
            request = JsonConvert.SerializeObject(new XdtsTokenRequest(scid, string.IsNullOrEmpty(sandbox)? null : new string[] { sandbox }));

            var utcNowString = DateTime.UtcNow.ToString();
            var expiredTimeString = (DateTime.UtcNow + expireTime).ToString();

            response = $"{{'IssueInstant':'{utcNowString}','NotAfter':'{expiredTimeString}','Token':'{DefaultEToken+scid+sandbox}','DisplayClaims':{{'eid':'{DefaultId}','enm':'{DefaultName}','eai':'{DefaultAccountId}','eam':'{DefaultMoniker}','eat':'{DefaultAccountType}'}}}}";
        }

        private void SetupMockAad()
        {
            this.authMock = new Mock<IAuthContext>();
            this.authMock.Setup(o => o.AcquireTokenSilentAsync())
                .ReturnsAsync("aadtoken");

            this.authMock.Setup(o => o.AcquireTokenAsync())
                .ReturnsAsync("aadtoken");

            this.authMock.Setup(o => o.UserName).Returns(string.Empty);
            this.authMock.Setup(o => o.AccountSource).Returns(DevAccountSource.WindowsDevCenter);
            this.authMock.Setup(o => o.XtdsEndpoint).Returns(DefaultXtdsEndpoint);
        }

        private async Task<DevAccount> SignInAsync(DevAccountSource accountSource, string userName)
        {
            return await ToolAuthentication.SignInAsync(accountSource, userName, this.authMock.Object);
        }

        [TestInitialize]
        public void TestInit()
        {
            this.SetupMockAad();
            ClientSettings.Singleton.CacheFolder = ".\\tokencache";
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestHook.MockHttpHandler = null;
            ToolAuthentication.SignOut();
        }

        [TestMethod]
        public async Task GetETokenTest()
        {
            var mockHttp = new MockHttpMessageHandler();

            this.ComposeETokenPayload(new TimeSpan(1, 0, 0), string.Empty, string.Empty, out string defaultRequest,
                out string defaultXdtsResponse);

            this.ComposeETokenPayload(new TimeSpan(1, 0, 0), DefaultScid, DefaultSandbox, out string sandboxRequest,
                out string sandboxResponse);

            mockHttp.Expect(DefaultXtdsEndpoint)
                .WithContent(defaultRequest)
                .Respond("application/json", defaultXdtsResponse);

            mockHttp.Expect(DefaultXtdsEndpoint)
                .WithContent(sandboxRequest)
                .Respond("application/json", sandboxResponse);

            TestHook.MockHttpHandler = mockHttp;

            var devAccount = await this.SignInAsync(DevAccountSource.WindowsDevCenter, string.Empty);
            Assert.AreEqual(DefaultId, devAccount.Id);
            Assert.AreEqual(DefaultName, devAccount.Name);
            Assert.AreEqual(DefaultAccountId, devAccount.AccountId);
            Assert.AreEqual(DefaultMoniker, devAccount.AccountMoniker);
            Assert.AreEqual(DefaultAccountType, devAccount.AccountType);
            Assert.AreEqual(DevAccountSource.WindowsDevCenter, devAccount.AccountSource);

            var token2 = await ToolAuthentication.GetDevTokenSilentlyAsync(DefaultScid, DefaultSandbox);
            Assert.AreEqual(token2, ToolAuthentication.PrepareForAuthHeader(DefaultEToken+DefaultScid+DefaultSandbox));
        }

        [TestMethod]
        public async Task GetETokenFailTest()
        {
            var mockHttp = new MockHttpMessageHandler();

            this.ComposeETokenPayload(new TimeSpan(1, 0, 0), string.Empty, string.Empty, out string defaultRequest,
                out string defaultXdtsResponse);

            mockHttp.Expect(DefaultXtdsEndpoint)
                .WithContent(defaultRequest)
                .Respond(HttpStatusCode.BadRequest);

            TestHook.MockHttpHandler = mockHttp;

            try
            {
                await this.SignInAsync(DevAccountSource.WindowsDevCenter, string.Empty);
            }
            catch (HttpRequestException ex)
            {
                Assert.IsFalse(string.IsNullOrEmpty(ex.Message));
                Assert.IsTrue(ex.Message.Contains("400"));
                return;
            }

            Assert.Fail("No exception was thrown.");
        }

        [TestMethod]
        public async Task TokenRefreshTest()
        {
            var mockHttp = new MockHttpMessageHandler();

            this.ComposeETokenPayload(TimeSpan.Zero, string.Empty, string.Empty, out string defaultRequest,
                out string defaultXdtsResponse);

            // Expect to be hit twice
            mockHttp.Expect(DefaultXtdsEndpoint)
                .WithContent(defaultRequest)
                .Respond("application/json", defaultXdtsResponse);

            TestHook.MockHttpHandler = mockHttp;

            var devAccount = await this.SignInAsync(DevAccountSource.WindowsDevCenter, string.Empty);

            Assert.AreEqual(devAccount.Id, DefaultId);
            Assert.AreEqual(devAccount.Name, DefaultName);
            Assert.AreEqual(devAccount.AccountId, DefaultAccountId);
            Assert.AreEqual(devAccount.AccountMoniker, DefaultMoniker);
            Assert.AreEqual(devAccount.AccountType, DefaultAccountType);
            Assert.AreEqual(devAccount.AccountSource, DevAccountSource.WindowsDevCenter);

            mockHttp.Expect(DefaultXtdsEndpoint)
                .WithContent(defaultRequest)
                .Respond("application/json", defaultXdtsResponse);

            var token = await ToolAuthentication.GetDevTokenSilentlyAsync(string.Empty, string.Empty);
            Assert.AreEqual(token, ToolAuthentication.PrepareForAuthHeader(DefaultEToken));

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task TokenCacheTest()
        {
            var mockHttp = new MockHttpMessageHandler();

            this.ComposeETokenPayload(new TimeSpan(1, 0, 0), string.Empty, string.Empty, out string defaultRequest,
                out string defaultXdtsResponse);

            // Expect to be hit twice, the second call for token will be fetched from cache
            mockHttp.Expect(DefaultXtdsEndpoint)
                .WithContent(defaultRequest)
                .Respond("application/json", defaultXdtsResponse);

            TestHook.MockHttpHandler = mockHttp;

            var devAccount = await this.SignInAsync(DevAccountSource.WindowsDevCenter, string.Empty);

            Assert.AreEqual(devAccount.Id, DefaultId);
            Assert.AreEqual(devAccount.Name, DefaultName);
            Assert.AreEqual(devAccount.AccountId, DefaultAccountId);
            Assert.AreEqual(devAccount.AccountMoniker, DefaultMoniker);
            Assert.AreEqual(devAccount.AccountType, DefaultAccountType);
            Assert.AreEqual(devAccount.AccountSource, DevAccountSource.WindowsDevCenter);

            var token = await ToolAuthentication.GetDevTokenSilentlyAsync(string.Empty, string.Empty);
            Assert.AreEqual(token, ToolAuthentication.PrepareForAuthHeader(DefaultEToken));

            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
