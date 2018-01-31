// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SessionHistoryViewer.DataContracts
{
    using System.Collections.Generic;

    public class HistoricalDocument
    {
        public IList<DocumentStateSnapshot> DocumentSnapshots { get; set; } = new List<DocumentStateSnapshot>();

        public string SessionName { get; set; }

        public string Branch { get; set; }

        public string LastModified { get; set; }

        public bool IsExpired { get; set; }

        public int NumSnapshots { get; set; }

        public string ActivityId { get; set; }
    }
}
