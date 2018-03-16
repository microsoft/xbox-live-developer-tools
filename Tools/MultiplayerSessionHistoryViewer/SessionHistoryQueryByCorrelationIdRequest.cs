// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SessionHistoryViewer
{
    using System.Runtime.Serialization;

    [DataContract]
    public class SessionHistoryQueryByCorrelationIdRequest
    {
        [DataMember(Name = "correlationId", EmitDefaultValue = false)]
        public string CorrelationId { get; set; }
    }
}
