using System;
using System.Collections.Generic;
using System.Net;
using System.Security;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;
using Moq;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;

namespace Microsoft.Xbox.Services.Tool.Unittest
{
    [TestClass]
    public class AuthTest
    {
        private const string DefaultEToken = "defaultEToken";
        private const string DefaultEScid = "scid";
        private const string DefaultESandbox = "sandbox";
        private const string DefaultXtdsEndpoint = "http://XtdsEndpoint.com";
        private const string DefaultId = "id";
        private const string DefaultName = "name";
        private const string DefaultAccountId = "accountid";
        private const string DefaultAccountType = "accounttype";
        private const string DefaultMoniker = "moniker";

        public void ComposeETokenPayload(TimeSpan expireTime, string scid, string sandbox, out string request, out string response)
        {
            request = JsonConvert.SerializeObject(new XdtsTokenRequest(scid, sandbox));

            var utcNowString = DateTime.UtcNow.ToString();
            var expiredTimeString = (DateTime.UtcNow + expireTime).ToString();

            response = $"{{'IssueInstant':'{utcNowString}','NotAfter':'{expiredTimeString}','Token':'{DefaultEToken+scid+sandbox}','DisplayClaims':{{'eid':'{DefaultId}','enm':'{DefaultName}','eai':'{DefaultAccountId}','eam':'{DefaultMoniker}','eat':'{DefaultAccountType}'}}}}";
        }

        private static void SetupMockAad()
        {
            bool hasCredential = false;
            var authMock = new Mock<IAuthContext>();
            authMock.Setup(o => o.AcquireTokenSilentAsync())
                .Callback(()=> hasCredential = true)
                .ReturnsAsync("aadtoken");

            authMock.Setup(o => o.AcquireTokenAsync(It.IsAny<string>()))
                .Callback(() =>hasCredential = true)
                .ReturnsAsync("aadtoken");

            authMock.Setup(o => o.AccountSource).Returns(DevAccountSource.UniversalDeveloperCenter);

            authMock.Setup(o => o.XtdsEndpoint).Returns(DefaultXtdsEndpoint);

            authMock.Setup(o => o.HasCredential).Returns(() => hasCredential);

            Auth.Client = new AuthClient(authMock.Object);
        }

        [TestInitialize]
        public void TestInit()
        {
            SetupMockAad();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Auth.Client = null;
            TestHook.MockHttpHandler = null;
        }

        [TestMethod]
        public async Task GetETokenTest()
        {
            var mockHttp = new MockHttpMessageHandler();

            string defaultRequest, defaultXdtsResponse, sandboxRequest, sandboxResponse;
            ComposeETokenPayload(new TimeSpan(1, 0, 0), string.Empty, string.Empty, out defaultRequest,
                out defaultXdtsResponse);

            ComposeETokenPayload(new TimeSpan(1, 0, 0), DefaultEScid, DefaultESandbox, out sandboxRequest,
                out sandboxResponse);

            mockHttp.Expect(DefaultXtdsEndpoint)
                .WithContent(defaultRequest)
                .Respond("application/json", defaultXdtsResponse);

            mockHttp.Expect(DefaultXtdsEndpoint)
                .WithContent(sandboxRequest)
                .Respond("application/json", sandboxResponse);

            TestHook.MockHttpHandler = mockHttp;

            var devAccount = await Auth.SignIn(DevAccountSource.UniversalDeveloperCenter, string.Empty);
            Assert.AreEqual(DefaultId, devAccount.Id);
            Assert.AreEqual(DefaultName, devAccount.Name);
            Assert.AreEqual(DefaultAccountId, devAccount.AccountId);
            Assert.AreEqual(DefaultMoniker, devAccount.AccountMoniker);
            Assert.AreEqual(DefaultAccountType, devAccount.AccountType);
            Assert.AreEqual(DevAccountSource.UniversalDeveloperCenter, devAccount.AccountSource);
            Assert.IsTrue(Auth.HasAuthInfo);

            var token2 = await Auth.GetETokenSilentlyAsync(DefaultEScid, DefaultESandbox);
            Assert.AreEqual(token2, Auth.PrepareForAuthHeader(DefaultEToken+DefaultEScid+DefaultESandbox));
        }


        [TestMethod]
        public async Task GetETokenFailTest()
        {
            SetupMockAad();

            var mockHttp = new MockHttpMessageHandler();

            string defaultRequest, defaultXdtsResponse;
            ComposeETokenPayload(new TimeSpan(1, 0, 0), string.Empty, string.Empty, out defaultRequest,
                out defaultXdtsResponse);

            mockHttp.Expect(DefaultXtdsEndpoint)
                .WithContent(defaultRequest)
                .Respond(HttpStatusCode.BadRequest);

            TestHook.MockHttpHandler = mockHttp;

            try
            {
                await Auth.SignIn(DevAccountSource.UniversalDeveloperCenter, string.Empty);
            }
            catch (XboxLiveException ex)
            {
                Assert.IsFalse(string.IsNullOrEmpty(ex.Message));
                Assert.AreEqual(ex.Response.StatusCode, HttpStatusCode.BadRequest);
                Assert.IsTrue(Auth.HasAuthInfo);

                return;
            }

            Assert.Fail("No exception was thrown.");
        }

        [TestMethod]
        public async Task TokenRefreshTest()
        {
            SetupMockAad();

            var mockHttp = new MockHttpMessageHandler();

            string defaultRequest, defaultXdtsResponse;
            ComposeETokenPayload(TimeSpan.Zero, string.Empty, string.Empty, out defaultRequest,
                out defaultXdtsResponse);

            // Excepct to be hit twice
            mockHttp.Expect(DefaultXtdsEndpoint)
                .WithContent(defaultRequest)
                .Respond("application/json", defaultXdtsResponse);

            mockHttp.Expect(DefaultXtdsEndpoint)
                .WithContent(defaultRequest)
                .Respond("application/json", defaultXdtsResponse);

            TestHook.MockHttpHandler = mockHttp;

            var devAccount = await Auth.SignIn(DevAccountSource.UniversalDeveloperCenter, string.Empty);

            Assert.AreEqual(devAccount.Id, DefaultId);
            Assert.AreEqual(devAccount.Name, DefaultName);
            Assert.AreEqual(devAccount.AccountId, DefaultAccountId);
            Assert.AreEqual(devAccount.AccountMoniker, DefaultMoniker);
            Assert.AreEqual(devAccount.AccountType, DefaultAccountType);
            Assert.AreEqual(devAccount.AccountSource, DevAccountSource.UniversalDeveloperCenter);
            Assert.IsTrue(Auth.HasAuthInfo);

            var token = await Auth.GetETokenSilentlyAsync(string.Empty, string.Empty);
            Assert.AreEqual(token, Auth.PrepareForAuthHeader(DefaultEToken));

            mockHttp.VerifyNoOutstandingExpectation();

        }

        [TestMethod]
        public async Task TokenCacheTest()
        {
            SetupMockAad();

            var mockHttp = new MockHttpMessageHandler();

            string defaultRequest, defaultXdtsResponse;
            ComposeETokenPayload(new TimeSpan(1,0,0), string.Empty, string.Empty, out defaultRequest,
                out defaultXdtsResponse);

            // Excepct to be hit twice, the second call for token will be fetched from cache
            mockHttp.Expect(DefaultXtdsEndpoint)
                .WithContent(defaultRequest)
                .Respond("application/json", defaultXdtsResponse);

            TestHook.MockHttpHandler = mockHttp;

            var devAccount = await Auth.SignIn(DevAccountSource.UniversalDeveloperCenter, string.Empty);

            Assert.AreEqual(devAccount.Id, DefaultId);
            Assert.AreEqual(devAccount.Name, DefaultName);
            Assert.AreEqual(devAccount.AccountId, DefaultAccountId);
            Assert.AreEqual(devAccount.AccountMoniker, DefaultMoniker);
            Assert.AreEqual(devAccount.AccountType, DefaultAccountType);
            Assert.AreEqual(devAccount.AccountSource, DevAccountSource.UniversalDeveloperCenter);
            Assert.IsTrue(Auth.HasAuthInfo);

            var token = await Auth.GetETokenSilentlyAsync(string.Empty, string.Empty);
            Assert.AreEqual(token, Auth.PrepareForAuthHeader(DefaultEToken));

            mockHttp.VerifyNoOutstandingExpectation();

        }
    }
}
