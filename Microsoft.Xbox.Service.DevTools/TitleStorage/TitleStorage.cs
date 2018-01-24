// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Linq;
using System.Net;
using Microsoft.Xbox.Services.DevTools.Common;

namespace Microsoft.Xbox.Services.DevTools.TitleStorage
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web;
    using Authentication;
    using DevTools.Common;

    /// <summary>
    /// Class for TitleStorage tooling functionality.
    /// </summary>
    public class TitleStorage
    {
        private static Uri baseUri = new Uri(ClientSettings.Singleton.TitleStorageEndpoint);

        /// <summary>
        /// Get title global storage quota information.
        /// </summary>
        /// <param name="serviceConfigurationId">The service configuration ID (SCID) of the title</param>
        /// <param name="sandbox">The target sandbox id for title storage</param>
        /// <returns>GlobalStorageQuotaInfo object contains quota info</returns>
        static public async Task<GlobalStorageQuotaInfo> GetGlobalStorageQuotaAsync(string serviceConfigurationId, string sandbox)
        {
            using (var request = new XboxLiveHttpRequest(true, serviceConfigurationId, sandbox))
            {
                HttpResponseMessage response = (await request.SendAsync(() =>
                {
                    var requestMsg = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, "/global/scids/" + serviceConfigurationId));
                    requestMsg.Headers.Add("x-xbl-contract-version", "1");

                    return requestMsg;

                })).Response;
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();
                JObject storageQuota = JObject.Parse(content);
                GlobalStorageQuotaInfo info = storageQuota["quotaInfo"].ToObject<GlobalStorageQuotaInfo>();

                Log.WriteLog($"GetGlobalStorageQuotaAsync for scid: {serviceConfigurationId}, sandbox: {sandbox}");

                return info;
            }

        }

        /// <summary>
        /// Gets a list of blob metadata objects under a given path for the title global storage.
        /// </summary>
        /// <param name="serviceConfigurationId">The service configuration ID (SCID) of the title</param>
        /// <param name="sandbox">The target sandbox id for title storage</param>
        /// <param name="path">The root path to enumerate.  Results will be for blobs contained in this path and all subpaths.(Optional)</param>
        /// <param name="maxItems">The maximum number of items to return.</param>
        /// <param name="skipItems">The number of items to skip before returning results.</param>
        /// <returns>TitleStorageBlobMetadataResult object contains result collection.</returns>
        static public async Task<TitleStorageBlobMetadataResult> GetGlobalStorageBlobMetaDataAsync(string serviceConfigurationId, string sandbox, string path, uint maxItems, uint skipItems)
        {
            return await GetGlobalStorageBlobMetaDataAsync(serviceConfigurationId, sandbox, path, maxItems, skipItems, null);
        }

        /// <summary>
        /// Uploads blob data to title storage.
        /// </summary>
        /// <param name="serviceConfigurationId">The service configuration ID (SCID) of the title</param>
        /// <param name="sandbox">The target sandbox id for title storage</param>
        /// <param name="pathAndFileName">Blob path and file name to store on XboxLive service.(example: "foo\bar\blob.txt")</param>
        /// <param name="blobType">Title storage blob type</param>
        /// <param name="blobBytes">The byte array contains the blob data to upload.</param>
        static public async Task UploadGlobalStorageBlobAsync(string serviceConfigurationId, string sandbox, string pathAndFileName, TitleStorageBlobType blobType, byte[] blobBytes)
        {
            using (var request = new XboxLiveHttpRequest(true, serviceConfigurationId, sandbox))
            {
                var response = await request.SendAsync(()=>
                {
                    var requestMsg = new HttpRequestMessage(HttpMethod.Put, new Uri(baseUri, $"/global/scids/{serviceConfigurationId}/data/{pathAndFileName},{blobType.ToString().ToLower()}"));
                    requestMsg.Headers.Add("x-xbl-contract-version", "1");
                    requestMsg.Content = new ByteArrayContent(blobBytes);

                    return requestMsg;
                });

                Log.WriteLog($"UploadGlobalStorageBlobAsync for scid: {serviceConfigurationId}, sandbox: {sandbox}");
            }
        }

        /// <summary>
        /// Downloads blob data from title storage.
        /// </summary>
        /// <param name="serviceConfigurationId">The service configuration ID (SCID) of the title</param>
        /// <param name="sandbox">The target sandbox id for title storage</param>
        /// <param name="pathAndFileName">Blob path and file name to store on XboxLive service.(example: "foo\bar\blob.txt")</param>
        /// <param name="blobType">Title storage blob type</param>
        /// <returns>The byte array contains downloaded blob data</returns>
        static public async Task<byte[]> DownloadGlobalStorageBlobAsync(string serviceConfigurationId, string sandbox, string pathAndFileName, TitleStorageBlobType blobType)
        {
            using (var request = new XboxLiveHttpRequest(true, serviceConfigurationId, sandbox))
            {
                HttpResponseMessage response = (await request.SendAsync(()=> 
                {
                    var requestMsg = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, $"/global/scids/{serviceConfigurationId}/data/{pathAndFileName},{blobType.ToString().ToLower()}"));
                    requestMsg.Headers.Add("x-xbl-contract-version", "1");

                    return requestMsg;
                })).Response;
                response.EnsureSuccessStatusCode();

                Log.WriteLog($"DownloadGlobalStorageBlobAsync for scid: {serviceConfigurationId}, sandbox: {sandbox}");

                return await response.Content.ReadAsByteArrayAsync();
            }

        }

        /// <summary>
        /// Deletes a blob from title storage.
        /// </summary>
        /// <param name="serviceConfigurationId">The service configuration ID (SCID) of the title</param>
        /// <param name="sandbox">The target sandbox id for title storage</param>
        /// <param name="pathAndFileName">Blob path and file name to store on XboxLive service.(example: "foo\bar\blob.txt")</param>
        /// <param name="blobType">Title storage blob type</param>
        static public async Task DeleteGlobalStorageBlobAsync(string serviceConfigurationId, string sandbox, string pathAndFileName, TitleStorageBlobType blobType)
        {
            using (var request = new XboxLiveHttpRequest(true, serviceConfigurationId, sandbox))
            {
                var response = await request.SendAsync(()=>
                {
                    var requestMsg = new HttpRequestMessage(HttpMethod.Delete, new Uri(baseUri, $"/global/scids/{serviceConfigurationId}/data/{pathAndFileName},{blobType.ToString().ToLower()}"));
                    requestMsg.Headers.Add("x-xbl-contract-version", "1");

                    return requestMsg;
                });

                Log.WriteLog($"DeleteGlobalStorageBlobAsync for scid: {serviceConfigurationId}, sandbox: {sandbox}");
            }

        }

        private TitleStorage()
        {
        }

        internal static async Task<TitleStorageBlobMetadataResult> GetGlobalStorageBlobMetaDataAsync(string serviceConfigurationId, string sandbox, string path, uint maxItems,
            uint skipItems, string continuationToken)
        {
            using (var request = new XboxLiveHttpRequest(true, serviceConfigurationId, sandbox))
            {
                var uriBuilder = new UriBuilder(baseUri)
                { 
                    Path = $"/global/scids/{serviceConfigurationId}/data/{path}"
                };

                AppendPagingInfo(ref uriBuilder, maxItems, skipItems, continuationToken);

                HttpResponseMessage response = (await request.SendAsync(()=>
                {
                    var requestMsg = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);
                    requestMsg.Headers.Add("x-xbl-contract-version", "1");

                    return requestMsg;
                })).Response;

                // Return empty list on 404
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new TitleStorageBlobMetadataResult();
                }

                response.EnsureSuccessStatusCode();


                Log.WriteLog($"GetGlobalStorageBlobMetaDataAsync for scid: {serviceConfigurationId}, sandbox: {sandbox}");

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
                        throw new FormatException("Invalid file name format in TitleStorageBlobMetadata response");
                    }
                    if (!Enum.TryParse(filePathAndTypeArray[1], true, out TitleStorageBlobType type))
                    {
                        throw new FormatException("Invalid file type in TitleStorageBlobMetadata response");
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
    }

}
