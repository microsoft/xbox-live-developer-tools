// Copyright © Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Security;
    using System.Text;
    using Microsoft.Xbox.Services.DevTools.Authentication;
    using Microsoft.Xbox.Services.DevTools.XblConfig;

    /// <summary>
    /// Represents the base class for all powershell commandlets to use.
    /// </summary>
    public abstract class PSCmdletBase : PSCmdlet, IDynamicParameters
    {
        /// <summary>
        /// Gets or sets the dynamic runtime paramters.
        /// </summary>
        protected RuntimeDefinedParameterDictionary RuntimeParameters { get; set; }

        /// <summary>
        /// Gets or sets a boolean value indicating whether the service call requires authorization or not.
        /// </summary>
        protected bool RequiresAuthorization { get; set; } = true;

        /// <inheritdoc />
        protected override void BeginProcessing()
        {
            DevAccount account = ToolAuthentication.LoadLastSignedInUser();

            if (this.RequiresAuthorization)
            {
                if (account == null)
                {
                    throw new Exception("Didn't find dev signin info, please use \"Connect-DevAccount\" to initiate.");
                }

                if (account.AccountSource != DevAccountSource.WindowsDevCenter)
                {
                    throw new Exception("You must sign in with a valid Windows Dev Center account.");
                }
            }

            PropertyInfo accountIdProperty = this.GetType().GetProperty("AccountId");
            if (accountIdProperty != null && account != null)
            {
                Guid accountIdPropertyValue = (Guid)accountIdProperty.GetValue(this);
                if (accountIdPropertyValue == Guid.Empty)
                {
                    accountIdProperty.SetValue(this, new Guid(account.AccountId));
                }
            }
        }

        /// <inheritdoc/>
        protected sealed override void ProcessRecord()
        {
            try
            {
                this.Process();
            }
            catch (AggregateException aex)
            {
                aex.Handle((ex) =>
                {
                    if (ex is HttpRequestException)
                    {
                        if (ex.Message.Contains(Convert.ToString((int)HttpStatusCode.Unauthorized)))
                        {
                            throw new SecurityException("Unable to authorize this account with Xbox Live. Please check your account.");
                        }
                    }

                    throw aex.Flatten().InnerException;
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// When inherited by a class, this processes the request.
        /// </summary>
        protected abstract void Process();

        /// <summary>
        /// Ensures that the file system directory exists.
        /// </summary>
        /// <param name="path">The path of the directory to check.</param>
        protected void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Expands a list of file paths which may contain wildcards into a list of files.
        /// </summary>
        protected IEnumerable<string> Glob(IEnumerable<string> files)
        {
            List<string> fileList = new List<string>();
            foreach (string file in files)
            {
                if (File.Exists(file))
                {
                    fileList.Add(file);
                    continue;
                }

                string pathRoot = Path.GetDirectoryName(file);
                if (string.IsNullOrEmpty(pathRoot) || pathRoot == ".")
                {
                    pathRoot = this.SessionState.Path.CurrentFileSystemLocation.Path;
                }

                string filename = Path.GetFileName(file);
                fileList.AddRange(Directory.GetFiles(pathRoot, filename));
            }

            return fileList;
        }

        /// <summary>
        /// Gets an ETag for a given sandbox.
        /// </summary>
        protected string GetETag(string directory, string sandbox)
        {
            string filename = Path.Combine(directory, $"{sandbox ?? "Account"}.etag");
            if (!File.Exists(filename))
            {
                return null;
            }

            using (StreamReader file = File.OpenText(filename))
            {
                return file.ReadToEnd();
            }
        }

        /// <summary>
        /// Gets an ETag for a given sandbox.
        /// </summary>
        protected string GetETag(IEnumerable<string> files, string sandbox)
        {
            foreach (string file in files)
            {
                string eTag = this.GetETag(Path.GetDirectoryName(file), sandbox ?? "Account");
                if (eTag != null)
                {
                    return eTag;
                }
            }

            return null;
        }

        /// <summary>
        /// Saves an ETag for a given sandbox.
        /// </summary>
        protected void SaveETag(string etag, string directory, string sandbox)
        {
            // Save the etag as a hidden file in the directory.
            string filename = Path.Combine(directory, $"{sandbox ?? "Account"}.etag");
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            using (StreamWriter sw = new StreamWriter(new FileStream(filename, FileMode.Create, FileAccess.Write), Encoding.UTF8))
            {
                sw.Write(etag);
            }

            new FileInfo(filename)
            {
                Attributes = FileAttributes.Hidden
            };
        }

        /// <summary>
        /// Prints the validation info object to the host.
        /// </summary>
        /// <param name="validationList">The list of ValidationInfo objects to print.</param>
        protected void PrintValidationInfo(IEnumerable<ValidationInfo> validationList)
        {
            using (PowerShell powershell = PowerShell.Create())
            {
                foreach (ValidationInfo validationInfo in validationList.Where(c => c.Severity == Severity.Warning))
                {
                    Console.WriteLine(validationInfo.Message);
                    if (powershell.Streams.Warning.IsOpen)
                    {
                        powershell.Streams.Warning.Add(new WarningRecord(validationInfo.DocumentName, validationInfo.Message));
                    }
                }

                foreach (ValidationInfo validationInfo in validationList.Where(c => c.Severity == Severity.Error))
                {
                    Console.WriteLine(validationInfo.Error);
                    if (powershell.Streams.Error.IsOpen)
                    {
                        powershell.Streams.Error.Add(new ErrorRecord(new Exception(validationInfo.Error), validationInfo.DocumentName, ErrorCategory.WriteError, null));
                    }
                }
            }
        }

        /// <inheritdoc />
        public virtual object GetDynamicParameters()
        {
            return this.RuntimeParameters;
        }

        /// <summary>
        /// Gets the value of the runtime parameter.
        /// </summary>
        /// <typeparam name="T">The type of the parameter to cast as.</typeparam>
        /// <param name="name">The name of the parameter.</param>
        protected T GetParameter<T>(string name)
        {
            if (!this.RuntimeParameters.ContainsKey(name))
            {
                throw new ArgumentException($"Cannot find the runtime parameter with key {name}", nameof(name));
            }

            return (T)this.RuntimeParameters[name].Value;
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    /// <summary>
    /// Represents the base class for all powershell commandlets to use.
    /// </summary>
    /// <typeparam name="T">The template to use. This will be used with dynamic properties.</typeparam>
    public abstract class PSCmdletBase<T> : PSCmdletBase
    {
        /// <inheritdoc />
        public override object GetDynamicParameters()
        {
            this.RuntimeParameters = new RuntimeDefinedParameterDictionary();

            foreach (var property in typeof(T).GetProperties())
            {
                Collection<Attribute> attributes = new Collection<Attribute>();
                foreach (var attribute in property.GetCustomAttributes())
                {
                    attributes.Add(attribute);
                }

                this.RuntimeParameters.Add(property.Name, new RuntimeDefinedParameter(property.Name, property.PropertyType, attributes));
            }

            return this.RuntimeParameters;
        }
    }
#pragma warning restore SA1402 // File may only contain a single class
}
