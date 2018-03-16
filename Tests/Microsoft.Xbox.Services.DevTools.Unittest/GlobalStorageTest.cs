// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Unittest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using DevTools.Authentication;
    using DevTools.Common;
    using DevTools.TitleStorage;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using RichardSzalay.MockHttp;

    [TestClass]
    public class GlobalStorageTest
    {
        private const string DefaultUserName = "username";
        private const string DefaultScid = "00000000-0000-0000-0000-012345678901";
        private const string DefaultSandbox = "sandbox";
        private const string DefaultEtoken = "etoken";
        private const ulong DefaultQuota = ulong.MaxValue;
        private const ulong DefaultUsedQuota = ulong.MaxValue - 2;
        private const string DefaultGlobalStoragePath = "path";
        private const uint DefaultMaxItems = 150;
        private const uint DefaultSkipItems = 2;
        private const string DefaultFileType = "config";
        private const uint ByteSize = 10;
        private byte[] defaultBytes;

        private void SetupMockBytes()
        {
            if (this.defaultBytes == null)
            {
                this.defaultBytes = new byte[ByteSize];
                for (int i = 0; i < ByteSize; i++)
                {
                    this.defaultBytes[i] = (byte)i;
                }
            }
        }

        private void SetUpMockAuth()
        {
            var mockAuth = new Mock<AuthClient>();
            mockAuth.Setup(o => o.GetETokenAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()))
                .ReturnsAsync((string scid, IEnumerable<string> sandboxes, bool refresh) => 
                DefaultEtoken + scid + string.Join(" ", sandboxes));
            ToolAuthentication.Client = mockAuth.Object;
            ToolAuthentication.SetAuthInfo(DevAccountSource.WindowsDevCenter, DefaultUserName);
        }

        private string ExpectedToken(string scid, string sandbox)
        {
            return "XBL3.0 x=-;" + DefaultEtoken + scid + sandbox;
        }

        [TestMethod]
        public async Task GetQuota()
        {
           this.SetUpMockAuth();

            string quotaResponse = $"{{'quotaInfo':{{'usedBytes':{DefaultUsedQuota},'quotaBytes':{DefaultQuota}}}}}";

            var uri = new Uri(new Uri(ClientSettings.Singleton.TitleStorageEndpoint), "/global/scids/" + DefaultScid);

            var mockHttp = new MockHttpMessageHandler();

            mockHttp.Expect(uri.ToString())
                .WithHeaders("Authorization", this.ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond("application/json", quotaResponse);

            TestHook.MockHttpHandler = mockHttp;

            var quotaInfo = await TitleStorage.GetGlobalStorageQuotaAsync(DefaultScid, DefaultSandbox);

            Assert.AreEqual(DefaultQuota, quotaInfo.QuotaBytes);
            Assert.AreEqual(DefaultUsedQuota, quotaInfo.UsedBytes);
        }

        [TestMethod]
        public async Task GetBlobMetadata()
        {
            this.SetUpMockAuth();

            string response =
                "{'blobs':[" +
                "{'fileName':'test1.txt,config','etag':'','size':2}," +
                "{'fileName':'test11.txt,binary','etag':'','size':2}," +
                "{'fileName':'test2.txt,config','etag':'','size':2}," +
                "{'fileName':'test3.txt,config','etag':'','size':2}," +
                "{'fileName':'test4.txt,config','etag':'','size':2}," +
                "{'fileName':'test5.txt,config','etag':'','size':2}" +
                "]," +
                "'pagingInfo':{'totalItems':8,'continuationToken':'123456'}}";

            var uri = new Uri(new Uri(ClientSettings.Singleton.TitleStorageEndpoint), $"global/scids/{DefaultScid}/data/{DefaultGlobalStoragePath}?maxItems={DefaultMaxItems}&skipItems={DefaultSkipItems}");

            var mockHttp = new MockHttpMessageHandler();

            mockHttp.Expect(uri.ToString())
                .WithHeaders("Authorization", this.ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond("application/json", response);

            TestHook.MockHttpHandler = mockHttp;

            var metadataResult = await TitleStorage.GetGlobalStorageBlobMetaDataAsync(DefaultScid, DefaultSandbox, DefaultGlobalStoragePath, DefaultMaxItems, DefaultSkipItems );
            Assert.AreEqual((uint)8, metadataResult.TotalItems);
            Assert.AreEqual(6, metadataResult.Items.Count());
            Assert.IsTrue(metadataResult.HasNext);

            // GetNextAsync
            var nextPageUri = new Uri(new Uri(ClientSettings.Singleton.TitleStorageEndpoint), $"global/scids/{DefaultScid}/data/{DefaultGlobalStoragePath}?maxItems={DefaultMaxItems}&continuationToken=123456");
            mockHttp.Expect(nextPageUri.ToString())
                .WithHeaders("Authorization", this.ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond("application/json", response);

            var metaResult1 = await metadataResult.GetNextAsync(DefaultMaxItems);
            Assert.AreEqual((uint)8, metadataResult.TotalItems);
            Assert.AreEqual(6, metadataResult.Items.Count());
            Assert.IsTrue(metadataResult.HasNext);
        }

        [TestMethod]
        public async Task GetBlobMetadataNotFound()
        {
            this.SetUpMockAuth();

            var uri = new Uri(new Uri(ClientSettings.Singleton.TitleStorageEndpoint), $"global/scids/{DefaultScid}/data/{DefaultGlobalStoragePath}");

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(uri.ToString())
                .WithHeaders("Authorization", this.ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond(HttpStatusCode.NotFound);

            TestHook.MockHttpHandler = mockHttp;

            var metadataResult = await TitleStorage.GetGlobalStorageBlobMetaDataAsync(DefaultScid, DefaultSandbox, DefaultGlobalStoragePath, DefaultMaxItems, DefaultSkipItems);
            Assert.AreEqual((uint)0, metadataResult.TotalItems);
            Assert.AreEqual(0, metadataResult.Items.Count());
            Assert.IsFalse(metadataResult.HasNext);
        }

        [TestMethod]
        public async Task UploadBlob()
        {
            this.SetUpMockAuth();
            this.SetupMockBytes();

            var uri = new Uri(new Uri(ClientSettings.Singleton.TitleStorageEndpoint), $"global/scids/{DefaultScid}/data/{DefaultGlobalStoragePath},{DefaultFileType}");

            var mockHttp = new MockHttpMessageHandler();

            mockHttp.Expect(HttpMethod.Put, uri.ToString())
                .WithHeaders("Authorization", this.ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond("application/json", "{}");

            TestHook.MockHttpHandler = mockHttp;

            TitleStorageBlobType fileType = (TitleStorageBlobType)Enum.Parse(typeof(TitleStorageBlobType), DefaultFileType, true);
            await TitleStorage.UploadGlobalStorageBlobAsync(DefaultScid, DefaultSandbox, DefaultGlobalStoragePath, fileType, this.defaultBytes);
        }

        [TestMethod]
        public async Task DownloadBlob()
        {
            this.SetUpMockAuth();
            this.SetupMockBytes();

            var uri = new Uri(new Uri(ClientSettings.Singleton.TitleStorageEndpoint), $"global/scids/{DefaultScid}/data/{DefaultGlobalStoragePath},{DefaultFileType}");

            var mockHttp = new MockHttpMessageHandler();

            Stream stream = new MemoryStream(this.defaultBytes);
            mockHttp.Expect(HttpMethod.Get, uri.ToString())
                .WithHeaders("Authorization", this.ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond("application/json", stream);

            TestHook.MockHttpHandler = mockHttp;

            TitleStorageBlobType fileType = (TitleStorageBlobType)Enum.Parse(typeof(TitleStorageBlobType), DefaultFileType, true);
            byte[] bytes = await TitleStorage.DownloadGlobalStorageBlobAsync(DefaultScid, DefaultSandbox, DefaultGlobalStoragePath, fileType);

            Assert.AreEqual(bytes.Length, this.defaultBytes.Length);
        }

        [TestMethod]
        public async Task DeleteBlob()
        {
            this.SetUpMockAuth();
            this.SetupMockBytes();

            var uri = new Uri(new Uri(ClientSettings.Singleton.TitleStorageEndpoint), $"global/scids/{DefaultScid}/data/{DefaultGlobalStoragePath},{DefaultFileType}");

            var mockHttp = new MockHttpMessageHandler();

            mockHttp.Expect(HttpMethod.Delete, uri.ToString())
                .WithHeaders("Authorization", this.ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond("application/json", "{}");

            TestHook.MockHttpHandler = mockHttp;

            TitleStorageBlobType fileType = (TitleStorageBlobType)Enum.Parse(typeof(TitleStorageBlobType), DefaultFileType, true);
            await TitleStorage.DeleteGlobalStorageBlobAsync(DefaultScid, DefaultSandbox, DefaultGlobalStoragePath, fileType);
        }
    }
}
