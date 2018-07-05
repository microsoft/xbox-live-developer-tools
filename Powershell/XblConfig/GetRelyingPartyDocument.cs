// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using Microsoft.Xbox.Services.DevTools.XblConfig;

    /// <summary>
    /// <para type="synopsis">Gets a specific relying party document.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "RelyingPartyDocument")]
    public class GetRelyingPartyDocument : PSCmdletBase
    {
        /// <summary>
        /// <para type="description">The filename of the document to retrieve.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The filename of the document to retrieve.", Position = 0, ValueFromPipeline = true)]
        public string Filename { get; set; }

        /// <summary>
        /// <para type="description">The directory to save the document in.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The directory to save the document in.", Position = 1, ValueFromPipeline = true)]
        public string Destination { get; set; }

        /// <summary>
        /// <para type="description">The account ID that owns the relying parties.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The account ID that owns the relying parties.", Position = 2, ValueFromPipeline = true)]
        public Guid AccountId { get; set; }

        /// <inheritdoc />
        protected override void Process()
        {
            this.WriteVerbose("Obtaining relying party document.");

            DocumentsResponse response = ConfigurationManager.GetRelyingPartyDocumentAsync(this.AccountId, this.Filename).Result;
            ConfigFileStream document = response.Documents.First();
            using (Stream stream = document.Stream)
            {
                if (string.IsNullOrEmpty(this.Destination))
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        this.WriteObject(sr.ReadToEnd());
                    }
                }
                else
                {
                    this.EnsureDirectory(this.Destination);
                    string path = Path.Combine(this.Destination, document.Name);
                    using (StreamWriter sw = File.CreateText(path))
                    {
                        stream.CopyTo(sw.BaseStream);
                    }

                    this.WriteVerbose($"Document saved as {path}");
                }
            }
        }
    }
}
