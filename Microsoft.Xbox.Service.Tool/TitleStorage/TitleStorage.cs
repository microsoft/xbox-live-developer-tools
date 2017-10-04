// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Linq;

namespace Microsoft.Xbox.Services.Tool
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web;

    public partial class TitleStorage
    {
        private static Uri baseUri = new Uri(ClientSettings.Singleton.TitleStorageEndpoint);

        static public async Task<GlobalStorageQuotaInfo> GetGlobalStorageQuotaAsync(string serviceConfigurationId, string sandbox)
        {
            using (var request = new XboxLiveHttpRequest())
            {
                var requestMsg = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, "/global/scids/" + serviceConfigurationId));

                string eToken = await Auth.GetETokenSilentlyAsync(serviceConfigurationId, sandbox);
                AddRequestHeaders(ref requestMsg, eToken);

                var response = await request.SendAsync(requestMsg);
                string content = await response.Content.ReadAsStringAsync();
                JObject storageQuota = JObject.Parse(content);
                GlobalStorageQuotaInfo info = storageQuota["quotaInfo"].ToObject<GlobalStorageQuotaInfo>();

                Log.WriteLog($"GetGlobalStorageQuotaAsync for scid: {serviceConfigurationId}, sandbox: {sandbox}");

                return info;
            }

        }

        static public async Task<TitleStorageBlobMetadataResult> GetGlobalStorageBlobMetaData(string serviceConfigurationId, string sandbox, string path, uint maxItems, uint skipItems)
        {
            return await GetGlobalStorageBlobMetaData(serviceConfigurationId, sandbox, path, maxItems, skipItems, null);
        }

        internal static async Task<TitleStorageBlobMetadataResult> GetGlobalStorageBlobMetaData(string serviceConfigurationId, string sandbox, string path, uint maxItems,
            uint skipItems, string continuationToken)
        {
            using (var request = new XboxLiveHttpRequest())
            {
                var uriBuilder = new UriBuilder(baseUri);
                uriBuilder.Path = $"/global/scids/{serviceConfigurationId}/data/{path}";

                AppendPagingInfo(ref uriBuilder, maxItems, skipItems, continuationToken);

                var requestMsg = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);

                string eToken = await Auth.GetETokenSilentlyAsync(serviceConfigurationId, sandbox);
                AddRequestHeaders(ref requestMsg, eToken);

                XboxLiveHttpContent response;
                try
                {
                    response = await request.SendAsync(requestMsg);
                }
                catch (XboxLiveException ex)
                {
                    // Return empty list on 404
                    if (ex.ErrorStatus == XboxLiveErrorStatus.NotFound)
                    {
                        return new TitleStorageBlobMetadataResult();
                    }
                    else
                    {
                        throw ex;
                    }
                }
                

                Log.WriteLog($"GetGlobalStorageBlobMetaData for scid: {serviceConfigurationId}, sandbox: {sandbox}");

                string stringContent = await response.Content.ReadAsStringAsync();
                JObject storageMetadata = JObject.Parse(stringContent);
                var result = new TitleStorageBlobMetadataResult
                {
                    TotalItems = storageMetadata["pagingInfo"]["totalItems"].Value<uint>(),
                    ContinuationToken = storageMetadata["pagingInfo"]["continuationToken"].Value<string>(),
                    ServiceConfigurationId = serviceConfigurationId,
                    Sandbox = sandbox,
                    Path = path
                };

                var array = (JArray)storageMetadata["blobs"];
                var metadataArray = array.Select((o) =>
                {
                    string fileName = o["fileName"].Value<string>();
                    UInt64 size = o["size"].Value<UInt64>();
                    var filePathAndTypeArray = fileName.Split(',');
                    if (filePathAndTypeArray.Length != 2)
                    {
                        throw new XboxLiveException("Invalid file name format in TitleStorageBlobMetadata response");
                    }
                    TitleStorageBlobType type;
                    if (!Enum.TryParse(filePathAndTypeArray[1], true, out type))
                    {
                        throw new XboxLiveException("Invalid file type in TitleStorageBlobMetadata response");
                    }

                    return new TitleStorageBlobMetadata
                    {
                        Path = filePathAndTypeArray[0],
                        Size = size,
                        Type = type

                    };
                }).ToArray();

                result.Items = metadataArray;

                return result;
            }
        }

        static public async Task UploadGlobalStorageBlob(string serviceConfigurationId, string sandbox, string pathAndFileName, TitleStorageBlobType blobType, byte[] bloBytes)
        {
            using (var request = new XboxLiveHttpRequest())
            {
                var requestMsg = new HttpRequestMessage(HttpMethod.Put, new Uri(baseUri, $"/global/scids/{serviceConfigurationId}/data/{pathAndFileName},{blobType.ToString().ToLower()}"));

                string eToken = await Auth.GetETokenSilentlyAsync(serviceConfigurationId, sandbox);
                AddRequestHeaders(ref requestMsg, eToken);

                requestMsg.Content = new ByteArrayContent(bloBytes);

                var response = await request.SendAsync(requestMsg);

                Log.WriteLog($"UploadGlobalStorageBlob for scid: {serviceConfigurationId}, sandbox: {sandbox}");
            }
        }

        static public async Task<byte[]> DownloadGlobalStorageBlob(string serviceConfigurationId, string sandbox, string pathAndFileName, TitleStorageBlobType blobType)
        {
            using (var request = new XboxLiveHttpRequest())
            {
                var requestMsg = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, $"/global/scids/{serviceConfigurationId}/data/{pathAndFileName},{blobType.ToString().ToLower()}"));

                string eToken = await Auth.GetETokenSilentlyAsync(serviceConfigurationId, sandbox);
                AddRequestHeaders(ref requestMsg, eToken);

                var response = await request.SendAsync(requestMsg);

                Log.WriteLog($"DownloadGlobalStorageBlob for scid: {serviceConfigurationId}, sandbox: {sandbox}");

                return await response.Content.ReadAsByteArrayAsync();
            }

        }

        static public async Task DeleteGlobalStorageBlob(string serviceConfigurationId, string sandbox, string pathAndFileName, TitleStorageBlobType blobType)
        {
            using (var request = new XboxLiveHttpRequest())
            {

                var requestMsg = new HttpRequestMessage(HttpMethod.Delete, new Uri(baseUri, $"/global/scids/{serviceConfigurationId}/data/{pathAndFileName},{blobType.ToString().ToLower()}"));

                string eToken = await Auth.GetETokenSilentlyAsync(serviceConfigurationId, sandbox);
                AddRequestHeaders(ref requestMsg, eToken);

                var response = await request.SendAsync(requestMsg);

                Log.WriteLog($"DeleteGlobalStorageBlob for scid: {serviceConfigurationId}, sandbox: {sandbox}");
            }

        }

        static private void AppendPagingInfo(ref UriBuilder uriBuilder, uint maxItems, uint skipItems,
            string continuationToken)
        {
            var queries = HttpUtility.ParseQueryString(uriBuilder.Query);

            if (maxItems > 0)
            {
                queries["maxItems"] = maxItems.ToString();
            }

            if (string.IsNullOrEmpty(continuationToken))
            {
                if (skipItems > 0)
                {
                    queries["skipItems"] = skipItems.ToString();
                }
            }
            else
            {
                   queries["continuationToken"] = continuationToken;
            }

            uriBuilder.Query = queries.ToString();
        }

        private static void AddRequestHeaders(ref HttpRequestMessage request, string eToken)
        {
            request.Headers.Add("x-xbl-contract-version", "1");
            request.Headers.Add("Authorization", eToken);
        }
    }

}
