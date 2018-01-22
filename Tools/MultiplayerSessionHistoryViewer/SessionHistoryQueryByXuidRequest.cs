//-----------------------------------------------------------------------
// <copyright file="SessionHistoryQueryByXuidRequest.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
//     Internal use only.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace SessionHistoryViewer
{
    [DataContract]
    public class SessionHistoryQueryByXuidRequest
    {
        [DataMember(EmitDefaultValue = false)]
        public string xuid { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public DateTime startAt { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public DateTime endAt { get; set; }
    }
}
