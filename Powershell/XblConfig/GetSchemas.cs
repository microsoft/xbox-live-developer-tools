// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using Microsoft.Xbox.Services.DevTools.XblConfig;

    /// <summary>
    /// <para type="synopsis">Gets document schemas.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Schemas")]
    [CmdletBinding(PositionalBinding = false)]
    public class GetSchemas : PSCmdletBase<GetSchemas.Template>
    {
        private IEnumerable<string> schemaTypes;

        /// <inheritdoc/>
        public override object GetDynamicParameters()
        {
            if (this.RuntimeParameters != null)
            {
                return this.RuntimeParameters;
            }

            base.GetDynamicParameters();

            this.RuntimeParameters[nameof(Template.Type)].Attributes.Add(new ValidateSetAttribute(this.GetSchemaTypes().ToArray()));

            return this.RuntimeParameters;
        }

        /// <inheritdoc />
        protected override void BeginProcessing()
        {
            this.RequiresAuthorization = false;
        }

        private IEnumerable<string> GetSchemaTypes()
        {
            if (this.schemaTypes != null)
            {
                return this.schemaTypes;
            }

            this.schemaTypes = ConfigurationManager.GetSchemaTypesAsync().Result.Result;
            return this.schemaTypes;
        }

        /// <inheritdoc />
        protected override void Process()
        {
            dynamic obj = new DynamicDictionary(this.RuntimeParameters);

            if (string.IsNullOrEmpty(obj.Type))
            {
                // Get the schema types.
                this.WriteVerbose("Obtaining document schema types.");
                this.WriteObject(this.GetSchemaTypes());

                if (!string.IsNullOrEmpty(obj.Destination))
                {
                    foreach (string schemaType in this.GetSchemaTypes())
                    {
                        this.EnsureDirectory(obj.Destination);
                        ConfigResponse<IEnumerable<SchemaVersion>> versions = ConfigurationManager.GetSchemaVersionsAsync(schemaType).Result;
                        foreach (SchemaVersion version in versions.Result)
                        {
                            ConfigResponse<Stream> schema = ConfigurationManager.GetSchemaAsync(schemaType, version.Version).Result;
                            string path = Path.Combine(obj.Destination, $"{schemaType.ToLowerInvariant()}_{version.Version}.xsd");
                            using (FileStream fileStream = File.Create(path))
                            {
                                schema.Result.CopyTo(fileStream);
                            }
                        }
                    }
                }
            }
            else if (obj.Version <= 0)
            {
                // Get the schema versions.
                this.WriteVerbose($"Obtaining document schema versions for type {obj.Type}.");

                ConfigResponse<IEnumerable<SchemaVersion>> schemaVersions = ConfigurationManager.GetSchemaVersionsAsync(obj.Type).Result;
                this.WriteObject(schemaVersions.Result);
            }
            else
            {
                this.WriteVerbose($"Obtaining document schema {this.GetParameter<string>("Type")} for version {obj.Version}.");

                ConfigResponse<Stream> schema = ConfigurationManager.GetSchemaAsync(obj.Type, obj.Version).Result;

                if (string.IsNullOrEmpty(obj.Destination))
                {
                    // The destination wasn't specified. Output the schema to stdout.
                    using (StreamReader sr = new StreamReader(schema.Result))
                    {
                        this.WriteObject(sr.ReadToEnd());
                    }
                }
                else
                {
                    // The destination exists. Save the file to the directory.
                    this.EnsureDirectory(obj.Destination);
                    string path = Path.Combine(obj.Destination, $"{obj.Type.ToLowerInvariant()}_{obj.Version}.xsd");
                    using (FileStream fileStream = File.Create(path))
                    {
                        schema.Result.CopyTo(fileStream);
                    }

                    this.WriteVerbose($"Schema saved as {path}");
                }
            }
        }

        /// <summary>
        /// Represents the properties for obtaining document schemas.
        /// </summary>
        public class Template
        {
            /// <summary>
            /// <para type="description">The document type to get.</para>
            /// </summary>
            [Parameter(HelpMessage = "The document type to get.", Position = 0, ValueFromPipeline = true)]
            public string Type { get; set; }

            /// <summary>
            /// <para type="description">The version of the schema to get.</para>
            /// </summary>
            [Parameter(Mandatory = false, HelpMessage = "The version of the schema to get.", Position = 1, ValueFromPipeline = true)]
            public int Version { get; set; }

            /// <summary>
            /// <para type="description">The directory to save the documents in.</para>
            /// </summary>
            [Parameter(Mandatory = false, HelpMessage = "The directory to save the documents in.", Position = 2, ValueFromPipeline = true)]
            public string Destination { get; set; }
        }
    }
}
