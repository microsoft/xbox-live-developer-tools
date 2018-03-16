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
    using System.Reflection;
    using System.Security;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Xbox.Services.DevTools.Authentication;
    using Microsoft.Xbox.Services.DevTools.Common;
    using Microsoft.Xbox.Services.DevTools.XblConfig;
    using Microsoft.Xbox.Services.DevTools.XblConfig.Contracts;
    using Moq;
    using RichardSzalay.MockHttp;

    [TestClass]
    public class XblConfigTest
    {
        private const string DefaultUserName = "username";
        private const string DefaultScid = "00000000-0000-0000-0000-012345678901";
        private const string DefaultAccountId = "00000000-0000-0000-0000-012345678901";
        private const string DefaultSandbox = "sandbox";
        private const string DefaultEtoken = "etoken";
        private const string DefaultCorrelationVector = "uPtLmFLxvka56G9YkOF1cw.0";
        private MockHttpMessageHandler mockHandler;

        private void SetUpMockAuth()
        {
            var mockAuth = new Mock<AuthClient>();
            mockAuth.Setup(o => o.GetETokenAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()))
                .ReturnsAsync((string scid, IEnumerable<string> sandboxes, bool refresh) => DefaultEtoken + scid + string.Join(" ", sandboxes ?? new string[0]));
            ToolAuthentication.Client = mockAuth.Object;
            ToolAuthentication.SetAuthInfo(DevAccountSource.WindowsDevCenter, DefaultUserName);
        }

        private string ExpectedToken(string scid, string sandbox)
        {
            return "XBL3.0 x=-;" + DefaultEtoken + scid + sandbox;
        }

        private HttpResponseMessage ExpectedJsonResponse(object jsonObject)
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.Add("MS-CV", DefaultCorrelationVector);
            response.Content = new StringContent(jsonObject.ToJson());
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            return response;
        }

        private HttpResponseMessage ExpectedBinaryResponse(byte[] content)
        {
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.Add("MS-CV", DefaultCorrelationVector);
            response.Content = new ByteArrayContent(content);
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            return response;
        }

        [TestInitialize]
        public void Init()
        {
            this.mockHandler = new MockHttpMessageHandler();
            TestHook.MockHttpHandler = this.mockHandler;
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CertificateHelperEmptyPrivateKey()
        {
            using (CertificateHelper helper = new CertificateHelper())
            {
                helper.ExportPfx(null, null);
            }
        }

        [TestMethod]
        public void CertificateHelperGenerateCertRequest()
        {
            using (CertificateHelper helper = new CertificateHelper())
            {
                try
                {
                    string request = helper.GenerateCertRequest();
                    Assert.IsNotNull(request);
                }
                catch (Exception ex)
                {
                    if (!helper.CanGenerateCertRequest)
                    {
                        Assert.IsInstanceOfType(ex, typeof(SecurityException));
                    }
                }
            }
        }

        [TestMethod]
        public async Task GetSchemaTypesSuccess()
        {
            SchemaTypes expectedSchemaTypes = new SchemaTypes
            {
                AvailableSchemaTypes = new string[]
                {
                    "achievements", "userstats"
                }
            };

            var uri = new Uri(new Uri(ClientSettings.Singleton.XConEndpoint), "/schema");

            this.mockHandler.Expect(uri.ToString())
                .Respond(res => this.ExpectedJsonResponse(expectedSchemaTypes));

            var schemaTypes = await ConfigurationManager.GetSchemaTypesAsync();

            Assert.AreEqual(DefaultCorrelationVector, schemaTypes.CorrelationId);
            Assert.AreEqual(2, schemaTypes.Result.Count());
        }

        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task GetSchemaTypesFailure()
        {
            var uri = new Uri(new Uri(ClientSettings.Singleton.XConEndpoint), "/schema");

            this.mockHandler.Expect(uri.ToString())
                .Respond(HttpStatusCode.ServiceUnavailable);

            var schemaTypes = await ConfigurationManager.GetSchemaTypesAsync();
        }

        [TestMethod]
        public async Task GetSchemaVersionsSuccess()
        {
            string schemaNamespace = "http://config.mgt.xboxlive.com/schema/achievements/1";
            SchemaVersions expectedSchemaVersions = new SchemaVersions
            {
                AvailableSchema = new string[] 
                {
                    schemaNamespace
                }
            };

            var uri = new Uri(new Uri(ClientSettings.Singleton.XConEndpoint), "/schema/achievements");
            this.mockHandler.Expect(uri.ToString())
                .Respond(res => this.ExpectedJsonResponse(expectedSchemaVersions));

            var schemaVersions = await ConfigurationManager.GetSchemaVersionsAsync("achievements");

            Assert.AreEqual(DefaultCorrelationVector, schemaVersions.CorrelationId);
            Assert.AreEqual(schemaNamespace, schemaVersions.Result.FirstOrDefault().Namespace);
            Assert.AreEqual(1, schemaVersions.Result.FirstOrDefault().Version);
        }

        [TestMethod]
        public async Task GetSchemasSuccess()
        {
            var uri = new Uri(new Uri(ClientSettings.Singleton.XConEndpoint), $"/schema/achievements/1");
            var responseString = "foo";

            this.mockHandler.Expect(uri.ToString())
                .Respond(res => this.ExpectedBinaryResponse(Encoding.UTF8.GetBytes(responseString)));

            var response = await ConfigurationManager.GetSchemaAsync("achievements", 1);
            Assert.AreEqual(DefaultCorrelationVector, response.CorrelationId);
            Assert.IsTrue(response.Result.CanRead);
            Assert.AreEqual(0, response.Result.Position);
            using (StreamReader sr = new StreamReader(response.Result))
            {
                Assert.AreEqual(responseString, sr.ReadToEnd());
            }
        }

        [TestMethod]
        public async Task GetSandboxesSuccess()
        {
            this.SetUpMockAuth();
            var uri = new Uri(new Uri(ClientSettings.Singleton.XOrcEndpoint), $"/sandboxes?accountId={DefaultAccountId}");

            var responseArray = new Sandbox[]
            {
                new Sandbox() { AccountId = Guid.Parse(DefaultAccountId), SandboxId = "XYZ.2" },
                new Sandbox() { AccountId = Guid.Parse(DefaultAccountId), SandboxId = "XYZ.10" },
                new Sandbox() { AccountId = Guid.Parse(DefaultAccountId), SandboxId = "XYZ.1" },
                new Sandbox() { AccountId = Guid.Parse(DefaultAccountId), SandboxId = "RETAIL" }
            };

            this.mockHandler.Expect(uri.ToString())
                .WithHeaders("Authorization", this.ExpectedToken(null, null))
                .Respond(res => this.ExpectedJsonResponse(responseArray));

            var response = await ConfigurationManager.GetSandboxesAsync(Guid.Parse(DefaultAccountId));

            // Ensure that the sandboxes are ordered correctly.
            Assert.AreEqual("RETAIL", response.Result.ElementAt(0));
            Assert.AreEqual("XYZ.1", response.Result.ElementAt(1));
            Assert.AreEqual("XYZ.2", response.Result.ElementAt(2));
            Assert.AreEqual("XYZ.10", response.Result.ElementAt(3));
        }

        [TestMethod]
        public async Task GetProductsSuccess()
        {
            this.SetUpMockAuth();
            var uri = new Uri(new Uri(ClientSettings.Singleton.XOrcEndpoint), $"/products?accountId={DefaultAccountId}&count=500");

            var responseArray = new Product[]
            {
                this.CreateProduct()
            };

            this.mockHandler.Expect(uri.ToString())
                .WithHeaders("Authorization", this.ExpectedToken(null, null))
                .Respond(res => this.ExpectedJsonResponse(responseArray));

            var response = await ConfigurationManager.GetProductsAsync(new Guid(DefaultAccountId));
            Assert.AreEqual(DefaultCorrelationVector, response.CorrelationId);
            Assert.AreEqual(1, response.Result.Count());
        }

        [TestMethod]
        public async Task GetProductSuccess()
        {
            this.SetUpMockAuth();
            var uri = new Uri(new Uri(ClientSettings.Singleton.XOrcEndpoint), $"/products/{DefaultScid}");

            var responseProduct = this.CreateProduct();

            this.mockHandler.Expect(uri.ToString())
                .WithHeaders("Authorization", this.ExpectedToken(DefaultScid, null))
                .Respond(res => this.ExpectedJsonResponse(responseProduct));

            var response = await ConfigurationManager.GetProductAsync(new Guid(DefaultScid));
            Assert.AreEqual(DefaultCorrelationVector, response.CorrelationId);
            Assert.AreEqual(new Guid(DefaultScid), response.Result.PrimaryServiceConfigId);
        }

        [TestMethod]
        public async Task UploadAchievementImageSuccess()
        {
            var achievementImage = Assembly.GetExecutingAssembly().GetManifestResourceStream("Microsoft.Xbox.Services.Tool.Unittest.AchievementImage.png");
            await this.UploadAchievementImage("filename", achievementImage);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UploadAchievementImageWrongSize()
        {
            var achievementImage = Assembly.GetExecutingAssembly().GetManifestResourceStream("Microsoft.Xbox.Services.Tool.Unittest.AchievementImageSmall.png");
            await this.UploadAchievementImage("filename", achievementImage);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UploadAchievementImageInvalidType()
        {
            var achievementImage = new MemoryStream();
            await this.UploadAchievementImage("filename", achievementImage);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task UploadAchievementImageInvalidFilename()
        {
            var achievementImage = new MemoryStream();
            await this.UploadAchievementImage(null, achievementImage);
        }

        private async Task UploadAchievementImage(string filename, Stream image)
        {
            this.SetUpMockAuth();
            
            int fileSize = (int)image.Length;
            Guid xfusId = Guid.NewGuid();
            string xfusToken = "1234";
            var initUri = new Uri(new Uri(ClientSettings.Singleton.XAchEndpoint), "/assets/initialize");

            var initResponse = new XachInitializeResponse()
            {
                XfusId = xfusId,
                XfusToken = xfusToken,
                XfusUploadWindowLocation = "5678"
            };

            var setMetadataResponse = new SetMetadataResponse()
            {
                BlobPartitions = 1,
                ChunkList = new int[] { 1 },
                ChunkSize = fileSize,
                Error = false,
                Id = xfusId,
                Message = string.Empty,
                ResumeRestart = false,
                ServerLocation = string.Empty
            };

            var uploadResponse = new UploadImageResponse()
            {
                AbsoluteUri = string.Empty,
                ChunkNum = 1,
                Error = false,
                ErrorCode = 0,
                Location = string.Empty,
                Message = string.Empty,
                MissingChunks = null,
                RawLocation = string.Empty,
                State = string.Empty
            };

            var finalizeResponse = new AchievementImage()
            {
                AssetId = Guid.NewGuid(),
                CdnUrl = new Uri("https://xboxlive.com/"),
                HeightInPixels = 1080,
                ImageType = "png",
                IsPublic = true,
                Scid = new Guid(DefaultScid),
                ThumbnailCdnUrl = new Uri("https://xboxlive.com/"),
                WidthInPixels = 1920
            };

            string expectedToken = this.ExpectedToken(DefaultScid, null);

            this.mockHandler.Expect(initUri.ToString())
                .WithHeaders("Authorization", expectedToken)
                .Respond(res => this.ExpectedJsonResponse(initResponse));

            var setMetadataUri = new Uri(new Uri(ClientSettings.Singleton.XFusEndpoint), $"/upload/SetMetadata?filename={filename}&fileSize={fileSize}&id={xfusId}&token={xfusToken}");

            this.mockHandler.Expect(setMetadataUri.ToString())
                .WithHeaders("Authorization", expectedToken)
                .Respond(res => this.ExpectedJsonResponse(setMetadataResponse));

            var uploadChunkUri = new Uri(new Uri(ClientSettings.Singleton.XFusEndpoint), $"/upload/uploadchunk/{xfusId}?blockNumber=1&token={xfusToken}");

            this.mockHandler.Expect(uploadChunkUri.ToString())
                .WithHeaders("Authorization", expectedToken)
                .Respond(res => this.ExpectedJsonResponse(uploadResponse));

            var finishUploadUri = new Uri(new Uri(ClientSettings.Singleton.XFusEndpoint), $"/upload/finished/{xfusId}?token={xfusToken}");

            this.mockHandler.Expect(finishUploadUri.ToString())
                .WithHeaders("Authorization", expectedToken)
                .Respond(res => this.ExpectedJsonResponse(uploadResponse));

            var finalizeUri = new Uri(new Uri(ClientSettings.Singleton.XAchEndpoint), $"/scids/{DefaultScid}/images");

            this.mockHandler.Expect(finalizeUri.ToString())
                .WithHeaders("Authorization", expectedToken)
                .Respond(res => this.ExpectedJsonResponse(finalizeResponse));

            var response = await ConfigurationManager.UploadAchievementImageAsync(new Guid(DefaultScid), filename, image);

            Assert.AreEqual(finalizeResponse.AssetId, response.Result.AssetId);
        }

        private Product CreateProduct()
        {
            return new Product()
            {
                AccountId = new Guid(DefaultAccountId),
                AlternateIds = new AlternateId[]
                    {
                        new AlternateId()
                        {
                            AlternateIdType = AlternateIdType.AppId,
                            Value = string.Empty
                        }
                    },
                IsTest = false,
                MsaAppId = "Foo",
                PfnId = "Bar",
                PrimaryServiceConfigId = new Guid(DefaultScid),
                ProductId = Guid.NewGuid(),
                TitleId = 12345,
                XboxLiveTier = XboxLiveTier.Full
            };
        }
    }
}
