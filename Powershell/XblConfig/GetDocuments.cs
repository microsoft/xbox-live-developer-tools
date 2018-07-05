// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Threading.Tasks;
    using Microsoft.Xbox.Services.DevTools.XblConfig;

    /// <summary>
    /// <para type="synopsis">Gets Xbox Live configuration documents.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Documents")]
    public class GetDocuments : PSCmdletBase
    {
        /// <summary>
        /// <para type="description">The service configuration ID.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The service configuration ID.", Position = 0, ValueFromPipeline = true)]
        public Guid Scid { get; set; }

        /// <summary>
        /// <para type="description">The sandbox of the product.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The sandbox of the product.", Position = 1, ValueFromPipeline = true)]
        public string Sandbox { get; set; }

        /// <summary>
        /// <para type="description">The directory to save the documents in.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The directory to save the documents in.", Position = 2, ValueFromPipeline = true)]
        public string Destination { get; set; }

        /// <summary>
        /// <para type="description">The type of documents to obtain. Can be either 'Sandbox' or 'Account'.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The type of documents to obtain. Must be either 'Sandbox' or 'Account'.", Position = 3, ValueFromPipeline = true)]
        [ValidateSet("Sandbox", "Account")]
        public string DocumentType { get; set; } = "Sandbox";

        /// <summary>
        /// <para type="description">The view to obtain.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The view to obtain. Must be either 'Working' or 'Published'.", Position = 4, ValueFromPipeline = true)]
        [ValidateSet("Working", "Published")]
        public string View { get; set; } = "Working";

        /// <summary>
        /// <para type="description">The config set version to obtain.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The config set version to obtain.", Position = 5, ValueFromPipeline = true)]
        public ulong? ConfigSetVersion { get; set; }

        /// <summary>
        /// <para type="description">The account ID of the user.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The account ID of the user.", Position = 6, ValueFromPipeline = true)]
        public Guid AccountId { get; set; }

        /// <inheritdoc />
        protected override void Process()
        {
            if (!Enum.TryParse<DocumentType>(this.DocumentType, out DocumentType documentType))
            {
                throw new ArgumentException("Invalid DocumentType. Must be either 'Sandbox' or 'Account'.", nameof(this.DocumentType));
            }

            if (!Enum.TryParse(this.View, out View view))
            {
                throw new ArgumentException("Invalid View. Must be either 'Working' or 'Published'.", nameof(this.View));
            }

            if (documentType == Microsoft.Xbox.Services.DevTools.XblConfig.DocumentType.Sandbox && string.IsNullOrEmpty(this.Sandbox))
            {
                throw new ArgumentException("Sandbox must be specified when obtaining sandbox documents.");
            }

            if (documentType == Microsoft.Xbox.Services.DevTools.XblConfig.DocumentType.Sandbox && this.Scid == Guid.Empty)
            {
                throw new ArgumentException("SCID must be specified when obtaining sandbox documents.");
            }

            if (documentType == Microsoft.Xbox.Services.DevTools.XblConfig.DocumentType.Account)
            {
                this.Sandbox = null;
            }

            this.EnsureDirectory(this.Destination);

            Task<DocumentsResponse> documentsTask;
            if (documentType == Microsoft.Xbox.Services.DevTools.XblConfig.DocumentType.Sandbox)
            {
                this.WriteVerbose("Obtaining sandbox documents.");
                documentsTask = ConfigurationManager.GetSandboxDocumentsAsync(this.Scid, this.Sandbox);
            }
            else
            {
                this.WriteVerbose("Obtaining account documents.");
                documentsTask = ConfigurationManager.GetAccountDocumentsAsync(this.AccountId);
            }

            using (DocumentsResponse documents = documentsTask.Result)
            {
                this.WriteVerbose($"ETag: {documents.ETag}");
                this.WriteVerbose($"Version: {documents.Version}");
                this.WriteVerbose("Files: ");

                foreach (ConfigFileStream file in documents.Documents)
                {
                    string path = Path.Combine(this.Destination, file.Name);
                    using (FileStream fileStream = File.Create(path))
                    {
                        file.Stream.CopyTo(fileStream);
                    }

                    this.WriteVerbose($" - {file.Name}");
                }

                this.SaveETag(documents.ETag, this.Destination, this.Sandbox);
                this.WriteVerbose($"Saved {documents.Documents.Count()} files to {this.Destination}.");
            }
        }
    }
}
