// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace SessionHistoryViewer
{
    public class SessionHistoryQueryResponse
    {
        private List<SessionHistoryQueryResultData> QueryResult = new List<SessionHistoryQueryResultData>();

        public SessionHistoryQueryResponse(string json)
        {
            dynamic response = JToken.Parse(json);

            continuationToken = response["continuationToken"];
            for (int i = 0; i < response.results.Count; i++)
            {
                QueryResult.Add(new SessionHistoryQueryResultData()
                    {
                        sessionName = response.results[i].sessionName,
                        branch = response.results[i].branch,
                        changes = response.results[i].changes,
                        lastModified = response.results[i].lastModified,
                        isExpired = response.results[i].expired == null ? false : true,
                        activityId = response.results[i].activityId,
                    });
            }
        }

        public string continuationToken { get; set; }
        public List<SessionHistoryQueryResultData> Results
        {
            get
            {
                return QueryResult;
            }
        }
    }
}
