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
        Timeout,
        Unknown
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
            var tasks = new List<Task<UserResetResult>>();
            foreach (string xuid in xuids)
            {
                tasks.Add(SubmitJobAndPollStatus(sandbox, scid, xuid));
            }

            await Task.WhenAll(tasks.ToArray());
            return tasks.Select(task => task.Result).ToList(); ;
        }

        private static async Task<UserResetResult> SubmitJobAndPollStatus(string sandbox, string scid, string xuid)
        {
            UserResetResult result = new UserResetResult
            {
                XboxLiveUserId = xuid,
                Status = ResetStatus.Unknown,
            };

            try
            {
                string jobid = await SubmitJobAsync(sandbox, scid, xuid);

                if (!string.IsNullOrEmpty(jobid))
                {
                    string status = "";
                    for (int i = 0; i < MaxPollingAttempts; i++)
                    {
                        status = await CheckJobStatus(jobid);
                        if (status == "CompletedSuccess")
                        {
                            result.Status = ResetStatus.Succeeded;
                            break;
                        }
                        else if (status == "CompletedFailed")
                        {
                            result.Status = ResetStatus.Failed;
                            break;
                        }

                        await Task.Delay(1000);
                    }

                    if (status == "InProgress" || status == "Queued")
                    {
                        result.Status = ResetStatus.Timeout;
                    }

                }
            }
            catch (XboxLiveException)
            {
                result.Status = ResetStatus.Failed;
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

                // remove "" if found one.
                jobid = jobid.Trim(new char[] { '\\', '\"' });
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
