// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Unittest
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using DevTools.Authentication;
    using DevTools.Common;
    using DevTools.PlayerReset;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using RichardSzalay.MockHttp;

    [TestClass]
    public class PlayerResetTest
    {
        private const string DefaultUserName = "username";
        private const string DefaultScid = "00000000-0000-0000-0000-012345678901";
        private const string DefaultSandbox = "sandbox";
        private const string DefaultEtoken = "etoken";
        private const string DefaultXuid = "xuid";

        private void SetUpMockAuth()
        {
            var mockAuth = new Mock<AuthClient>();
            mockAuth.Setup(o => o.GetETokenAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()))
                .ReturnsAsync((string scid, IEnumerable<string> sandboxes, bool refresh) => DefaultEtoken + scid + string.Join(" ", sandboxes));
            ToolAuthentication.Client = mockAuth.Object;
            ToolAuthentication.SetAuthInfo(DevAccountSource.WindowsDevCenter, DefaultUserName, "common");
        }

        private string ExpectedToken(string scid, string sandbox)
        {
            return "XBL3.0 x=-;" + DefaultEtoken + scid + sandbox;
        }

        [TestInitialize]
        public void Init()
        {
            PlayerReset.RetryDelay = 0;
        }

        [TestMethod]
        public async Task ResetPlayerDataSuccess()
        {
            this.SetUpMockAuth();

            var mockHttp = new MockHttpMessageHandler();
            Guid jobId = Guid.NewGuid();
            var submitJobUri = new Uri(new Uri(ClientSettings.Singleton.OmegaResetToolEndpoint), "/submitJob");
            string submitJobResponse = $"\"{jobId}\"";
            string submitJobContent = $"{{\"userId\":\"{DefaultXuid}\",\"Scid\":\"{DefaultScid}\"}}";

            mockHttp.Expect(submitJobUri.ToString())
                .WithContent(submitJobContent)
                .WithHeaders("Authorization", this.ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond("application/json", submitJobResponse);

            var queryJobUri = new Uri(new Uri(ClientSettings.Singleton.OmegaResetToolEndpoint), "/jobs/"+jobId);
            string queryJobResponse = $"{{\'jobId\':'{jobId}','overallStatus':'CompletedSuccess'," +
                $"'providerStatus':[" +
                $"{{'provider':'StatsDelete','status':'CompletedSuccess'}}," +
                $"{{'provider':'TitleHistoryDelete','status':'CompletedSuccess'}}," +
                $"{{'provider':'AchievementsDelete','status':'CompletedSuccess'}}," +
                $"{{'provider':'GoalEngineDelete','status':'CompletedSuccess'}}," +
                $"{{'provider':'Leaderboards','status':'CompletedSuccess'}}" +
                $"]}}";

            mockHttp.Expect(queryJobUri.ToString())
                .WithHeaders("Authorization", this.ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond("application/json", queryJobResponse);

            TestHook.MockHttpHandler = mockHttp;

            var resetResult = await PlayerReset.ResetPlayerDataAsync(DefaultScid, DefaultSandbox, DefaultXuid);
            Assert.AreEqual(ResetOverallResult.Succeeded, resetResult.OverallResult);
            Assert.AreEqual(5, resetResult.ProviderStatus.Count);

            Assert.IsNull(resetResult.ProviderStatus[0].ErrorMessage);
            Assert.AreEqual("StatsDelete", resetResult.ProviderStatus[0].Provider);
            Assert.AreEqual(ResetProviderStatus.CompletedSuccess, resetResult.ProviderStatus[0].Status);

            Assert.IsNull(resetResult.ProviderStatus[1].ErrorMessage);
            Assert.AreEqual("TitleHistoryDelete", resetResult.ProviderStatus[1].Provider);
            Assert.AreEqual(ResetProviderStatus.CompletedSuccess, resetResult.ProviderStatus[1].Status);

            Assert.IsNull(resetResult.ProviderStatus[2].ErrorMessage);
            Assert.AreEqual("AchievementsDelete", resetResult.ProviderStatus[2].Provider);
            Assert.AreEqual(ResetProviderStatus.CompletedSuccess, resetResult.ProviderStatus[2].Status);

            Assert.IsNull(resetResult.ProviderStatus[3].ErrorMessage);
            Assert.AreEqual("GoalEngineDelete", resetResult.ProviderStatus[3].Provider);
            Assert.AreEqual(ResetProviderStatus.CompletedSuccess, resetResult.ProviderStatus[3].Status);

            Assert.IsNull(resetResult.ProviderStatus[4].ErrorMessage);
            Assert.AreEqual("Leaderboards", resetResult.ProviderStatus[4].Provider);
            Assert.AreEqual(ResetProviderStatus.CompletedSuccess, resetResult.ProviderStatus[4].Status);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task ResetPlayerDataSuccessWith400()
        {
            this.SetUpMockAuth();

            var mockHttp = new MockHttpMessageHandler();
            Guid jobId = Guid.NewGuid();
            var submitJobUri = new Uri(new Uri(ClientSettings.Singleton.OmegaResetToolEndpoint), "/submitJob");
            string submitJobResponse = $"\"{jobId}\"";
            string submitJobContent = $"{{\"userId\":\"{DefaultXuid}\",\"Scid\":\"{DefaultScid}\"}}";

            mockHttp.Expect(submitJobUri.ToString())
                .WithContent(submitJobContent)
                .WithHeaders("Authorization", this.ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond("application/json", submitJobResponse);

            var queryJobUri = new Uri(new Uri(ClientSettings.Singleton.OmegaResetToolEndpoint), "/jobs/" + jobId);
            string queryJobResponse = $"{{\'jobId\':'{jobId}','overallStatus':'CompletedSuccess'," +
                $"'providerStatus':[" +
                $"{{'provider':'StatsDelete','status':'CompletedSuccess'}}," +
                $"{{'provider':'TitleHistoryDelete','status':'CompletedSuccess'}}," +
                $"{{'provider':'AchievementsDelete','status':'CompletedSuccess'}}," +
                $"{{'provider':'GoalEngineDelete','status':'CompletedSuccess'}}," +
                $"{{'provider':'Leaderboards','status':'CompletedSuccess'}}" +
                $"]}}";

            // Job might not get created in time, first hit 400, then retry until gets the result
            mockHttp.Expect(queryJobUri.ToString())
                .WithHeaders("Authorization", this.ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond(HttpStatusCode.BadRequest);

            mockHttp.Expect(queryJobUri.ToString())
                .WithHeaders("Authorization", this.ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond(HttpStatusCode.BadRequest);

            mockHttp.Expect(queryJobUri.ToString())
                .WithHeaders("Authorization", this.ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond(HttpStatusCode.BadRequest);

            mockHttp.Expect(queryJobUri.ToString())
                .WithHeaders("Authorization", this.ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond("application/json", queryJobResponse);

            TestHook.MockHttpHandler = mockHttp;

            var resetResult = await PlayerReset.ResetPlayerDataAsync(DefaultScid, DefaultSandbox, DefaultXuid);
            Assert.AreEqual(ResetOverallResult.Succeeded, resetResult.OverallResult);
            Assert.AreEqual(5, resetResult.ProviderStatus.Count);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task ResetPlayerDataCompleteWithError()
        {
            this.SetUpMockAuth();

            var mockHttp = new MockHttpMessageHandler();
            Guid jobId = Guid.NewGuid();
            var submitJobUri = new Uri(new Uri(ClientSettings.Singleton.OmegaResetToolEndpoint), "/submitJob");
            string submitJobResponse = $"\"{jobId}\"";
            string submitJobContent = $"{{\"userId\":\"{DefaultXuid}\",\"Scid\":\"{DefaultScid}\"}}";

            mockHttp.Expect(submitJobUri.ToString())
                .WithContent(submitJobContent)
                .WithHeaders("Authorization", this.ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond("application/json", submitJobResponse);

            var queryJobUri = new Uri(new Uri(ClientSettings.Singleton.OmegaResetToolEndpoint), "/jobs/" + jobId);
            string queryJobResponse = $"{{\'jobId\':'{jobId}','overallStatus':'CompletedError'," +
                $"'providerStatus':[" +
                $"{{'provider':'StatsDelete','status':'CompletedSuccess'}}," +
                $"{{'provider':'TitleHistoryDelete','status':'CompletedError'}}," +
                $"{{'provider':'AchievementsDelete','status':'Abandoned'}}," +
                $"{{'provider':'GoalEngineDelete','status':'CompletedPartialSuccess', 'ErrorMessage':'error'}}," +
                $"{{'provider':'GoalEngineDelete1','status':'NotStarted'}}," +
                $"{{'provider':'GoalEngineDelete2','status':'Queued'}}," +
                $"{{'provider':'Leaderboards','status':'InProgress'}}" +
                $"]}}";

            mockHttp.Expect(queryJobUri.ToString())
                .WithHeaders("Authorization", this.ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond("application/json", queryJobResponse);

            TestHook.MockHttpHandler = mockHttp;

            var resetResult = await PlayerReset.ResetPlayerDataAsync(DefaultScid, DefaultSandbox, DefaultXuid);
            Assert.AreEqual(ResetOverallResult.CompletedError, resetResult.OverallResult);
            Assert.AreEqual(7, resetResult.ProviderStatus.Count);

            Assert.IsNull(resetResult.ProviderStatus[0].ErrorMessage);
            Assert.AreEqual("StatsDelete", resetResult.ProviderStatus[0].Provider);
            Assert.AreEqual(ResetProviderStatus.CompletedSuccess, resetResult.ProviderStatus[0].Status);

            Assert.IsNull(resetResult.ProviderStatus[1].ErrorMessage);
            Assert.AreEqual("TitleHistoryDelete", resetResult.ProviderStatus[1].Provider);
            Assert.AreEqual(ResetProviderStatus.CompletedError, resetResult.ProviderStatus[1].Status);

            Assert.IsNull(resetResult.ProviderStatus[2].ErrorMessage);
            Assert.AreEqual("AchievementsDelete", resetResult.ProviderStatus[2].Provider);
            Assert.AreEqual(ResetProviderStatus.Abandoned, resetResult.ProviderStatus[2].Status);

            Assert.AreEqual("error", resetResult.ProviderStatus[3].ErrorMessage);
            Assert.AreEqual("GoalEngineDelete", resetResult.ProviderStatus[3].Provider);
            Assert.AreEqual(ResetProviderStatus.CompletedPartialSuccess, resetResult.ProviderStatus[3].Status);

            Assert.IsNull(resetResult.ProviderStatus[4].ErrorMessage);
            Assert.AreEqual("GoalEngineDelete1", resetResult.ProviderStatus[4].Provider);
            Assert.AreEqual(ResetProviderStatus.NotStarted, resetResult.ProviderStatus[4].Status);

            Assert.IsNull(resetResult.ProviderStatus[5].ErrorMessage);
            Assert.AreEqual("GoalEngineDelete2", resetResult.ProviderStatus[5].Provider);
            Assert.AreEqual(ResetProviderStatus.Queued, resetResult.ProviderStatus[5].Status);

            Assert.IsNull(resetResult.ProviderStatus[6].ErrorMessage);
            Assert.AreEqual("Leaderboards", resetResult.ProviderStatus[6].Provider);
            Assert.AreEqual(ResetProviderStatus.InProgress, resetResult.ProviderStatus[6].Status);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [TestMethod]
        public async Task ResetPlayerDataSuccessTimeOut()
        {
            this.SetUpMockAuth();

            var mockHttp = new MockHttpMessageHandler();
            Guid jobId = Guid.NewGuid();
            var submitJobUri = new Uri(new Uri(ClientSettings.Singleton.OmegaResetToolEndpoint), "/submitJob");
            string submitJobResponse = $"\"{jobId}\"";
            string submitJobContent = $"{{\"userId\":\"{DefaultXuid}\",\"Scid\":\"{DefaultScid}\"}}";

            mockHttp.Expect(submitJobUri.ToString())
                .WithContent(submitJobContent)
                .WithHeaders("Authorization", this.ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond("application/json", submitJobResponse);

            var queryJobUri = new Uri(new Uri(ClientSettings.Singleton.OmegaResetToolEndpoint), "/jobs/" + jobId);
            string queryJobResponse = $"{{\'jobId\':'{jobId}','overallStatus':'InProgress'}}";

            var mockRequest = mockHttp.When(queryJobUri.ToString())
                .WithHeaders("Authorization", this.ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond("application/json", queryJobResponse);

            TestHook.MockHttpHandler = mockHttp;

            var resetResult = await PlayerReset.ResetPlayerDataAsync(DefaultScid, DefaultSandbox, DefaultXuid);
            Assert.AreEqual(ResetOverallResult.Timeout, resetResult.OverallResult);
            Assert.AreEqual(0, resetResult.ProviderStatus.Count);

            mockHttp.VerifyNoOutstandingExpectation();
            Assert.AreEqual(PlayerReset.MaxPollingAttempts, mockHttp.GetMatchCount(mockRequest));
        }

        [TestMethod]
        public async Task ResetPlayerDataSubmitJobError()
        {
            this.SetUpMockAuth();

            var mockHttp = new MockHttpMessageHandler();
            Guid jobId = Guid.NewGuid();
            var submitJobUri = new Uri(new Uri(ClientSettings.Singleton.OmegaResetToolEndpoint), "/submitJob");
            string submitJobContent = $"{{\"userId\":\"{DefaultXuid}\",\"Scid\":\"{DefaultScid}\"}}";

            mockHttp.Expect(submitJobUri.ToString())
                .WithContent(submitJobContent)
                .WithHeaders("Authorization", this.ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond(HttpStatusCode.BadRequest);

            TestHook.MockHttpHandler = mockHttp;

            var resetResult = await PlayerReset.ResetPlayerDataAsync(DefaultScid, DefaultSandbox, DefaultXuid);
            Assert.AreEqual(ResetOverallResult.CompletedError, resetResult.OverallResult);
            Assert.AreEqual(0, resetResult.ProviderStatus.Count);

            mockHttp.VerifyNoOutstandingExpectation();
        }
    }
}
