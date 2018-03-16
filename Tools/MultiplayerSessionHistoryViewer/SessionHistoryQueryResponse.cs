// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SessionHistoryViewer
{
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    public class SessionHistoryQueryResponse
    {
        private List<SessionHistoryQueryResultData> queryResult = new List<SessionHistoryQueryResultData>();

        public SessionHistoryQueryResponse(string json)
        {
            dynamic response = JToken.Parse(json);

            this.ContinuationToken = response["continuationToken"];
            for (int i = 0; i < response.results.Count; i++)
            {
                this.queryResult.Add(new SessionHistoryQueryResultData()
                    {
                        SessionName = response.results[i].sessionName,
                        Branch = response.results[i].branch,
                        Changes = response.results[i].changes,
                        LastModified = response.results[i].lastModified,
                        IsExpired = response.results[i].expired == null ? false : true,
                        ActivityId = response.results[i].activityId,
                    });
            }
        }

        public string ContinuationToken { get; set; }

        public List<SessionHistoryQueryResultData> Results
        {
            get
            {
                return this.queryResult;
            }
        }
    }
}
