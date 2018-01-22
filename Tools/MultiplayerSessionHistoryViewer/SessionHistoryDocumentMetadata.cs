// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;

namespace SessionHistoryViewer
{
    public class SessionHistoryDocumentMetadata
    {
        public long changeNumber { get; set; }
        public string changedBy { get; set; }
        public DateTime timestamp { get; set; }
        public string titleId { get; set; }
        public string serviceId { get; set; }
        public string correlationId { get; set; }
        public string details { get; set; }
    }
}
