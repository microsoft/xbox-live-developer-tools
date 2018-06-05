// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using Microsoft.Xbox.Services.DevTools.XblConfig;

    /// <summary>
    /// <para type="synopsis">Gets products for given account.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Products")]
    public class GetProducts : PSCmdletBase
    {
        /// <summary>
        /// <para type="description">The account ID associated with the products to get.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The account ID associated with the products to get.", Position = 0, ValueFromPipeline = true)]
        public Guid AccountId { get; set; }

        /// <inheritdoc />
        protected override void Process()
        {
            this.WriteVerbose("Obtaining products.");

            ConfigResponse<IEnumerable<Product>> response = ConfigurationManager.GetProductsAsync(this.AccountId).Result;
            this.WriteObject(response.Result, true);
        }
    }
}
