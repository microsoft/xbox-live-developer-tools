// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.IO;
    using System.Management.Automation;
    using System.Security;
    using Microsoft.Xbox.Services.DevTools.XblConfig;

    /// <summary>
    /// <para type="synopsis">Generates a web service certificate.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "WebServiceCertificate")]
    public class NewWebServiceCertificate : PSCmdletBase
    {
        /// <summary>
        /// <para type="description">The ID of the web service.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The ID of the web service.", Position = 0, ValueFromPipeline = true)]
        public Guid ServiceId { get; set; }

        /// <summary>
        /// <para type="description">The location to save the certificate to.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The location to save the certificate to.", Position = 1, ValueFromPipeline = true)]
        public string Destination { get; set; }

        /// <summary>
        /// <para type="description">The account ID that owns the web service.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The account ID that owns the web service.", Position = 2, ValueFromPipeline = true)]
        public Guid AccountId { get; set; }

        /// <inheritdoc />
        protected override void Process()
        {
            this.WriteVerbose("Generating web service certificate.");

            FileInfo fi = new FileInfo(this.Destination);
            this.EnsureDirectory(fi.Directory.FullName);

            this.Host.UI.Write("Please enter the password you would like to secure this certificate with: ");
            
            using (SecureString password = this.Host.UI.ReadLineAsSecureString())
            {
                ConfigResponse<Stream> response = ConfigurationManager.GenerateWebServiceCertificateAsync(this.AccountId, this.ServiceId, password).Result;
                using (FileStream fileStream = File.Create(this.Destination))
                {
                    response.Result.CopyTo(fileStream);
                }
            }

            this.WriteObject("Certificate generated.");
        }
    }
}
