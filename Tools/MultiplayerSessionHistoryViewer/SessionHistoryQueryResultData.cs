// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SessionHistoryViewer
{
    using System;

    public class SessionHistoryQueryResultData
    {
        public string SessionName { get; set; }

        public string Branch { get; set; }

        public int Changes { get; set; }

        public DateTime LastModified { get; set; }

        public bool IsExpired { get; set; }

        public string ActivityId { get; set; }
    }
}
