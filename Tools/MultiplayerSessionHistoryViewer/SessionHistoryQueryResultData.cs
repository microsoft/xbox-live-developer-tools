//-----------------------------------------------------------------------
// <copyright file="SessionHistoryQueryResultData.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
//     Internal use only.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SessionHistoryViewer
{
    public class SessionHistoryQueryResultData
    {
        public string sessionName { get; set; }
        public string branch { get; set; }
        public int changes { get; set; }
        public DateTime lastModified { get; set; }
        public bool isExpired { get; set; }
        public string activityId { get; set; }
    }
}
