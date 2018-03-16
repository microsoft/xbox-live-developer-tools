// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SessionHistoryViewer
{
    using System;

    public class SessionHistoryDocumentMetadata
    {
        public long ChangeNumber { get; set; }

        public string ChangedBy { get; set; }

        public DateTime Timestamp { get; set; }

        public string TitleId { get; set; }

        public string ServiceId { get; set; }

        public string CorrelationId { get; set; }

        public string Details { get; set; }
    }
}
