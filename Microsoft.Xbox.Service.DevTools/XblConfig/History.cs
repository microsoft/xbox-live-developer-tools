// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a history entry for a document commit.
    /// </summary>
    public class History
    {
        /// <summary>
        /// Gets or sets the <see cref="DateTime"/> that the commit happened.
        /// </summary>
        public DateTime CommitTime { get; set; }

        /// <summary>
        /// Gets or sets the ETag associated with a commit.
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        /// Gets or sets the message saved during the commit.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the user ID of the user that initiated the commit.
        /// </summary>
        public ulong User { get; set; }

        /// <summary>
        /// Gets or sets the version of the changeset.
        /// </summary>
        public ulong Version { get; set; }

        /// <summary>
        /// Gets or sets the collection of documents associated with the changeset.
        /// </summary>
        public Dictionary<string, DocumentHashSet> Documents { get; set; }

        /// <summary>
        /// Represents a name/value pair of documents and their hash.
        /// </summary>
        public class DocumentHashSet
        {
            /// <summary>
            /// Gets or sets a hash associated with a document.
            /// </summary>
            public string Hash { get; set; }

            /// <summary>
            /// Gets or sets the name of the document.
            /// </summary>
            public string Name { get; set; }
        }
    }
}
