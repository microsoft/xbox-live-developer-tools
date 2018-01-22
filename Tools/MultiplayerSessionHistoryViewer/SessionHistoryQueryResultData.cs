// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


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
