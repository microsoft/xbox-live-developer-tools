//-----------------------------------------------------------------------
// <copyright file="SessionHistoryQueryByGamertagRequest.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
//     Internal use only.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace SessionHistoryViewer
{
    [DataContract]
    public class SessionHistoryQueryByGamertagRequest
    {
        [DataMember(EmitDefaultValue = false)]
        public string gamertag { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public DateTime startAt { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public DateTime endAt { get; set; }
    }
}
