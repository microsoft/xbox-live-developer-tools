// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SessionHistoryViewer
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public class SessionHistoryQueryByXuidRequest
    {
        [DataMember(Name = "xuid", EmitDefaultValue = false)]
        public string Xuid { get; set; }

        [DataMember(Name = "startAt", EmitDefaultValue = false)]
        public DateTime StartAt { get; set; }

        [DataMember(Name = "endAt", EmitDefaultValue = false)]
        public DateTime EndAt { get; set; }
    }
}
