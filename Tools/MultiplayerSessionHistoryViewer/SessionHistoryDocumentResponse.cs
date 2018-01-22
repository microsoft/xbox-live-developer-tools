//-----------------------------------------------------------------------
// <copyright file="SessionHistoryDocumentResponse.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
//     Internal use only.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace SessionHistoryViewer
{
    public class SessionHistoryDocumentResponse
    {
        private List<SessionHistoryDocumentMetadata> QueryResult = new List<SessionHistoryDocumentMetadata>();

        public SessionHistoryDocumentResponse(string json)
        {
            dynamic response = JToken.Parse(json);

            for (int i = 0; i < response.results.Count; i++)
            {
                string changedByUsers = string.Empty;
                foreach (var user in response.results[i].changedBy)
                {
                    changedByUsers += string.Format("{0}, ", user.ToString());
                }

                if (changedByUsers.Length > 0)
                {
                    changedByUsers = changedByUsers.Trim();
                    changedByUsers = changedByUsers.TrimEnd(new char[] { ',' });
                }

                string changeSummary = string.Empty;
                foreach (var change in response.results[i].details)
                {
                    changeSummary += string.Format("{0}, ", change);
                }

                if (changeSummary.Length > 0)
                {
                    changeSummary = changeSummary.Trim();
                    changeSummary = changeSummary.TrimEnd(new char[] { ',' });
                }

                QueryResult.Add(new SessionHistoryDocumentMetadata()
                    {
                        changeNumber = response.results[i].changeNumber,
                        changedBy = changedByUsers,
                        timestamp = response.results[i].timestamp,
                        titleId = response.results[i].titleId,
                        serviceId = response.results[i].serviceId,
                        correlationId = response.results[i].correlationId,
                        details = changeSummary
                    });
            }
        }

        public string continuationToken { get; set; }
        public List<SessionHistoryDocumentMetadata> Results
        {
            get
            {
                return QueryResult;
            }
        }
    }
}
