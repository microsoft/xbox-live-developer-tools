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
    using Newtonsoft.Json.Converters;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    internal class JobSubmitReqeust
    {
        public JobSubmitReqeust(string scid, string xuid)
        {
            UserId = xuid;
            Scid = scid;
        }

        [JsonProperty("userId", Required = Required.Always)]
        public string UserId { get; set; } = "deletedata";

        [JsonProperty("Scid", Required = Required.Always)]
        public string Scid { get; set; }
    }

    internal class JobSubmitResponse
    {
        public string JobId { get; set; }
        public string CorrelationId { get; set; }
    }

    public class JobProviderStatus
    {
        [JsonProperty("provider")]
        public string Provider { get; set; }

        [JsonProperty("status"),JsonConverter(typeof(StringEnumConverter))]
        public ResetProviderStatus Status { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; }

    }

    internal class JobStatusResponse
    {
        [JsonProperty("jobId")]
        public string JobId { get; set; }

        [JsonProperty("overallStatus")]
        public string Status { get; set; }

        [JsonProperty("providerStatus")]
        public List<JobProviderStatus> ProviderStatus { get; set; }
    }

    public enum ResetOverallStatus
    {
        Succeeded = 0,
        CompletedError,
        Timeout,
        Unknown
    }

    public enum ResetProviderStatus
    {
        CompletedSuccess = 0,
        CompletedError,
        NotStarted,
        InProgress,
        NotImplemented,
        Unknown
    }

    public class UserResetResult
    {
        public string XboxLiveUserId { get; internal set; }

        public ResetOverallStatus OverallStatus { get; internal set; }

        //public List<JobProviderStatus> ProviderStatus { get; internal set; }
    }

    public class ProgressResetter
    {
        private static Uri baseUri = new Uri(ClientSettings.Singleton.OmegaResetToolEndpoint);
        private const int MaxPollingAttempts = 4;

        static public async Task<IEnumerable<UserResetResult>> ResetProgressAsync(string sandbox, string scid, IEnumerable<string> xuids)
        {
            // Pre-fetch the product/sandbox etoken before getting into the loop, so that we can 
            // populate the auth error up-front.
            await Auth.GetETokenSilentlyAsync(scid, sandbox);

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
                OverallStatus = ResetOverallStatus.Unknown,
            };
            JobStatusResponse jobStatus = null;

            try
            {
                string correlationId = string.Empty;
                var jobResponse = await SubmitJobAsync(sandbox, scid, xuid);

                if (!string.IsNullOrEmpty(jobResponse.JobId))
                {
                    for (int i = 0; i < MaxPollingAttempts; i++)
                    {
                        // Wait for 3 seconds for each interval
                        await Task.Delay(3000);

                        try
                        {
                            jobStatus = await CheckJobStatus(jobResponse);
                        }
                        catch(XboxLiveException ex)
                        {
                            // TODO: BUG: service currently have bug that if polling to early for 
                            // job status, it will return 400. Right now we threat it as job queue
                            // and wait for next poll.
                            if (ex.Response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                            {
                                jobStatus = new JobStatusResponse
                                {
                                    Status = "Queued",
                                    JobId = jobResponse.JobId
                                };
                            }
                            else
                            {
                                throw ex;
                            }

                        }

                        if (jobStatus.Status == "CompletedSuccess")
                        {
                            result.OverallStatus = ResetOverallStatus.Succeeded;
                            break;
                        }
                        else if (jobStatus.Status == "CompletedError")
                        {
                            result.OverallStatus = ResetOverallStatus.CompletedError;
                            break;
                        }
                    }

                    if (jobStatus.Status == "InProgress" || jobStatus.Status == "Queued")
                    {
                        result.OverallStatus = ResetOverallStatus.Timeout;
                    }

                }
            }
            catch (XboxLiveException)
            {
                result.OverallStatus = ResetOverallStatus.CompletedError;
            }

            // Log detail status
            if(result.OverallStatus != ResetOverallStatus.Succeeded)
            {
                Log.WriteLog($"Resetting player {xuid} result {result.OverallStatus}: ");
                if (jobStatus != null && jobStatus.ProviderStatus != null)
                {
                    foreach (var providerStatus in jobStatus.ProviderStatus)
                    {
                        Log.WriteLog($"\t provider: {providerStatus.Provider}, status: {providerStatus.Status}, error message: {providerStatus.ErrorMessage}");
                    }
                }
            }

            return result;
        }

        private static void AddRequestHeaders(ref HttpRequestMessage request, string eToken)
        {
            request.Headers.Add("x-xbl-contract-version", "100");
            request.Headers.Add("Authorization", eToken);
        }

        private static async Task<JobSubmitResponse> SubmitJobAsync(string sandbox, string scid, string xuid)
        {
            var job = new JobSubmitResponse();

            using (var submitRequest = new XboxLiveHttpRequest())
            {
                var requestMsg = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "submitJob"));

                var requestContent = JsonConvert.SerializeObject(new JobSubmitReqeust(scid, xuid));
                requestMsg.Content = new StringContent(requestContent);

                string eToken = await Auth.GetETokenSilentlyAsync(scid, sandbox);
                AddRequestHeaders(ref requestMsg, eToken);

                var responseContent = await submitRequest.SendAsync(requestMsg);

                // remove "" if found one.
                job.JobId = responseContent.Content.Trim(new char[] { '\\', '\"' });
                job.CorrelationId = responseContent.CollrelationId;

                Log.WriteLog($"Submitting delete job for scid:{scid}, user:{xuid}, sandbox:{sandbox} succeeded. Jobid: {job.JobId}");
            }

            return job;
        }

        private static async Task<JobStatusResponse> CheckJobStatus(JobSubmitResponse job)
        {
            using (var submitRequest = new XboxLiveHttpRequest())
            {
                var requestMsg = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, "jobs/" + job.JobId));
                if (!string.IsNullOrEmpty(job.CorrelationId))
                {
                    requestMsg.Headers.Add("X-XblCorrelationId", job.CorrelationId);
                }

                string eToken = await Auth.GetETokenSilentlyAsync(string.Empty, string.Empty);
                AddRequestHeaders(ref requestMsg, eToken);

                var response = await submitRequest.SendAsync(requestMsg);
                var jobstatus =  JsonConvert.DeserializeObject<JobStatusResponse>(response.Content);

                Log.WriteLog($"Checking {job.JobId} job stauts: {jobstatus.Status}");

                return jobstatus;
            }
        }

    }
}
