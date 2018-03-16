// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Represents a configuration response of type T.
    /// </summary>
    /// <typeparam name="T">The strongly typed result of the response.</typeparam>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleType", Justification = "Contains the same type with different type parameters.")]
    public class ConfigResponse<T> : ConfigResponseBase
    {
        /// <summary>
        /// Gets or sets result of the response.
        /// </summary>
        public T Result { get; set; }
    }

    /// <summary>
    /// Represents a configuration response.
    /// </summary>
    public class ConfigResponse : ConfigResponseBase
    {
    }
}
