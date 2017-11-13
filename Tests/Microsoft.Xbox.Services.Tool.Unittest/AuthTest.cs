// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Xbox.Services.Tool.Unittest
{
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

        public void ComposeETokenPayload(TimeSpan expireTime, string scid, string sandbox, out string request, out string response)
        {
            request = JsonConvert.SerializeObject(new XdtsTokenRequest(scid, sandbox));

            var utcNowString = DateTime.UtcNow.ToString();
            var expiredTimeString = (DateTime.UtcNow + expireTime).ToString();

            response = $"{{'IssueInstant':'{utcNowString}','NotAfter':'{expiredTimeString}','Token':'{DefaultEToken+scid+sandbox}','DisplayClaims':{{'eid':'{DefaultId}','enm':'{DefaultName}','eai':'{DefaultAccountId}','eam':'{DefaultMoniker}','eat':'{DefaultAccountType}'}}}}";
        }

        private static void SetupMockAad()
        {
            var authMock = new Mock<IAuthContext>();
            authMock.Setup(o => o.AcquireTokenSilentAsync())
                .ReturnsAsync("aadtoken");

            authMock.Setup(o => o.AcquireTokenAsync(It.IsAny<string>()))
                .ReturnsAsync("aadtoken");

            authMock.Setup(o => o.AccountSource).Returns(DevAccountSource.UniversalDeveloperCenter);

            authMock.Setup(o => o.XtdsEndpoint).Returns(DefaultXtdsEndpoint);

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

            ComposeETokenPayload(new TimeSpan(1, 0, 0), DefaultScid, DefaultSandbox, out sandboxRequest,
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
            Assert.IsTrue(Auth.IsSignedIn);

            var token2 = await Auth.GetETokenSilentlyAsync(DefaultScid, DefaultSandbox);
            Assert.AreEqual(token2, Auth.PrepareForAuthHeader(DefaultEToken+DefaultScid+DefaultSandbox));
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
                Assert.IsFalse(Auth.IsSignedIn);

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
            Assert.IsTrue(Auth.IsSignedIn);

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
            Assert.IsTrue(Auth.IsSignedIn);

            var token = await Auth.GetETokenSilentlyAsync(string.Empty, string.Empty);
            Assert.AreEqual(token, Auth.PrepareForAuthHeader(DefaultEToken));

            mockHttp.VerifyNoOutstandingExpectation();

        }
    }
}
