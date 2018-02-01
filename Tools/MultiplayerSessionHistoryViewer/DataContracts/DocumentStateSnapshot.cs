// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SessionHistoryViewer.DataContracts
{
    using System;

    public class DocumentStateSnapshot
    {
        public string ChangeDetails { get; set; }

        public string ModifiedByXuids { get; set; }

        public DateTime Timestamp { get; set; }

        public long Change { get; set; }

        public string TitleId { get; set; }

        public string ServiceId { get; set; }

        public string CorrelationId { get; set; }

        public string Body { get; set; }
    }
}
