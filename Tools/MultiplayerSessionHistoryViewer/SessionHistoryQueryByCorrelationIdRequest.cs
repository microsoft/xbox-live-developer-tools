// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


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
