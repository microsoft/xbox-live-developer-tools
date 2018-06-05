// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.Management.Automation;
    using Microsoft.Xbox.Services.DevTools.XblConfig;

    /// <summary>
    /// <para type="synopsis">Gets a product.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Product")]
    public class GetProduct : PSCmdletBase
    {
        /// <summary>
        /// <para type="description">The product ID of the product to get.</para>
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "The product ID of the product to get.", Position = 0, ValueFromPipeline = true)]
        public Guid ProductId { get; set; }

        /// <inheritdoc />
        protected override void Process()
        {
            this.WriteVerbose("Obtaining product.");

            ConfigResponse<Product> response = ConfigurationManager.GetProductAsync(this.ProductId).Result;
            this.WriteObject(response.Result, true);
        }
    }
}
