//-----------------------------------------------------------------------
// <copyright file="DocumentStateSnapshot.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
//     Internal use only.
// </copyright>
//-----------------------------------------------------------------------


using System;
using System.Collections.Generic;

namespace SessionHistoryViewer.DataContracts
{
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
