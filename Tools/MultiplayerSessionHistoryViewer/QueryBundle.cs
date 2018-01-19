//-----------------------------------------------------------------------
// <copyright file="QueryBundle.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
//     Internal use only.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SessionHistoryViewer
{
    public class QueryBundle
    {
        public string Sandbox { get; set; }
        public string Scid { get; set; }
        public string TemplateName { get; set; }
        public string QueryKey { get; set; }
        public int QueryKeyIndex { get; set; }
        public DateTime QueryFrom { internal get; set; }
        public DateTime QueryTo { internal get; set; }

        // Convert Query StartAt date to align with the UTC timestamp indexes of the data on the service
        public DateTime QueryStartDate
        {
            get
            {
                return QueryFrom.ToUniversalTime();
            }
        }

        // Convert Query endAt date to align with the UTC timestamp indexes of the data on the service
        public DateTime QueryEndDate
        {
            get
            {
                DateTime endDate = new DateTime(QueryTo.Year, QueryTo.Month, QueryTo.Day, 23, 59, 59); // align end date to the end of the day
                return endDate.ToUniversalTime();
            }
        }
    }
}
