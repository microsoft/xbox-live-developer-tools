// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SessionHistoryViewer
{
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    public class SessionHistoryDocumentResponse
    {
        private List<SessionHistoryDocumentMetadata> queryResult = new List<SessionHistoryDocumentMetadata>();

        public SessionHistoryDocumentResponse(string json)
        {
            dynamic response = JToken.Parse(json);

            for (int i = 0; i < response.results.Count; i++)
            {
                string changedByUsers = string.Empty;
                foreach (var user in response.results[i].changedBy)
                {
                    changedByUsers += $"{user}, ";
                }

                if (changedByUsers.Length > 0)
                {
                    changedByUsers = changedByUsers.Trim();
                    changedByUsers = changedByUsers.TrimEnd(new char[] { ',' });
                }

                string changeSummary = string.Empty;
                foreach (var change in response.results[i].details)
                {
                    changeSummary += $"{change}, ";
                }

                if (changeSummary.Length > 0)
                {
                    changeSummary = changeSummary.Trim();
                    changeSummary = changeSummary.TrimEnd(new char[] { ',' });
                }

                this.queryResult.Add(new SessionHistoryDocumentMetadata()
                    {
                        ChangeNumber = response.results[i].changeNumber,
                        ChangedBy = changedByUsers,
                        Timestamp = response.results[i].timestamp,
                        TitleId = response.results[i].titleId,
                        ServiceId = response.results[i].serviceId,
                        CorrelationId = response.results[i].correlationId,
                        Details = changeSummary
                    });
            }
        }

        public string ContinuationToken { get; set; }

        public List<SessionHistoryDocumentMetadata> Results
        {
            get
            {
                return this.queryResult;
            }
        }
    }
}
