// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig
{
    using System;
    using System.IO;

    /// <summary>
    /// Represents an Xbox Live configuration file.
    /// </summary>
    public class ConfigFileStream : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigFileStream"/> class.
        /// </summary>
        /// <param name="name">The name of the file.</param>
        /// <param name="stream">The stream containing the contents of the configuration file.</param>
        internal ConfigFileStream(string name, Stream stream)
        {
            this.Name = name;
            this.Stream = stream;
        }

        /// <summary>
        /// Gets the name of the configuration file.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the stream of the contents of the configuration file.
        /// </summary>
        public Stream Stream { get; }

        /// <summary>
        /// Releases all resources used by the <see cref="ConfigFileStream"/>.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="ConfigFileStream"/>.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.Stream != null)
                {
                    this.Stream.Dispose();
                }
            }
        }
    }
}
