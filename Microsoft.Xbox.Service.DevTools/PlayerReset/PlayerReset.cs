// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.PlayerReset
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Xbox.Services.DevTools.Authentication;
    using Microsoft.Xbox.Services.DevTools.Common;
    using Newtonsoft.Json;

    /// <summary>
    /// Class for PlayerReset tooling functionality.
    /// </summary>
    public class PlayerReset
    {
        internal const int MaxPollingAttempts = 4;
        internal const int MaxRetryAttempts = 4;
        private static Uri baseUri = new Uri(ClientSettings.Singleton.OmegaResetToolEndpoint);

        private PlayerReset()
        {
        }
 
        internal static int RetryDelay { get; set; } = 3000;

        /// <summary>
        /// Reset one player's data in test sandboxes, includes: achievements, leaderboards, player stats and title history. 
        /// </summary>
        /// <param name="serviceConfigurationId">The service configuration ID (SCID) of the title for player data resetting</param>
        /// <param name="sandbox">The target sandbox id for player resetting</param>
        /// <param name="xboxUserIds">The Xbox user ids of the player to be reset</param>
        /// <returns>The UserResetResult object for the reset result</returns>
        public static async Task<UserResetResult> ResetPlayerDataAsync(string serviceConfigurationId, string sandbox, List<string> xboxUserIds)
        {
            // Pre-fetch the product/sandbox etoken before getting into the loop, so that we can 
            // populate the auth error up-front.
            if (ToolAuthentication.Client.AuthContext.AccountSource == DevAccountSource.TestAccount)
            {
                await ToolAuthentication.GetTestTokenSilentlyAsync(sandbox);
            }
            else
            {
                await ToolAuthentication.GetDevTokenSilentlyAsync(serviceConfigurationId, sandbox);
            }

            // Initialize xuid-task mapping
            Dictionary<string, Task<UserResetResult>> userTaskMap = new Dictionary<string, Task<UserResetResult>>();
            foreach (string xuid in xboxUserIds)
            {
                userTaskMap.Add(xuid, null);
            }

            for (int i = 0; i <= MaxRetryAttempts; i++)
            {
                // Run a task per id
                List<string> ids = userTaskMap.Keys.ToList();
                foreach (string userId in ids)
                {
                    userTaskMap[userId] = SubmitJobAndPollStatus(sandbox, serviceConfigurationId, userId, MaxRetryAttempts - i > 1);
                }

                var t = Task.WhenAll(userTaskMap.Values);

                // Remove xuids that succeeded
                foreach (string key in userTaskMap.Keys.ToArray()
                    .Where(xuid => userTaskMap[xuid].Result.OverallResult == ResetOverallResult.Succeeded))
                    userTaskMap.Remove(key);

                // If everything succeeded, end the loop
                if (userTaskMap.Count == 0)
                {
                    break;
                }
                else if (i < MaxRetryAttempts)
                {
                    Console.WriteLine($"Retrying {userTaskMap.Count} accounts. {MaxRetryAttempts - i} more attempt(s).");
                }
            }

            UserResetResult result = new UserResetResult();

            var failedTaskMap = userTaskMap.Where(kvp => kvp.Value.Result.OverallResult != ResetOverallResult.Succeeded).ToDictionary(t => t.Key, t=>t.Value);
            if (failedTaskMap.Count > 0)
            {
                var firstFailedTask = failedTaskMap.First().Value;
                result.OverallResult = firstFailedTask.Result.OverallResult;
                result.ProviderStatus = firstFailedTask.Result.ProviderStatus;
                result.HttpErrorMessage = firstFailedTask.Result.HttpErrorMessage;

                string failedXuids = string.Join(",", failedTaskMap.Keys.ToList());
                Console.WriteLine($"Failed to reset {failedTaskMap.Count} account(s): {failedXuids}");
            }
            else
            {
                result.OverallResult = ResetOverallResult.Succeeded;
            }
            return result;
        }

        private static async Task<UserResetResult> SubmitJobAndPollStatus(string sandbox, string scid, string xuid, bool canRetry)
        {
            UserResetResult result = new UserResetResult();
            JobStatusResponse jobStatus = null;

            try
            {
                string correlationId = string.Empty;
                var jobResponse = await SubmitJobAsync(sandbox, scid, xuid);

                if (!string.IsNullOrEmpty(jobResponse.HttpErrorMessage))
                {
                    result.OverallResult = ResetOverallResult.CompletedError;
                    result.HttpErrorMessage = jobResponse.HttpErrorMessage;
                }
                else if (!string.IsNullOrEmpty(jobResponse.JobId))
                {
                    for (int i = 0; i < MaxPollingAttempts; i++)
                    {
                        // Wait for 3 seconds for each interval
                        await Task.Delay(RetryDelay);

                        jobStatus = await CheckJobStatus(jobResponse);

                        if (jobStatus.Status == "CompletedSuccess")
                        {
                            result.OverallResult = ResetOverallResult.Succeeded;
                            result.ProviderStatus = jobStatus.ProviderStatus;
                            break;
                        }
                        else if (jobStatus.Status == "CompletedError")
                        {
                            result.OverallResult = ResetOverallResult.CompletedError;
                            result.ProviderStatus = jobStatus.ProviderStatus;
                            break;
                        }
                    }

                    if (jobStatus.Status == "InProgress" || jobStatus.Status == "Queued")
                    {
                        result.OverallResult = ResetOverallResult.Timeout;
                        result.ProviderStatus = jobStatus.ProviderStatus;
                    }
                }
            }
            catch (Exception)
            {
                result.OverallResult = ResetOverallResult.CompletedError;
            }

            // Log detail status
            // Log.WriteLog($"Resetting player {xuid} result {result.OverallResult}: ");
            string retryClause = result.OverallResult == ResetOverallResult.Succeeded || !canRetry ? string.Empty : ". Will retry.";
            Console.WriteLine($"Resetting player {xuid} result: {result.OverallResult}{retryClause}");
            if (result.OverallResult != ResetOverallResult.Succeeded)
            {
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

        private static async Task<UserResetJob> SubmitJobAsync(string sandbox, string scid, string xuid)
        {
            var job = new UserResetJob { Sandbox = sandbox, Scid = scid };

            using (var submitRequest = new XboxLiveHttpRequest(true, scid, sandbox))
            {
                var response = await submitRequest.SendAsync(()=> 
                {
                    var requestMsg = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "submitJob"));
                    var requestContent = JsonConvert.SerializeObject(new JobSubmitRequest(scid, xuid));
                    requestMsg.Content = new StringContent(requestContent);
                    requestMsg.Headers.Add("x-xbl-contract-version", "100");

                    return requestMsg;
                });

                if (response.Response.IsSuccessStatusCode)
                {
                    // remove "" if found one.
                    string responseContent = await response.Response.Content.ReadAsStringAsync();
                    job.JobId = responseContent.Trim(new char[] { '\\', '\"' });
                    job.CorrelationId = response.CorrelationId;

                    Log.WriteLog($"Submitting delete job for scid:{scid}, user:{xuid}, sandbox:{sandbox} succeeded. Jobid: {job.JobId}");
                }
                else
                {
                    job.HttpErrorMessage = await response.Response.Content.ReadAsStringAsync();
                    Log.WriteLog($"Submitting delete job for scid:{scid}, user:{xuid}, sandbox:{sandbox} failed. Error: {job.HttpErrorMessage}");
                }
            }

            return job;
        }

        private static async Task<JobStatusResponse> CheckJobStatus(UserResetJob userResetJob)
        {
            using (var submitRequest = new XboxLiveHttpRequest(true, userResetJob.Scid, userResetJob.Sandbox))
            {
                XboxLiveHttpResponse xblResponse = await submitRequest.SendAsync(()=>
                {
                    var requestMsg = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, "jobs/" + userResetJob.JobId));
                    if (!string.IsNullOrEmpty(userResetJob.CorrelationId))
                    {
                        requestMsg.Headers.Add("X-XblCorrelationId", userResetJob.CorrelationId);
                    }
                    requestMsg.Headers.Add("x-xbl-contract-version", "100");

                    return requestMsg;
                });

                // There is a chance if polling too early for job status, it will return 400. 
                // We threat it as job queue and wait for next poll.
                if (xblResponse.Response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    return new JobStatusResponse
                    {
                        Status = "Queued",
                        JobId = userResetJob.JobId
                    };
                }

                // Throw HttpRequestExcetpion for other HTTP status code
                xblResponse.Response.EnsureSuccessStatusCode();

                var jobstatus = await xblResponse.Response.Content.DeserializeJsonAsync<JobStatusResponse>();

                Log.WriteLog($"Checking {userResetJob.JobId} job status: {jobstatus.Status}");

                return jobstatus;
            }
        }
    }
}
