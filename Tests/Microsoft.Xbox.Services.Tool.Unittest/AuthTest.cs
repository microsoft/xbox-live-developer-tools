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

        public void ComposeETokenPayload(TimeSpan expireTime, string scid, string sandbox, out string request, out string response)
        {
            request = JsonConvert.SerializeObject(new XDTSTokenRequest(scid, sandbox));

            var utcNowString = DateTime.UtcNow.ToString();
            var expiredTimeString = (DateTime.UtcNow + expireTime).ToString();

            response = $"{{'IssueInstant':'{utcNowString}','NotAfter':'{expiredTimeString}','Token':'{DefaultEToken+scid+sandbox}','DisplayClaims':null}}";
        }

        private static void SetupMockAad()
        {
            bool hasCredential = false;
            var aadAuthMock = new Mock<AadAuthContext>();
            aadAuthMock.Setup(o => o.AcquireTokenSilentAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Callback(()=> hasCredential = true)
                .ReturnsAsync("aadtoken");

            aadAuthMock.Setup(o => o.AcquireTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserCredential>()))
                .Callback(() =>hasCredential = true)
                .ReturnsAsync("aadtoken");

            aadAuthMock.Setup(o => o.AcquireTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Uri>(),
                    It.IsAny<IPlatformParameters>(), It.IsAny<UserIdentifier>()))
                .Callback(() =>hasCredential = true)
                .ReturnsAsync("aadtoken");

            aadAuthMock.Setup(o => o.HasCredential).Returns(() => hasCredential);

            Auth.Client = new UDCAuthClient(aadAuthMock.Object);
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
        public async Task GetUDCETokenTest()
        {

            var mockHttp = new MockHttpMessageHandler();

            string defaultRequest, defaultXdtsResponse, sandboxRequest, sandboxResponse;
            ComposeETokenPayload(new TimeSpan(1, 0, 0), string.Empty, string.Empty, out defaultRequest,
                out defaultXdtsResponse);

            ComposeETokenPayload(new TimeSpan(1, 0, 0), DefaultEScid, DefaultESandbox, out sandboxRequest,
                out sandboxResponse);

            mockHttp.Expect(ClientSettings.Singleton.UDCAuthEndpoint)
                .WithContent(defaultRequest)
                .Respond("application/json", defaultXdtsResponse);

            mockHttp.Expect(ClientSettings.Singleton.UDCAuthEndpoint)
                .WithContent(sandboxRequest)
                .Respond("application/json", sandboxResponse);

            TestHook.MockHttpHandler = mockHttp;

            var token = await Auth.GetUDCEToken("userName", new SecureString());
            Assert.AreEqual(token, Auth.PrepareForAuthHeader(DefaultEToken));
            Assert.IsTrue(Auth.HasAuthInfo);

            var token2 = await Auth.GetETokenSilentlyAsync(DefaultEScid, DefaultESandbox);
            Assert.AreEqual(token2, Auth.PrepareForAuthHeader(DefaultEToken+DefaultEScid+DefaultESandbox));
        }


        [TestMethod]
        public async Task GetUDCETokenFailTest()
        {
            SetupMockAad();

            var mockHttp = new MockHttpMessageHandler();

            string defaultRequest, defaultXdtsResponse;
            ComposeETokenPayload(new TimeSpan(1, 0, 0), string.Empty, string.Empty, out defaultRequest,
                out defaultXdtsResponse);

            mockHttp.Expect(ClientSettings.Singleton.UDCAuthEndpoint)
                .WithContent(defaultRequest)
                .Respond(HttpStatusCode.BadRequest);

            TestHook.MockHttpHandler = mockHttp;

            try
            {
                var token = await Auth.GetUDCEToken("userName", new SecureString());
                Assert.AreEqual(token, Auth.PrepareForAuthHeader(DefaultEToken));
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
            mockHttp.Expect(ClientSettings.Singleton.UDCAuthEndpoint)
                .WithContent(defaultRequest)
                .Respond("application/json", defaultXdtsResponse);

            mockHttp.Expect(ClientSettings.Singleton.UDCAuthEndpoint)
                .WithContent(defaultRequest)
                .Respond("application/json", defaultXdtsResponse);

            TestHook.MockHttpHandler = mockHttp;

            var token = await Auth.GetUDCEToken("userName", new SecureString());
            Assert.AreEqual(token, Auth.PrepareForAuthHeader(DefaultEToken));

            var token1 = await Auth.GetETokenSilentlyAsync(string.Empty, string.Empty);
            Assert.AreEqual(token, token1);

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
            mockHttp.Expect(ClientSettings.Singleton.UDCAuthEndpoint)
                .WithContent(defaultRequest)
                .Respond("application/json", defaultXdtsResponse);

            TestHook.MockHttpHandler = mockHttp;

            var token = await Auth.GetUDCEToken("userName", new SecureString());
            Assert.AreEqual(token, Auth.PrepareForAuthHeader(DefaultEToken));

            var token1 = await Auth.GetETokenSilentlyAsync(string.Empty, string.Empty);
            Assert.AreEqual(token, token1);

            mockHttp.VerifyNoOutstandingExpectation();

        }
    }
}
