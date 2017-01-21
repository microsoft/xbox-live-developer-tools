//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

namespace Microsoft.Xbox.Services.Tool
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Collections.Concurrent;

    internal class JobSubmitReqeust
    {
        public JobSubmitReqeust(string sandbox, string scid, string xuid)
        {
            this.JobProperties = new Dictionary<string, string>
            {
                {"Sandbox", sandbox},
                {"Scid", scid},
                {"UserId", xuid},
            };
        }

        [JsonProperty("jobType", Required = Required.Always)]
        public string JobType { get; set; } = "deletedata";

        [JsonProperty("jobProperties", Required = Required.Always)]
        public Dictionary<string, string> JobProperties { get; set; }
    }

    internal class JobStatusResponse
    {
        [JsonProperty("jobId")]
        public string JobId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("statusProperties")]
        public Dictionary<string, string> StatusProperties { get; set; }
    }

    public enum ResetStatus
    {
        Succeeded = 0,
        Failed,
        Timeout
    }

    public class UserResetResult
    {
        public string XboxLiveUserId { get; internal set; }

        public ResetStatus Status { get; internal set; }
    }

    public class ProgressResetter
    {
        private static Uri baseUri = new Uri(ClientSettings.Singleton.OmegaResetToolEndpoint);
        private const int MaxPollingAttempts = 10;

        static public async Task<IEnumerable<UserResetResult>> ResetProgressAsync(string sandbox, string scid, IEnumerable<string> xuids)
        {
            BlockingCollection<UserResetResult> result = new BlockingCollection<UserResetResult>();
            foreach (string xuid in xuids)
            {
                try
                {
                    string jobid = await SubmitJobAsync(sandbox, scid, xuid);

                    if (!string.IsNullOrEmpty(jobid))
                    {
                        string status = "InProgress";
                        for (int i = 0; i < MaxPollingAttempts; i++)
                        {
                            status = await CheckJobStatus(jobid);
                            if (status == "CompletedSuccess")
                            {
                                result.Add(new UserResetResult
                                {
                                    XboxLiveUserId = xuid,
                                    Status = ResetStatus.Succeeded,
                                });
                                break;
                            }
                            else if (status == "CompletedFailed")
                            {
                                result.Add(new UserResetResult
                                {
                                    XboxLiveUserId = xuid,
                                    Status = ResetStatus.Failed,
                                });
                                break;
                            }

                            await Task.Delay(1000);
                        }

                        if (status == "InProgress")
                        {
                            result.Add(new UserResetResult
                            {
                                XboxLiveUserId = xuid,
                                Status = ResetStatus.Timeout,
                            });
                        }
                    }
                }
                catch(XboxLiveException)
                {
                    result.Add(new UserResetResult
                    {
                        XboxLiveUserId = xuid,
                        Status = ResetStatus.Failed,
                    });
                }
            }
            return result;
        }

        private static void AddRequestHeaders(ref HttpRequestMessage request, string eToken)
        {
            request.Headers.Add("x-xbl-contract-version", "100");
            request.Headers.Add("Authorization", eToken);
        }

        private static async Task<string> SubmitJobAsync(string sandbox, string scid, string xuid)
        {
            string jobid = string.Empty;

            using (var submitRequest = new XboxLiveHttpRequest())
            {
                var requestMsg = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "submitJob"));

                var requestContent = JsonConvert.SerializeObject(new JobSubmitReqeust(sandbox, scid, xuid));
                requestMsg.Content = new StringContent(requestContent);

                string eToken = await Auth.GetXDPETokenSilentlyAsync(sandbox);
                AddRequestHeaders(ref requestMsg, eToken);

                jobid = await submitRequest.SendAsync(requestMsg);
            }

            return jobid;
        }

        private static async Task<string> CheckJobStatus(string jobid)
        {
            using (var submitRequest = new XboxLiveHttpRequest())
            {
                var requestMsg = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, "jobs/" + jobid));

                string eToken = await Auth.GetXDPETokenSilentlyAsync();
                AddRequestHeaders(ref requestMsg, eToken);

                var response = await submitRequest.SendAsync(requestMsg);
                var jobstatus =  JsonConvert.DeserializeObject<JobStatusResponse>(response);

                return jobstatus.Status;
            }
        }

    }
}
