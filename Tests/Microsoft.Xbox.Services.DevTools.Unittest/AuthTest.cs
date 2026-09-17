// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Unittest
{
    using System;
    using System.Globalization;
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
        private const string DefaultEToken = "etoken";
        private const string DefaultScid = "00000000-0000-0000-0000-012345678901";
        private const string DefaultSandbox = "sandbox";
        private const string DefaultXtdsEndpoint = "http://XtdsEndpoint.com";
        private const string DefaultId = "id";
        private const string DefaultName = "name";
        private const string DefaultAccountId = "accountid";
        private const string DefaultAccountType = "accounttype";
        private const string DefaultMoniker = "moniker";
        private const string DefaultTestAccountUserName = "tester@xboxtest.com";
        private const string DefaultTestAccountSandbox = "XXXXXX.0";
        private const string DefaultXToken = "xtoken";
        private const string DefaultUserHash = "userhash";
        private const string DefaultGamertag = "gamertag";
        private const string DefaultXuid = "2814000000000000";

        private Mock<IAuthContext> authMock;

        public void ComposeETokenPayload(TimeSpan expireTime, string scid, string sandbox, out string request, out string response)
        {
            request = JsonConvert.SerializeObject(new XdtsTokenRequest(scid, string.IsNullOrEmpty(sandbox)? null : new string[] { sandbox }));

            var utcNowString = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            var expiredTimeString = (DateTime.UtcNow + expireTime).ToString("o", CultureInfo.InvariantCulture);

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

        private Mock<IAuthContext> SetupMockTestAccountAuth()
        {
            var testAuthMock = new Mock<IAuthContext>();

            testAuthMock.Setup(o => o.UserName).Returns(DefaultTestAccountUserName);
            testAuthMock.Setup(o => o.AccountSource).Returns(DevAccountSource.TestAccount);
            testAuthMock.Setup(o => o.Tenant).Returns("consumers");
            testAuthMock.Setup(o => o.XtdsEndpoint).Returns(ClientSettings.Singleton.XASUEndpoint);

            return testAuthMock;
        }

        private void ComposeXstsPayload(out string xasuResponse, out string xstsResponse)
        {
            var utcNowString = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            var expiredTimeString = (DateTime.UtcNow + new TimeSpan(1, 0, 0)).ToString("o", CultureInfo.InvariantCulture);

            xasuResponse = $"{{'IssueInstant':'{utcNowString}','NotAfter':'{expiredTimeString}','Token':'xasutoken','DisplayClaims':{{'xui':[{{'uhs':'{DefaultUserHash}'}}]}}}}";
            xstsResponse = $"{{'IssueInstant':'{utcNowString}','NotAfter':'{expiredTimeString}','Token':'{DefaultXToken}','DisplayClaims':{{'xui':[{{'uhs':'{DefaultUserHash}','gtg':'{DefaultGamertag}','xid':'{DefaultXuid}'}}]}}}}";
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

            // The token caches are written to disk and outlive the run. SignOut only drops tokens
            // whose "enm" claim matches the signed in user name, which the mocked account never
            // does, so a token cached by one test would otherwise be served to the next one and
            // make tests that assert on fetching depend on the order they run in.
            ToolAuthentication.Client.ETokenCache.Value.Clear();
            ToolAuthentication.Client.XTokenCache.Value.Clear();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestHook.MockHttpHandler = null;
            ToolAuthentication.SignOutTestAccount();

            try
            {
                ToolAuthentication.SignOut();
            }
            catch (InvalidOperationException)
            {
                // No developer account was signed in by this test.
            }
        }

        [TestMethod]
        public async Task SignInTestAccountSilentlyTest()
        {
            var mockHttp = new MockHttpMessageHandler();
            this.ComposeXstsPayload(out string xasuResponse, out string xstsResponse);

            mockHttp.Expect(ClientSettings.Singleton.XASUEndpoint).Respond("application/json", xasuResponse);
            mockHttp.Expect(ClientSettings.Singleton.XSTSEndpoint).Respond("application/json", xstsResponse);

            TestHook.MockHttpHandler = mockHttp;

            Mock<IAuthContext> testAuthMock = this.SetupMockTestAccountAuth();
            testAuthMock.Setup(o => o.AcquireTokenSilentAsync()).ReturnsAsync("msatoken");
            testAuthMock.Setup(o => o.AcquireTokenAsync()).ReturnsAsync("interactivemsatoken");

            TestAccount testAccount = await ToolAuthentication.SignInTestAccountAsync(DefaultTestAccountSandbox, testAuthMock.Object);

            Assert.AreEqual(DefaultGamertag, testAccount.Gamertag);
            Assert.AreEqual(DefaultXuid, testAccount.Xuid);
            Assert.AreEqual(DefaultUserHash, testAccount.UserHash);
            Assert.AreEqual(DefaultTestAccountUserName, testAccount.UserName);
            Assert.AreEqual(DefaultTestAccountSandbox, testAccount.Sandbox);

            // A cached credential must be enough, no sign in UI may be shown.
            testAuthMock.Verify(o => o.AcquireTokenAsync(), Times.Never);
            mockHttp.VerifyNoOutstandingExpectation();

            string token = await ToolAuthentication.GetTestTokenSilentlyAsync(DefaultTestAccountSandbox);
            Assert.AreEqual($"XBL3.0 x={DefaultUserHash};{DefaultXToken}", token);
        }

        [TestMethod]
        public async Task SignInTestAccountFallsBackToInteractiveTest()
        {
            var mockHttp = new MockHttpMessageHandler();
            this.ComposeXstsPayload(out string xasuResponse, out string xstsResponse);

            mockHttp.Expect(ClientSettings.Singleton.XASUEndpoint).Respond("application/json", xasuResponse);
            mockHttp.Expect(ClientSettings.Singleton.XSTSEndpoint).Respond("application/json", xstsResponse);

            TestHook.MockHttpHandler = mockHttp;

            Mock<IAuthContext> testAuthMock = this.SetupMockTestAccountAuth();
            testAuthMock.Setup(o => o.AcquireTokenSilentAsync())
                .ThrowsAsync(new InvalidOperationException("No cached user found."));
            testAuthMock.Setup(o => o.AcquireTokenAsync()).ReturnsAsync("interactivemsatoken");

            TestAccount testAccount = await ToolAuthentication.SignInTestAccountAsync(DefaultTestAccountSandbox, testAuthMock.Object);

            Assert.AreEqual(DefaultGamertag, testAccount.Gamertag);
            testAuthMock.Verify(o => o.AcquireTokenSilentAsync(), Times.Once);
            testAuthMock.Verify(o => o.AcquireTokenAsync(), Times.Once);
            mockHttp.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task SignInTestAccountReusesCachedXTokenTest()
        {
            var mockHttp = new MockHttpMessageHandler();
            this.ComposeXstsPayload(out string xasuResponse, out string xstsResponse);

            mockHttp.Expect(ClientSettings.Singleton.XASUEndpoint).Respond("application/json", xasuResponse);
            mockHttp.Expect(ClientSettings.Singleton.XSTSEndpoint).Respond("application/json", xstsResponse);

            TestHook.MockHttpHandler = mockHttp;

            Mock<IAuthContext> testAuthMock = this.SetupMockTestAccountAuth();
            testAuthMock.Setup(o => o.AcquireTokenSilentAsync()).ReturnsAsync("msatoken");

            await ToolAuthentication.SignInTestAccountAsync(DefaultTestAccountSandbox, testAuthMock.Object);
            mockHttp.VerifyNoOutstandingExpectation();

            // A second sign in of the same account and sandbox must be served entirely from the
            // token cache, without contacting MSAL or Xbox Live again.
            Mock<IAuthContext> secondAuthMock = this.SetupMockTestAccountAuth();
            secondAuthMock.Setup(o => o.AcquireTokenSilentAsync())
                .ThrowsAsync(new InvalidOperationException("Token acquisition should not be needed."));
            secondAuthMock.Setup(o => o.AcquireTokenAsync())
                .ThrowsAsync(new InvalidOperationException("Interactive sign in should not be needed."));

            TestAccount cachedAccount = await ToolAuthentication.SignInTestAccountAsync(DefaultTestAccountSandbox, secondAuthMock.Object);

            Assert.AreEqual(DefaultGamertag, cachedAccount.Gamertag);
            Assert.AreEqual(DefaultXuid, cachedAccount.Xuid);
            secondAuthMock.Verify(o => o.AcquireTokenSilentAsync(), Times.Never);
            secondAuthMock.Verify(o => o.AcquireTokenAsync(), Times.Never);
        }

        [TestMethod]
        public async Task LoadLastSignedInTestAccountTest()
        {
            var mockHttp = new MockHttpMessageHandler();
            this.ComposeXstsPayload(out string xasuResponse, out string xstsResponse);

            mockHttp.Expect(ClientSettings.Singleton.XASUEndpoint).Respond("application/json", xasuResponse);
            mockHttp.Expect(ClientSettings.Singleton.XSTSEndpoint).Respond("application/json", xstsResponse);

            TestHook.MockHttpHandler = mockHttp;

            Mock<IAuthContext> testAuthMock = this.SetupMockTestAccountAuth();
            testAuthMock.Setup(o => o.AcquireTokenSilentAsync()).ReturnsAsync("msatoken");

            await ToolAuthentication.SignInTestAccountAsync(DefaultTestAccountSandbox, testAuthMock.Object);

            TestAccount reloaded = ToolAuthentication.LoadLastSignedInTestAccount();

            Assert.IsNotNull(reloaded);
            Assert.AreEqual(DefaultTestAccountUserName, reloaded.UserName);
            Assert.AreEqual(DefaultTestAccountSandbox, reloaded.Sandbox);
            Assert.AreEqual(DefaultGamertag, reloaded.Gamertag);
            Assert.AreEqual(DefaultXuid, reloaded.Xuid);

            ToolAuthentication.SignOutTestAccount();
            Assert.IsNull(ToolAuthentication.LoadLastSignedInTestAccount());
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
