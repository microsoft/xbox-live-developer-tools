//-----------------------------------------------------------------------
// <copyright file="HistoricalDocument.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
//     Internal use only.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;

namespace SessionHistoryViewer.DataContracts
{
    public class HistoricalDocument
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public IList<DocumentStateSnapshot> DocumentSnapshots = new List<DocumentStateSnapshot>();
        public string SessionName { get; set; }
        public string Branch { get; set; }
        public string LastModified { get; set; }
        public bool IsExpired { get; set; }
        public int NumSnapshots { get; set; }
        public string ActivityId { get; set; }
    }
}
