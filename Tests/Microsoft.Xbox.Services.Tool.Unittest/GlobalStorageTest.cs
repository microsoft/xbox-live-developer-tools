using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Xbox.Services.Tool;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RichardSzalay.MockHttp;
using System.IO;
using System.Net;

namespace Microsoft.Xbox.Services.Tool.Unittest
{
    [TestClass]
    public class GlobalStorageTest
    {
        private const string DefaultUserName = "username";
        private const string DefaultScid = "scid";
        private const string DefaultSandbox = "sandbox";
        private const string DefaultEtoken = "etoken";
        private const UInt64 DefaultQuota = UInt64.MaxValue;
        private const UInt64 DefaultUsedQuota = UInt64.MaxValue - 2 ;
        private const string DefaultGlobalStoragePath = "path";
        private const uint DefaultMaxItems = 150;
        private const uint DefaultSkipItems = 2;
        private const string DefaultFileType = "config";
        private const uint ByteSize = 10;
        private byte[] defaultBytes;
        

        private void SetupMockBytes()
        {
            if (defaultBytes == null)
            {
                defaultBytes = new byte[ByteSize];
                for (int i = 0; i < ByteSize; i++)
                {
                    defaultBytes[i] = (byte)i;
                }
            }
        }

        private void SetUpMockAuth()
        {
            var mockAuth = new Mock<AuthClient>();
            mockAuth.Setup(o => o.GetETokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync((string scid, string sandbox, bool refresh) => DefaultEtoken + scid + sandbox);
            Auth.Client = mockAuth.Object;
            Auth.SetAuthInfo(DefaultUserName, DevAccountSource.WindowsDevCenter);
        }

        private string ExpectedToken(string scid, string sandbox)
        {
            return "XBL3.0 x=-;" + DefaultEtoken + scid + sandbox;
        }

        [TestMethod]
        public async Task GetQuota()
        {
            SetUpMockAuth();

            string quotaResponse = $"{{'quotaInfo':{{'usedBytes':{DefaultUsedQuota},'quotaBytes':{DefaultQuota}}}}}";

            var uri = new Uri(new Uri(ClientSettings.Singleton.TitleStorageEndpoint), "/global/scids/" + DefaultScid);

            var mockHttp = new MockHttpMessageHandler();

            mockHttp.Expect(uri.ToString())
                .WithHeaders("Authorization", ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond("application/json", quotaResponse);

            TestHook.MockHttpHandler = mockHttp;

            var quotaInfo = await TitleStorage.GetGlobalStorageQuotaAsync(DefaultScid, DefaultSandbox);

            Assert.AreEqual(DefaultQuota, quotaInfo.QuotaBytes);
            Assert.AreEqual(DefaultUsedQuota, quotaInfo.UsedBytes);
        }

        [TestMethod]
        public async Task GetBlobMetadata()
        {
            SetUpMockAuth();

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
                .WithHeaders("Authorization", ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond("application/json", response);

            TestHook.MockHttpHandler = mockHttp;

            var metadataResult = await TitleStorage.GetGlobalStorageBlobMetaData(DefaultScid, DefaultSandbox, DefaultGlobalStoragePath, DefaultMaxItems, DefaultSkipItems );
            Assert.AreEqual((uint)8, metadataResult.TotalItems);
            Assert.AreEqual(6, metadataResult.Items.Count());
            Assert.IsTrue(metadataResult.HasNext);


            // GetNextAsync
            var nextPageUri = new Uri(new Uri(ClientSettings.Singleton.TitleStorageEndpoint), $"global/scids/{DefaultScid}/data/{DefaultGlobalStoragePath}?maxItems={DefaultMaxItems}&continuationToken=123456");
            mockHttp.Expect(nextPageUri.ToString())
                .WithHeaders("Authorization", ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond("application/json", response);

            var metaResult1 = await metadataResult.GetNextAsync(DefaultMaxItems);
            Assert.AreEqual((uint)8, metadataResult.TotalItems);
            Assert.AreEqual(6, metadataResult.Items.Count());
            Assert.IsTrue(metadataResult.HasNext);
        }

        [TestMethod]
        public async Task GetBlobMetadataNotFound()
        {
            SetUpMockAuth();

            var uri = new Uri(new Uri(ClientSettings.Singleton.TitleStorageEndpoint), $"global/scids/{DefaultScid}/data/{DefaultGlobalStoragePath}");

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(uri.ToString())
                .WithHeaders("Authorization", ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond(HttpStatusCode.NotFound);

            TestHook.MockHttpHandler = mockHttp;

            var metadataResult = await TitleStorage.GetGlobalStorageBlobMetaData(DefaultScid, DefaultSandbox, DefaultGlobalStoragePath, DefaultMaxItems, DefaultSkipItems);
            Assert.AreEqual((uint)0, metadataResult.TotalItems);
            Assert.AreEqual(0, metadataResult.Items.Count());
            Assert.IsFalse(metadataResult.HasNext);
        }

        [TestMethod]
        public async Task UploadBlob()
        {
            SetUpMockAuth();
            SetupMockBytes();

            var uri = new Uri(new Uri(ClientSettings.Singleton.TitleStorageEndpoint), $"global/scids/{DefaultScid}/data/{DefaultGlobalStoragePath},{DefaultFileType}");

            var mockHttp = new MockHttpMessageHandler();

            mockHttp.Expect(HttpMethod.Put, uri.ToString())
                .WithHeaders("Authorization", ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond("application/json", "{}");

            TestHook.MockHttpHandler = mockHttp;

            TitleStorageBlobType fileType = (TitleStorageBlobType)Enum.Parse(typeof(TitleStorageBlobType), DefaultFileType, true);
            await TitleStorage.UploadGlobalStorageBlob(DefaultScid, DefaultSandbox, DefaultGlobalStoragePath, fileType, this.defaultBytes);
        }


        [TestMethod]
        public async Task DownloadBlob()
        {
            SetUpMockAuth();
            SetupMockBytes();

            var uri = new Uri(new Uri(ClientSettings.Singleton.TitleStorageEndpoint), $"global/scids/{DefaultScid}/data/{DefaultGlobalStoragePath},{DefaultFileType}");

            var mockHttp = new MockHttpMessageHandler();

            Stream stream = new MemoryStream(this.defaultBytes);
            mockHttp.Expect(HttpMethod.Get, uri.ToString())
                .WithHeaders("Authorization", ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond("application/json", stream);

            TestHook.MockHttpHandler = mockHttp;

            TitleStorageBlobType fileType = (TitleStorageBlobType)Enum.Parse(typeof(TitleStorageBlobType), DefaultFileType, true);
            byte[] bytes = await TitleStorage.DownloadGlobalStorageBlob(DefaultScid, DefaultSandbox, DefaultGlobalStoragePath, fileType);

            Assert.AreEqual(bytes.Length, this.defaultBytes.Length);

        }

        [TestMethod]
        public async Task DeleteBlob()
        {
            SetUpMockAuth();
            SetupMockBytes();

            var uri = new Uri(new Uri(ClientSettings.Singleton.TitleStorageEndpoint), $"global/scids/{DefaultScid}/data/{DefaultGlobalStoragePath},{DefaultFileType}");

            var mockHttp = new MockHttpMessageHandler();

            mockHttp.Expect(HttpMethod.Delete, uri.ToString())
                .WithHeaders("Authorization", ExpectedToken(DefaultScid, DefaultSandbox))
                .Respond("application/json", "{}");

            TestHook.MockHttpHandler = mockHttp;

            TitleStorageBlobType fileType = (TitleStorageBlobType)Enum.Parse(typeof(TitleStorageBlobType), DefaultFileType, true);
            await TitleStorage.DeleteGlobalStorageBlob(DefaultScid, DefaultSandbox, DefaultGlobalStoragePath, fileType);
        }
    }
}
