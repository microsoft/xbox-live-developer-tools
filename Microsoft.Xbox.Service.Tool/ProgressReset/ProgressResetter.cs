// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.Tool
{
    using Newtonsoft.Json;
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    public class PlayerResetter
    {
        private static Uri baseUri = new Uri(ClientSettings.Singleton.OmegaResetToolEndpoint);
        private const int MaxPollingAttempts = 4;

        /// <summary>
        /// Reset one player's data in test sandboxes, includes: achievements, leaderboards, player stats and title history. 
        /// </summary>
        /// <param name="serviceConfigurationId">The service configuration ID (SCID) of the title for player data resetting</param>
        /// <param name="sandbox">The target sandbox id for player resetting</param>
        /// <param name="xboxUserId">The xbox user id of the player to be reset</param>
        /// <returns></returns>
        static public async Task<UserResetResult> ResetPlayerDataAsync(string serviceConfigurationId, string sandbox, string xboxUserId)
        {
            // Pre-fetch the product/sandbox etoken before getting into the loop, so that we can 
            // populate the auth error up-front.
            await Auth.GetETokenSilentlyAsync(serviceConfigurationId, sandbox);

            return await SubmitJobAndPollStatus(sandbox, serviceConfigurationId, xboxUserId);
        }

        private static async Task<UserResetResult> SubmitJobAndPollStatus(string sandbox, string scid, string xuid)
        {
            UserResetResult result = new UserResetResult();
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

        private static async Task<UserResetJob> SubmitJobAsync(string sandbox, string scid, string xuid)
        {
            var job = new UserResetJob{Sandbox = sandbox, Scid = scid};

            using (var submitRequest = new XboxLiveHttpRequest())
            {
                var requestMsg = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "submitJob"));

                var requestContent = JsonConvert.SerializeObject(new JobSubmitReqeust(scid, xuid));
                requestMsg.Content = new StringContent(requestContent);

                string eToken = await Auth.GetETokenSilentlyAsync(scid, sandbox);
                AddRequestHeaders(ref requestMsg, eToken);

                var response = await submitRequest.SendAsync(requestMsg);

                // remove "" if found one.
                string responseContent = await response.Content.ReadAsStringAsync();
                job.JobId = responseContent.Trim(new char[] { '\\', '\"' });
                job.CorrelationId = response.CollrelationId;

                Log.WriteLog($"Submitting delete job for scid:{scid}, user:{xuid}, sandbox:{sandbox} succeeded. Jobid: {job.JobId}");
            }

            return job;
        }

        private static async Task<JobStatusResponse> CheckJobStatus(UserResetJob userResetJob)
        {
            using (var submitRequest = new XboxLiveHttpRequest())
            {
                var requestMsg = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, "jobs/" + userResetJob.JobId));
                if (!string.IsNullOrEmpty(userResetJob.CorrelationId))
                {
                    requestMsg.Headers.Add("X-XblCorrelationId", userResetJob.CorrelationId);
                }

                string eToken = await Auth.GetETokenSilentlyAsync(userResetJob.Scid, userResetJob.Sandbox);
                AddRequestHeaders(ref requestMsg, eToken);

                var response = await submitRequest.SendAsync(requestMsg);
                string responseConent = await response.Content.ReadAsStringAsync();
                var jobstatus =  JsonConvert.DeserializeObject<JobStatusResponse>(responseConent);

                Log.WriteLog($"Checking {userResetJob.JobId} job stauts: {jobstatus.Status}");

                return jobstatus;
            }
        }

    }
}
