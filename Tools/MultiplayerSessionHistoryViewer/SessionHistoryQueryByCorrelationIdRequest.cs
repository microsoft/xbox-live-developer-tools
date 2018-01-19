//-----------------------------------------------------------------------
// <copyright file="SessionHistoryQueryByCorrelationIdRequest.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
//     Internal use only.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace SessionHistoryViewer
{
    [DataContract]
    public class SessionHistoryQueryByCorrelationIdRequest
    {
        [DataMember(EmitDefaultValue = false)]
        public string correlationId { get; set; }
    }
}
