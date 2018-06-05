// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Threading.Tasks;
    using Microsoft.Xbox.Services.DevTools.XblConfig;

    /// <summary>
    /// <para type="synopsis">Commits documents back to Xbox Live for the given sandbox.</para>
    /// </summary>
    [Cmdlet(VerbsData.Save, "Documents")]
    public class SaveDocuments : PSCmdletBase
    {
        /// <summary>
        /// <para type="description">The files to commit.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The files to commit.", Position = 0, ValueFromPipeline = true)]
        public string[] Files { get; set; }

        /// <summary>
        /// <para type="description">The service configuration ID.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The service configuration ID.", Position = 1, ValueFromPipeline = true)]
        public Guid Scid { get; set; }

        /// <summary>
        /// <para type="description">The sandbox of the product.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The sandbox of the product.", Position = 2, ValueFromPipeline = true)]
        public string Sandbox { get; set; }

        /// <summary>
        /// <para type="description">Pass 'true' to only attempt the commit, 'false' to actually commit it. Defaults to 'true'.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Pass 'true' to only attempt the commit, 'false' to actually commit it. Defaults to 'true'.", Position = 3, ValueFromPipeline = true)]
        public bool ValidateOnly { get; set; }

        /// <summary>
        /// <para type="description">The ETag of the changeset that was previously committed. If this is unknown, the ETag stored by this tool previously will be used.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The ETag of the changeset that was previously committed. If this is unknown, the ETag stored by this tool previously will be used.", Position = 4, ValueFromPipeline = true)]
        public string ETag { get; set; }

        /// <summary>
        /// <para type="description">Setting this will override the ETag requirement and force commit the documents regardless of concurrency violations.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Setting this will override the ETag requirement and force commit the documents regardless of concurrency violations.", Position = 5, ValueFromPipeline = true)]
        public bool Force { get; set; }

        /// <summary>
        /// <para type="description">The type of documents to obtain.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The type of documents to obtain.", Position = 6, ValueFromPipeline = true)]
        [ValidateSet("Sandbox", "Account")]
        public string DocumentType { get; set; }

        /// <summary>
        /// <para type="description">The commit message to save with.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The commit message to save with.", Position = 7, ValueFromPipeline = true)]
        public string Message { get; set; }

        /// <summary>
        /// <para type="description">The account ID of the user.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The account ID of the user.", Position = 8, ValueFromPipeline = true)]
        public Guid AccountId { get; set; }

        /// <inheritdoc/>
        protected override void Process()
        {
            if (!Enum.TryParse<DocumentType>(this.DocumentType, out DocumentType documentType))
            {
                throw new ArgumentException("Invalid DocumentType. Must be either 'Sandbox' or 'Account'.", nameof(this.DocumentType));
            }

            if (documentType == Microsoft.Xbox.Services.DevTools.XblConfig.DocumentType.Sandbox && string.IsNullOrEmpty(this.Sandbox))
            {
                throw new ArgumentException("Sandbox must be specified when committing sandbox documents.");
            }

            if (documentType == Microsoft.Xbox.Services.DevTools.XblConfig.DocumentType.Account)
            {
                this.Sandbox = null;
            }

            IEnumerable<string> files = this.Glob(this.Files);
            int fileCount = files.Count();
            if (fileCount == 0)
            {
                throw new ArgumentException("There are no files selected to commit.", nameof(this.Files));
            }

            this.WriteVerbose($"Committing {fileCount} file(s) to Xbox Live.");

            string eTag = this.ETag ?? this.GetETag(files, this.Sandbox);
            if (this.Force)
            {
                eTag = null;
            }

            Task<ConfigResponse<ValidationResponse>> documentsTask;
            if (documentType == Microsoft.Xbox.Services.DevTools.XblConfig.DocumentType.Sandbox)
            {
                this.WriteVerbose("Committing sandbox documents.");
                documentsTask = ConfigurationManager.CommitSandboxDocumentsAsync(files, this.Scid, this.Sandbox, eTag, this.ValidateOnly, this.Message);
            }
            else
            {
                this.WriteVerbose("Committing account documents.");
                documentsTask = ConfigurationManager.CommitAccountDocumentsAsync(files, this.AccountId, eTag, this.ValidateOnly, this.Message);
            }

            ConfigResponse<ValidationResponse> result = documentsTask.Result;

            this.SaveETag(result.Result.ETag, Path.GetDirectoryName(files.First()), this.Sandbox);

            this.WriteVerbose($"Can Commit: {result.Result.CanCommit}");
            this.WriteVerbose($"Committed:  {result.Result.Committed}");

            this.PrintValidationInfo(result.Result.ValidationInfo);
        }
    }
}
