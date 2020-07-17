// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.TitleStorage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Xbox.Services.DevTools.Common;
    using Newtonsoft.Json;

    /// <summary>
    /// Class for TitleStorage tooling functionality.
    /// </summary>
    public class ConnectedStorage
    {
        private static Uri baseUri = new Uri(ClientSettings.Singleton.TitleStorageEndpoint);

        private ConnectedStorage()
        {
        }

        /// <summary>
        /// Gets a list of savedgames under a given path.
        /// </summary>
        /// <param name="xuid">The xuid of the user</param>
        /// <param name="serviceConfigurationId">The service configuration ID (SCID) of the title</param>
        /// <param name="sandbox">The target sandbox id for title storage</param>
        /// <param name="path">The root path to enumerate.  Results will be for blobs contained in this path and all subpaths.(Optional)</param>
        /// <param name="maxItems">The maximum number of items to return.</param>
        /// <param name="skipItems">The number of items to skip before returning results.</param>
        /// <returns>TitleStorageBlobMetadataResult object contains result collection.</returns>
        public static async Task<List<TitleBlobInfo>> ListSavedGamesAsync(ulong xuid, string serviceConfigurationId, string sandbox, string path, uint maxItems, uint skipItems)
        {
            List<TitleBlobInfo> savedGames = new List<TitleBlobInfo>();
            string continuationToken = null;
            do
            {
                ListTitleDataResponse response = await ListSavedGamesAsync(xuid, serviceConfigurationId, sandbox, path, maxItems, skipItems, continuationToken);
                continuationToken = response.PagingInfo.ContinuationToken;
                savedGames.AddRange(response.Blobs);
            } while (!string.IsNullOrEmpty(continuationToken));

            return savedGames;
        }

        /// <summary>
        /// Downloads a savedgame from Connected Storage.
        /// </summary>
        /// <param name="xuid">The xuid of the user</param>
        /// <param name="serviceConfigurationId">The service configuration ID (SCID) of the title</param>
        /// <param name="sandbox">The target sandbox id for title storage</param>
        /// <param name="pathAndFileName">Blob path and file name on XboxLive service.</param>
        /// <returns>The byte array contains downloaded blob data</returns>
        public static async Task<SavedGameV2Response> DownloadSavedGameAsync(ulong xuid, Guid serviceConfigurationId, string sandbox, string pathAndFileName)
        {
            using (var request = new XboxLiveHttpRequest(true, serviceConfigurationId, sandbox))
            {
                HttpResponseMessage response = (await request.SendAsync(() =>
                {
                    var requestMsg = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, $"/connectedstorage/users/xuid({xuid})/scids/{serviceConfigurationId}/savedgames/{pathAndFileName}"));
                    requestMsg.Headers.Add("x-xbl-contract-version", "1");

                    return requestMsg;
                })).Response;
                response.EnsureSuccessStatusCode();

                Log.WriteLog($"DownloadSavedGameAsync for xuid: {xuid}, scid: {serviceConfigurationId}, sandbox: {sandbox}");

                JsonSerializer jsonSerializer = JsonSerializer.CreateDefault();
                using (StreamReader streamReader = new StreamReader(await response.Content.ReadAsStreamAsync()))
                {
                    using (JsonTextReader jsonTextReader = new JsonTextReader(streamReader))
                    {
                        return jsonSerializer.Deserialize<SavedGameV2Response>(jsonTextReader);
                    }
                }
            }
        }

        /// <summary>
        /// Downloads atom from Connected Storage.
        /// </summary>
        /// <param name="xuid">The xuid of the user</param>
        /// <param name="serviceConfigurationId">The service configuration ID (SCID) of the title</param>
        /// <param name="sandbox">The target sandbox id for title storage</param>
        /// <param name="pathAndFileName">Blob path and file name to store on XboxLive service.(example: "foo\bar\blob.txt")</param>
        /// <returns>The byte array contains downloaded blob data</returns>
        public static async Task<byte[]> DownloadAtomAsync(ulong xuid, Guid serviceConfigurationId, string sandbox, string pathAndFileName)
        {
            using (var request = new XboxLiveHttpRequest(true, serviceConfigurationId, sandbox))
            {
                HttpResponseMessage response = (await request.SendAsync(()=> 
                {
                    var requestMsg = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, $"/connectedstorage/users/xuid({xuid})/scids/{serviceConfigurationId}/{pathAndFileName},binary"));
                    requestMsg.Headers.Add("x-xbl-contract-version", "1");
                    requestMsg.Headers.AcceptEncoding.Add(StringWithQualityHeaderValue.Parse("gzip"));

                    return requestMsg;
                })).Response;
                response.EnsureSuccessStatusCode();

                Log.WriteLog($"DownloadAtomAsync for xuid: {xuid}, scid: {serviceConfigurationId}, sandbox: {sandbox}");

                // if the response came gzip encoded, we need to decompress it
                if (response.Content.Headers.ContentEncoding.FirstOrDefault() == "gzip")
                {
                    using (GZipStream gzipStream = new GZipStream(await response.Content.ReadAsStreamAsync(), CompressionMode.Decompress))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await gzipStream.CopyToAsync(memoryStream);
                            return memoryStream.ToArray();
                        }
                    }
                } 
                else
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
            }
        }

        internal static async Task<ListTitleDataResponse> ListSavedGamesAsync(ulong xuid, string serviceConfigurationId, string sandbox, string path, uint maxItems,
            uint skipItems, string continuationToken)
        {
            using (var request = new XboxLiveHttpRequest(true, serviceConfigurationId, sandbox))
            {
                var uriBuilder = new UriBuilder(baseUri)
                { 
                    Path = $"/connectedstorage/users/xuid({xuid})/scids/{serviceConfigurationId}/{path}"
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
                    return new ListTitleDataResponse();
                }

                response.EnsureSuccessStatusCode();

                Log.WriteLog($"ListSavedGamesAsync for xuid: {xuid}, scid: {serviceConfigurationId}, sandbox: {sandbox}");

                JsonSerializer jsonSerializer = JsonSerializer.CreateDefault();
                using (StreamReader streamReader = new StreamReader(await response.Content.ReadAsStreamAsync()))
                {
                    using (JsonTextReader jsonTextReader = new JsonTextReader(streamReader))
                    {
                        return jsonSerializer.Deserialize<ListTitleDataResponse>(jsonTextReader);
                    }
                }
            }
        }

        private static void AppendPagingInfo(ref UriBuilder uriBuilder, uint maxItems, uint skipItems,
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
