// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblConfig
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Security;
    using System.Text;
    using System.Threading.Tasks;
    using CommandLine;
    using Microsoft.Xbox.Services.DevTools.Authentication;
    using Microsoft.Xbox.Services.DevTools.XblConfig;

    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            int exitCode = 0;
            DevAccount account = ToolAuthentication.LoadLastSignedInUser();
            if (account == null)
            {
                Console.Error.WriteLine("Didn't find dev signin info, please use \"XblDevAccount.exe signin\" to initiate.");
                return -1;
            }

            if (account.AccountSource != DevAccountSource.WindowsDevCenter)
            {
                Console.Error.WriteLine("You must sign in with a valid Windows Dev Center account.");
            }

            string invokedVerb = null;
            object opts = null;

            Console.WriteLine($"Using Dev account {account.Name} from {account.AccountSource}");
            try
            {
                // Find all of the subclasses which inherit from BaseOptions.
                Type[] argumentTypes = typeof(Program).GetNestedTypes(BindingFlags.NonPublic).Where(c => c.IsSubclassOf(typeof(BaseOptions))).ToArray();

                // Only assign the option and verb here, as the commandlineParser doesn't support async callback yet.
                Parser.Default.ParseArguments(args, argumentTypes)
                    .WithParsed(options =>
                    {
                        VerbAttribute verbAttribute = Attribute.GetCustomAttribute(options.GetType(), typeof(VerbAttribute)) as VerbAttribute;
                        invokedVerb = verbAttribute?.Name;
                        opts = options;
                    })
                    .WithNotParsed(err => exitCode = -1);

                // Find property called AccountId. If it not set, then set it to the logged in user's account Id.
                PropertyInfo accountIdProperty = opts.GetType().GetProperty("AccountId");
                Guid accountIdPropertyValue = (Guid)accountIdProperty.GetValue(opts);
                if (accountIdPropertyValue == Guid.Empty)
                {
                    accountIdProperty.SetValue(opts, new Guid(account.AccountId));
                }

                // Find the method which takes this class as an argument.
                MethodInfo method = typeof(Program).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(c => c.GetParameters().FirstOrDefault()?.ParameterType == opts.GetType()).FirstOrDefault();
                if (method == null)
                {
                    // This should never happen, but just in case...
                    throw new InvalidOperationException($"Method with parameter {opts.GetType()} not found.");
                }

                Task<int> methodResult = (Task<int>)method.Invoke(null, new object[] { opts });
                exitCode = await methodResult;
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"Error: XblConfig {invokedVerb} failed.");

                if (ex.Message.Contains(Convert.ToString((int)HttpStatusCode.Unauthorized)))
                {
                    Console.Error.WriteLine(
                        $"Unable to authorize the account with the XboxLive service, please contact your administrator.");
                }
                else if (ex.Message.Contains(Convert.ToString((int)HttpStatusCode.Forbidden)))
                {
                    Console.Error.WriteLine(
                        "Your account doesn't have access to perform the operation, please contact your administrator.");
                }
                else
                {
                    Console.WriteLine(ex.Message);
                }

                exitCode = -1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: unexpected error found.");
                Console.Error.WriteLine(ex.Message);
                exitCode = -1;
            }

            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.Read();
            }

            return exitCode;
        }

        private static async Task<int> GetDocumentsAsync(GetDocumentsOptions options)
        {
            if (options.DocumentType == DocumentType.Sandbox && string.IsNullOrEmpty(options.Sandbox))
            {
                throw new ArgumentException("Sandbox must be specified when obtaining sandbox documents.");
            }

            if (options.DocumentType == DocumentType.Account)
            {
                options.Sandbox = null;
            }

            EnsureDirectory(options.Destination);

            Task<DocumentsResponse> documentsTask;
            if (options.DocumentType == DocumentType.Sandbox)
            {
                Console.WriteLine("Obtaining sandbox documents.");
                documentsTask = ConfigurationManager.GetSandboxDocumentsAsync(options.Scid, options.Sandbox);
            }
            else
            {
                Console.WriteLine("Obtaining account documents.");
                if (options.AccountId == Guid.Empty)
                {
                    DevAccount user = ToolAuthentication.LoadLastSignedInUser();
                    options.AccountId = new Guid(user.AccountId);
                }

                documentsTask = ConfigurationManager.GetAccountDocumentsAsync(options.AccountId);
            }

            using (DocumentsResponse documents = await documentsTask)
            {
                Console.WriteLine($"ETag: {documents.ETag}");
                Console.WriteLine($"Version: {documents.Version}");
                Console.WriteLine("Files: ");

                foreach (var file in documents.Documents)
                {
                    var path = Path.Combine(options.Destination, file.Name);
                    using (var fileStream = File.Create(path))
                    {
                        await file.Stream.CopyToAsync(fileStream);
                    }

                    Console.WriteLine($" - {file.Name}");
                }

                SaveETag(documents.ETag, options.Destination, options.Sandbox);
                Console.WriteLine($"Saved {documents.Documents.Count()} files to {options.Destination}.");
            }

            return 0;
        }

        private static async Task<int> CommitDocumentsAsync(CommitDocumentsOptions options)
        {
            if (options.DocumentType == DocumentType.Sandbox && string.IsNullOrEmpty(options.Sandbox))
            {
                throw new ArgumentException("Sandbox must be specified when committing sandbox documents.");
            }

            if (options.DocumentType == DocumentType.Account)
            {
                options.Sandbox = null;
            }

            var files = Glob(options.Files);
            int fileCount = files.Count();
            if (fileCount == 0)
            {
                Console.Error.WriteLine("There are no files selected to commit.");
                return -1;
            }

            Console.WriteLine($"Committing {fileCount} file(s) to Xbox Live.");

            var eTag = options.ETag ?? GetETag(files, options.Sandbox);
            if (options.Force)
            {
                eTag = null;
            }

            Task<ConfigResponse<ValidationResponse>> documentsTask;
            if (options.DocumentType == DocumentType.Sandbox)
            {
                Console.WriteLine("Committing sandbox documents.");
                documentsTask = ConfigurationManager.CommitSandboxDocumentsAsync(files, options.Scid, options.Sandbox, eTag, options.ValidateOnly, options.Message);
            }
            else
            {
                Console.WriteLine("Committing account documents.");
                if (options.AccountId == Guid.Empty)
                {
                    DevAccount user = ToolAuthentication.LoadLastSignedInUser();
                    options.AccountId = new Guid(user.AccountId);
                }

                documentsTask = ConfigurationManager.CommitAccountDocumentsAsync(files, options.AccountId, eTag, options.ValidateOnly, options.Message);
            }

            var result = await documentsTask;

            SaveETag(result.Result.ETag, Path.GetDirectoryName(files.First()), options.Sandbox);

            Console.WriteLine($"Can Commit: {result.Result.CanCommit}");
            Console.WriteLine($"Committed:  {result.Result.Committed}");

            PrintValidationInfo(result.Result.ValidationInfo);

            return 0;
        }

        private static async Task<int> GetSchemasAsync(GetSchemasOptions options)
        {
            if (string.IsNullOrEmpty(options.Type))
            {
                // Get the schema types.
                Console.WriteLine("Obtaining document schema types.");
                var schemaTypes = await ConfigurationManager.GetSchemaTypesAsync();
                foreach (var schemaType in schemaTypes.Result)
                {
                    Console.WriteLine($" - {schemaType}");
                    if (!string.IsNullOrEmpty(options.Destination))
                    {
                        EnsureDirectory(options.Destination);
                        var versions = await ConfigurationManager.GetSchemaVersionsAsync(schemaType);
                        foreach (var version in versions.Result)
                        {
                            var schema = await ConfigurationManager.GetSchemaAsync(schemaType, version.Version);
                            var path = Path.Combine(options.Destination, $"{schemaType.ToLowerInvariant()}_{version.Version}.xsd");
                            using (var fileStream = File.Create(path))
                            {
                                await schema.Result.CopyToAsync(fileStream);
                            }
                        }
                    }
                }
            }
            else if (options.Version <= 0)
            {
                // Get the schema versions.
                Console.WriteLine($"Obtaining document schema versions for type {options.Type}.");
                var schemaVersions = await ConfigurationManager.GetSchemaVersionsAsync(options.Type);
                foreach (var schemaVersion in schemaVersions.Result)
                {
                    Console.WriteLine($"{schemaVersion.Version} - {schemaVersion.Namespace}");
                }
            }
            else
            {
                Console.WriteLine($"Obtaining document schema {options.Type} for version {options.Version}.");
                var schema = await ConfigurationManager.GetSchemaAsync(options.Type, options.Version);

                if (string.IsNullOrEmpty(options.Destination))
                {
                    // The destination wasn't specified. Output the schema to stdout.
                    await schema.Result.CopyToAsync(Console.OpenStandardOutput());
                }
                else
                {
                    // The destination exists. Save the file to the directory.
                    EnsureDirectory(options.Destination);
                    var path = Path.Combine(options.Destination, $"{options.Type.ToLowerInvariant()}_{options.Version}.xsd");
                    using (var fileStream = File.Create(path))
                    {
                        await schema.Result.CopyToAsync(fileStream);
                    }

                    Console.WriteLine($"Schema saved as {path}");
                }
            }

            return 0;
        }

        private static async Task<int> GetProductsAsync(GetProductsOptions options)
        {
            Console.WriteLine("Obtaining products.");

            var response = await ConfigurationManager.GetProductsAsync(options.AccountId);

            // Determine the max length of the pfnId
            var longestProduct = response.Result.Aggregate((max, cur) => max.PfnId.Length > cur.PfnId.Length ? max : cur);
            string format = $"{{0, -36}}  {{1, -{longestProduct.PfnId.Length}}}  {{2, -10}}  {{3}}";
            Console.WriteLine(format, "Product ID", "Package Family Name", "Title ID", "Tier");

            foreach (var product in response.Result)
            {
                Console.WriteLine(format, product.ProductId, product.PfnId, product.TitleId, product.XboxLiveTier);
            }

            return 0;
        }

        private static async Task<int> GetProductAsync(GetProductOptions options)
        {
            Console.WriteLine("Obtaining product.");

            var response = await ConfigurationManager.GetProductAsync(options.ProductId);
            Console.WriteLine(response.Result);
            
            return 0;
        }

        private static async Task<int> GetRelyingPartiesAsync(GetRelyingPartiesOptions options)
        {
            Console.WriteLine("Obtaining relying parties.");

            var response = await ConfigurationManager.GetRelyingPartiesAsync(options.AccountId);
            var longestRelyingParty = response.Result.Aggregate((max, cur) => max.Name.Length > cur.Name.Length ? max : cur);
            string format = $"{{0, -{longestRelyingParty.Name.Length}}}  {{1}}";
            Console.WriteLine(format, "Name", "Filename");
            foreach (var relyingParty in response.Result)
            {
                Console.WriteLine(format, relyingParty.Name, relyingParty.Filename);
            }

            return 0;
        }

        private static async Task<int> GetRelyingPartyDocumentAsync(GetRelyingPartyDocumentOptions options)
        {
            Console.WriteLine("Obtaining relying party document.");

            var response = await ConfigurationManager.GetRelyingPartyDocumentAsync(options.AccountId, options.Filename);
            var document = response.Documents.First();
            using (Stream stream = document.Stream)
            {
                if (string.IsNullOrEmpty(options.Destination))
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        Console.WriteLine(sr.ReadToEnd());
                    }
                }
                else
                {
                    EnsureDirectory(options.Destination);
                    var path = Path.Combine(options.Destination, document.Name);
                    using (StreamWriter sw = File.CreateText(path))
                    {
                        await stream.CopyToAsync(sw.BaseStream);
                    }

                    Console.WriteLine($"Document saved as {path}");
                }                
            }

            return 0;
        }

        private static async Task<int> GetWebServicesAsync(GetWebServicesOptions options)
        {
            Console.WriteLine("Obtaining web services.");

            var response = await ConfigurationManager.GetWebServicesAsync(options.AccountId);
            var longestWebService = response.Result.Aggregate((max, cur) => max.Name.Length > cur.Name.Length ? max : cur);
            string format = $"{{0, -{longestWebService.Name.Length}}}  {{1, -36}}  {{2}}";
            Console.WriteLine(format, "Name", "Service ID", "Telemetry Access");
            foreach (var webService in response.Result)
            {
                Console.WriteLine(format, webService.Name, webService.AccountId, webService.TelemetryAccess);
            }

            return 0;
        }

        private static async Task<int> CreateWebServiceAsync(CreateWebServiceOptions options)
        {
            Console.WriteLine("Creating web service.");

            var response = await ConfigurationManager.CreateWebServiceAsync(options.AccountId, options.Name, options.TelemetryAccess, options.AppChannelsAccess);

            Console.WriteLine($"Web service created with ID {response.Result.ServiceId}");

            return 0;
        }

        private static async Task<int> UpdateWebServiceAsync(UpdateWebServiceOptions options)
        {
            Console.WriteLine("Updating web service.");

            var response = await ConfigurationManager.UpdateWebServiceAsync(options.ServiceId, options.AccountId, options.Name, options.TelemetryAccess, options.AppChannelsAccess);

            Console.WriteLine($"Web service with ID {response.Result.ServiceId} successfully updated.");

            return 0;
        }

        private static async Task<int> DeleteWebServiceAsync(DeleteWebServiceOptions options)
        {
            Console.WriteLine("Deleting web service.");

            var response = await ConfigurationManager.DeleteWebServiceAsync(options.AccountId, options.ServiceId);

            Console.WriteLine($"Web service with ID {options.ServiceId} successefully deleted.");

            return 0;
        }

        private static async Task<int> GenerateWebServiceCertificateAsync(GenerateWebServiceCertificateOptions options)
        {
            FileInfo fi = new FileInfo(options.Destination);
            EnsureDirectory(fi.Directory.FullName);
            Console.WriteLine("Generating web service certificate.");
            Console.Write("Please enter the password you would like to secure this certificate with: ");
            using (SecureString password = GetPassword())
            {
                ConfigResponse<Stream> response = await ConfigurationManager.GenerateWebServiceCertificateAsync(options.AccountId, options.ServiceId, password);
                using (var fileStream = File.Create(options.Destination))
                {
                    response.Result.CopyTo(fileStream);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Certificate generated.");
            return 0;
        }

        private static async Task<int> PublishAsync(PublishOptions options)
        {
            Console.WriteLine(options.ValidateOnly ? "Validating." : "Publishing.");
            ConfigResponse<PublishResponse> response;
            if (options.ConfigSetVersion.HasValue)
            {
                response = await ConfigurationManager.PublishAsync(options.Scid, options.SourceSandbox, options.DestinationSandbox, options.ValidateOnly, options.ConfigSetVersion.Value);
            }
            else
            {
                response = await ConfigurationManager.PublishAsync(options.Scid, options.SourceSandbox, options.DestinationSandbox, options.ValidateOnly);
            }
            
            PrintValidationInfo(response.Result.ValidationResult);
            Console.WriteLine($"Status: {response.Result.Status}");
            if (!string.IsNullOrEmpty(response.Result.StatusMessage))
            {
                Console.WriteLine($"Status Message: {response.Result.StatusMessage}");
            }

            return 0;
        }

        private static async Task<int> GetPublishStatusAsync(PublishStatusOptions options)
        {
            Console.WriteLine("Getting publish status.");
            var response = await ConfigurationManager.GetPublishStatusAsync(options.Scid, options.Sandbox);
            Console.WriteLine($"Status: {response.Result.Status}");
            if (!string.IsNullOrEmpty(response.Result.StatusMessage))
            {
                Console.WriteLine($"Status Message: {response.Result.StatusMessage}");
            }

            return 1;
        }

        private static async Task<int> UploadAchievementImageAsync(UploadAchievementImageOptions options)
        {
            Console.WriteLine("Uploading achievement image.");
            using (FileStream stream = File.OpenRead(options.Filename))
            {
                var response = await ConfigurationManager.UploadAchievementImageAsync(options.Scid, Path.GetFileName(stream.Name), stream);
                Console.WriteLine($"Image uploaded to: {response.Result.CdnUrl}");
            }
                
            return 0;
        }

        private static async Task<int> GetAchievementImageAsync(GetAchievementImageOptions options)
        {
            Console.WriteLine("Getting achievement image.");
            var response = await ConfigurationManager.GetAchievementImageAsync(options.Scid, options.AssetId);
            Console.WriteLine(response.Result.CdnUrl);
            return 0;
        }

        private static async Task<int> GetSandboxesAsync(GetSandboxOptions options)
        {
            Console.WriteLine("Getting list of sandboxes.");
            var response = await ConfigurationManager.GetSandboxesAsync(options.AccountId);
            foreach (var sandbox in response.Result)
            {
                Console.WriteLine(sandbox);
            }

            return 0;
        }

        #region Helper Functions

        private static SecureString GetPassword()
        {
            var pwd = new SecureString();
            while (true)
            {
                ConsoleKeyInfo i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    if (pwd.Length > 0)
                    {
                        break;
                    }                    
                }
                else if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd.RemoveAt(pwd.Length - 1);
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    pwd.AppendChar(i.KeyChar);
                    Console.Write("*");
                }
            }

            return pwd;
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static IEnumerable<string> Glob(IEnumerable<string> files)
        {
            List<string> fileList = new List<string>();
            foreach (var file in files)
            {
                if (File.Exists(file))
                {
                    fileList.Add(file);
                    continue;
                }

                var pathRoot = Path.GetDirectoryName(file);
                if (string.IsNullOrEmpty(pathRoot))
                {
                    pathRoot = Directory.GetCurrentDirectory();
                }

                var filename = Path.GetFileName(file);
                fileList.AddRange(Directory.GetFiles(pathRoot, filename));
            }

            return fileList;
        }

        private static string GetETag(string directory, string sandbox)
        {
            string filename = Path.Combine(directory, $"{sandbox ?? "Account"}.etag");
            if (!File.Exists(filename))
            {
                return null;
            }

            using (var file = File.OpenText(filename))
            {
                return file.ReadToEnd();
            }
        }

        private static string GetETag(IEnumerable<string> files, string sandbox)
        {
            foreach (var file in files)
            {
                var eTag = GetETag(Path.GetDirectoryName(file), sandbox ?? "Account");
                if (eTag != null)
                {
                    return eTag;
                }
            }

            return null;
        }

        private static void SaveETag(string etag, string directory, string sandbox)
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

        private static void PrintValidationInfo(IEnumerable<ValidationInfo> validationList)
        {
            var warnings = validationList.Where(c => c.Severity == Severity.Warning);
            var errors = validationList.Where(c => c.Severity == Severity.Error);
            if (warnings.Count() > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Warnings:");
                foreach (var validationInfo in warnings)
                {
                    Console.WriteLine($" - {validationInfo.Message}");
                }

                Console.WriteLine();
                Console.ResetColor();
            }

            if (errors.Count() > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Errors:");
                foreach (var validationInfo in errors)
                {
                    Console.Error.WriteLine($" - {validationInfo.Message}");
                }

                Console.WriteLine();
                Console.ResetColor();
            }
        }

        #endregion

        #region Model Classes

        private class BaseOptions
        {
            [Option("verbose")]
            public bool Verbose { get; set; }
        }

        [Verb("get-documents", HelpText = "Gets documents.")]
        private class GetDocumentsOptions : BaseOptions
        {
            [Option('c', "scid", Required = false, HelpText = "The service configuration ID.")]
            public Guid Scid { get; set; }

            [Option('a', "accountId", Required = false, HelpText = "The account ID of the user.")]
            public Guid AccountId { get; set; }

            [Option('s', "sandbox", Required = false, HelpText = "The sandbox of the product.")]
            public string Sandbox { get; set; }

            [Option('d', "destination", Required = true, HelpText = "The directory to save the documents in.")]
            public string Destination { get; set; }

            [Option('t', "type", Required = false, Default = DocumentType.Sandbox, HelpText = "The type of documents to obtain. Can be either 'Sandbox' or 'Account'.")]
            public DocumentType DocumentType { get; set; }

            [Option('v', "view", Required = false, Default = View.Working, HelpText = "The view to obtain.")]
            public View View { get; set; }

            [Option('o', "configset", Required = false, HelpText = "The config set version to obtain.")]
            public ulong? ConfigSetVersion { get; set; }
        }

        [Verb("commit", HelpText = "Commits documents back to Xbox Live for the given sandbox.")]
        private class CommitDocumentsOptions : BaseOptions
        {
            [Option('c', "scid", Required = false, HelpText = "The service configuration ID.")]
            public Guid Scid { get; set; }

            [Option('a', "accountId", Required = false, HelpText = "The account ID of the user.")]
            public Guid AccountId { get; set; }

            [Option('s', "sandbox", Required = false, HelpText = "The sandbox of the product.")]
            public string Sandbox { get; set; }

            [Option('f', "files", Required = true, HelpText = "The files to commit.")]
            public IEnumerable<string> Files { get; set; }

            [Option('v', "validateOnly", Required = false, Default = false, HelpText = "Pass 'true' to only attempt the commit, 'false' to actually commit it. Defaults to 'true'.")]
            public bool ValidateOnly { get; set; }

            [Option('e', "eTag", Required = false, HelpText = "The ETag of the changeset that was previously committed. If this is unknown, the ETag stored by this tool previously will be used.")]
            public string ETag { get; set; }

            [Option("force", Default = false, HelpText = "Setting this will override the ETag requirement and force commit the documents regardless of concurrency violations.")]
            public bool Force { get; set; }

            [Option('t', "type", Required = false, Default = DocumentType.Sandbox, HelpText = "The type of documents to obtain.")]
            public DocumentType DocumentType { get; set; }

            [Option('m', "message", Required = false, HelpText = "The commit message to save with.")]
            public string Message { get; set; }
        }

        [Verb("get-schemas", HelpText = "Gets document schemas.")]
        private class GetSchemasOptions : BaseOptions
        {
            [Option('t', "type", Required = false, HelpText = "The document type to get.")]
            public string Type { get; set; }

            [Option('v', "version", Required = false, HelpText = "The document version.")]
            public int Version { get; set; }

            [Option('d', "destination", Required = false, HelpText = "The directory to save the documents in.")]
            public string Destination { get; set; }
        }

        [Verb("get-products", HelpText = "Gets products for given account.")]
        private class GetProductsOptions : BaseOptions
        {
            [Option('a', "accountId", Required = false, HelpText = "The account ID associated with the products to get.")]
            public Guid AccountId { get; set; }
        }

        [Verb("get-product", HelpText = "Gets a product.")]
        private class GetProductOptions : BaseOptions
        {
            [Option('p', "productId", Required = false, HelpText = "The product ID of the product to get.")]
            public Guid ProductId { get; set; }
        }

        [Verb("getRelyingParties", HelpText = "Gets relying parties.")]
        private class GetRelyingPartiesOptions : BaseOptions
        {
            [Option('a', "accountId", Required = false, HelpText = "The account ID that owns the relying parties.")]
            public Guid AccountId { get; set; }
        }

        [Verb("get-relying-party-document", HelpText = "Gets a specific relying party document for a given account.")]
        private class GetRelyingPartyDocumentOptions : GetRelyingPartiesOptions
        {
            [Option('f', "filename", Required = true, HelpText = "The filename of the document to retrieve.")]
            public string Filename { get; set; }

            [Option('d', "destination", Required = false, HelpText = "The directory to save the document in.")]
            public string Destination { get; set; }
        }

        [Verb("get-web-services", HelpText = "Gets a list of web services for a given account.")]
        private class GetWebServicesOptions : BaseOptions
        {
            [Option('a', "accountId", Required= false, HelpText = "The account ID that owns the web services.")]
            public Guid AccountId { get; set; }
        }

        [Verb("create-web-service", HelpText = "Create a new web service.")]
        private class CreateWebServiceOptions : BaseOptions
        {
            [Option('a', "accountId", Required = false, HelpText = "The account ID that owns the web service.")]
            public Guid AccountId { get; set; }

            [Option('n', "name", Required = true, HelpText = "The name to give the web service.")]
            public string Name { get; set; }
            
            [Option('t', "telemetryAccess", Required = false, HelpText = "A boolean value allowing your service to retrieve game telemetry data for any of your games.")]
            public bool TelemetryAccess { get; set; }

            [Option('c', "appChannelAccess", Required = false, HelpText = "A boolean value that gives the media provider owning the service the authority to programmatically publish app channels for consumption on console through the OneGuide twist.")]
            public bool AppChannelsAccess { get; set; }
        }

        [Verb("update-web-service", HelpText = "Update a web service.")]
        private class UpdateWebServiceOptions : CreateWebServiceOptions
        {
            [Option('s', "serviceId", Required = true, HelpText = "The ID of the web service.")]
            public Guid ServiceId { get; set; }
        }

        [Verb("delete-web-service", HelpText = "Delete a web service.")]
        private class DeleteWebServiceOptions : BaseOptions
        {
            [Option('a', "accountId", Required = false, HelpText = "The account ID that owns the web service.")]
            public Guid AccountId { get; set; }

            [Option('s', "serviceId", Required = true, HelpText = "The ID of the web service.")]
            public Guid ServiceId { get; set; }
        }

        [Verb("generate-web-service-cert", HelpText = "Generates a web service certificate.")]
        private class GenerateWebServiceCertificateOptions : BaseOptions
        {
            [Option('a', "accountId", Required = false, HelpText = "The account ID that owns the web service.")]
            public Guid AccountId { get; set; }

            [Option('s', "serviceId", Required = true, HelpText = "The ID of the web service.")]
            public Guid ServiceId { get; set; }

            [Option('d', "destination", Required = false, HelpText = "The location to save the certificate to.")]
            public string Destination { get; set; }
        }

        [Verb("publish", HelpText = "Publishes a set of documents for use by Xbox Live services.")]
        private class PublishOptions : BaseOptions
        {
            [Option('s', "scid", Required = true, HelpText = "The Service configuration ID.")]
            public Guid Scid { get; set; }

            [Option('f', "from", Required = true, HelpText = "The sandbox to publish from.")]
            public string SourceSandbox { get; set; }

            [Option('t', "to", Required = true, HelpText = "The sandbox to publish to.")]
            public string DestinationSandbox { get; set; }

            [Option('v', "validateOnly", Required = false, HelpText = "A boolean valid indicating whether to perform a validate-only publish.")]
            public bool ValidateOnly { get; set; }

            [Option('o', "configset", Required = false, HelpText = "The config set version to publish.")]
            public ulong? ConfigSetVersion { get; set; }
        }

        [Verb("get-publish-status", HelpText = "Gets the publish status.")]
        private class PublishStatusOptions : BaseOptions
        {
            [Option('s', "scid", Required = true, HelpText = "The service configuration ID.")]
            public Guid Scid { get; set; }

            [Option('d', "sandbox", Required = true, HelpText = "The sandbox being published to.")]
            public string Sandbox { get; set; }
        }

        [Verb("get-achievement-image", HelpText = "Gets the details of an achievement image by its asset ID.")]
        private class GetAchievementImageOptions : BaseOptions
        {
            [Option('s', "scid", Required = true, HelpText = "The service configuration ID.")]
            public Guid Scid { get; set; }

            [Option('a', "assetId", Required = true, HelpText = "The ID of the image.")]
            public Guid AssetId { get; set; }
        }

        [Verb("upload-achievement-image", HelpText = "Uploads an achievement image to a specific SCID.")]
        private class UploadAchievementImageOptions : BaseOptions
        {
            [Option('s', "scid", Required = true, HelpText = "The service configuration ID.")]
            public Guid Scid { get; set; }

            [Option('f', "file", Required = true, HelpText = "The file to upload.")]
            public string Filename { get; set; }
        }

        [Verb("get-sandboxes", HelpText = "Gets a list of a sandboxes for a given account.")]
        private class GetSandboxOptions : BaseOptions
        {
            [Option('a', "accountId", Required = false, HelpText = "The account ID associated with the list of sandboxes.")]
            public Guid AccountId { get; set; }
        }

        #endregion
    }
}
