// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net.Http.Headers;

    /// <summary>
    /// Represents a response of configuration documents from Xbox Live.
    /// </summary>
    public class DocumentsResponse : ConfigResponseBase, IDisposable
    {
        /// <summary>
        /// Creates a new instance of the <see cref="DocumentsResponse"/> object.
        /// </summary>
        /// <param name="documentsStream">A stream of a zip file containing the configuration documents.</param>
        /// <param name="headers">The collection of headers that came back during the request.</param>
        internal DocumentsResponse(Stream documentsStream, HttpResponseHeaders headers)
        {
            this.CorrelationId = headers.GetValues("MS-CV").FirstOrDefault();
            this.Version = int.Parse(headers.GetValues("X-Version").FirstOrDefault());
            this.ETag = headers.ETag.Tag;
            ZipArchive documents = new ZipArchive(documentsStream, ZipArchiveMode.Read, false);
            List<ConfigFileStream> files = new List<ConfigFileStream>();
            foreach (ZipArchiveEntry document in documents.Entries)
            {
                MemoryStream ms = new MemoryStream();
                document.Open().CopyTo(ms);
                ms.Position = 0;
                files.Add(new ConfigFileStream(document.Name, ms));
            }

            this.Documents = files;
        }

        /// <summary>
        /// Gets the version of the changeset.
        /// </summary>
        public int Version { get; }

        /// <summary>
        /// Gets the ETag associated with the changeset.
        /// </summary>
        public string ETag { get; }

        /// <summary>
        /// Gets a collection of <see cref="ConfigFileStream"/> objects containing the documents in this changeset.
        /// </summary>
        public IEnumerable<ConfigFileStream> Documents { get; }

        /// <summary>
        /// Releases all resources used by the <see cref="DocumentsResponse"/>.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="DocumentsResponse"/>.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.Documents != null)
                {
                    foreach (ConfigFileStream document in this.Documents)
                    {
                        if (document.Stream != null)
                        {
                            document.Dispose();
                        }
                    }
                }
            }
        }
    }
}
