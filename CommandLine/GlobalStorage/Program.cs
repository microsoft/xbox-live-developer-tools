// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using CommandLine.Text;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xbox.Services.Tool;
using System.IO;
using System.Linq;

namespace GlobalStorage
{
    class Program
    {
        private enum BlobType
        {
            Config = TitleStorageBlobType.Config, 
            Json = TitleStorageBlobType.Json,
            Binary = TitleStorageBlobType.Binary
        }

        private class BaseOptions
        {
            [Option('c', "scid", Required = true,
                HelpText = "The service configuration ID (SCID) of the title")]
            public string ServiceConfigurationId { get; set; }

            [Option('s', "sandbox", Required = true,
                HelpText = "The target sandbox for title storage")]
            public string Sandbox { get; set; }
        }

        [Verb("quota", HelpText = "Get title global storage quota information.")]
        private class QuotaOptions: BaseOptions
        {
            [Usage(ApplicationAlias = "GlobalStorage")]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example("Get title global storage quota information.",
                        new QuotaOptions {ServiceConfigurationId = "xxx", Sandbox = "xxx"});
                }
            }
        }

        [Verb("list", HelpText =
            "Gets a list of blob metadata objects under a given path for the title global storage.")]
        private class BlobMetadataOptions : BaseOptions
        {
            [Option('p', "path", HelpText =
                "The root path to enumerate.  Results will be for blobs contained in this path and all subpaths.")]
            public string Path { get; set; }

            [Option('n', "max-items", HelpText = "The maximum number of items to return.")]
            public uint MaxItems { get; set; }

            [Option('k', "skip-items", HelpText = "The number of items to skip before returning results.")]
            public uint SkipItems { get; set; }

            [Usage(ApplicationAlias = "GlobalStorage")]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example(
                        "Gets a list of blob metadata objects under root for the title global storage.",
                        new BlobMetadataOptions { ServiceConfigurationId = "xxx", Sandbox = "xxx"});
                    yield return new Example("Gets a list of blob metadata objects under 'path', from position 0 to 9",
                        new BlobMetadataOptions
                        {
                            ServiceConfigurationId = "xxx",
                            Sandbox = "xxx",
                            Path = "path",
                            MaxItems = 10,
                            SkipItems = 0
                        });
                }
            }
        }

        private class BlobOptions : BaseOptions
        {
            [Option('p', "blob-path", Required = true, HelpText =
                @"The string that conforms to a path\file format on service, example: 'foo\bar\blob.txt'")]
            public string Path { get; set; }

            [Option('t', "type", Required = true, HelpText =
                @"Type of storage. Accept 'config', 'json' or 'binary'")]
            public BlobType Type { get; set; }
        }

        [Verb("delete", HelpText = "Deletes a blob from title storage.")]
        private class DeleteOptions : BlobOptions
        {
            [Usage(ApplicationAlias = "GlobalStorage")]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example(@"Deletes config blob 'foo\bar\blob.txt' from title storage.",
                        new DeleteOptions { ServiceConfigurationId = "xxx", Sandbox = "xxx", Path = @"foo\bar\blob.txt", Type = BlobType.Json});
                }
            }
        }

        [Verb("download", HelpText = "Downloads blob data from title storage.")]
        private class DownloadOptions : BlobOptions
        {
            [Option('o', "output", Required = true, HelpText = "The output file path to save as for downloading")]
            public string OutputFile { get; set; }

            [Option('f', "force-overwrite", HelpText = "Force overwrite if local file already exists")]
            public bool ForceOverwrite { get; set; }

            [Usage(ApplicationAlias = "GlobalStorage")]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example(@"Download config blob \text.txt and save as c:\text.txt",
                        new DownloadOptions { ServiceConfigurationId = "xxx", Sandbox = "xxx", OutputFile = @"c:\test.txt", Path = @"\text.txt", Type = BlobType.Json });
                }
            }
        }

        [Verb("upload", HelpText = "Uploads blob data to title storage.")]
        private class UploadOptions : BlobOptions
        {
            [Option('f', "file", Required = true, HelpText ="The local file to be uploaded")]
            public string File { get; set; }

            [Usage(ApplicationAlias = "GlobalStorage")]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example(@"Uploads file 'c:\test.txt' to title storage as '\text.txt'",
                        new UploadOptions { ServiceConfigurationId = "xxx", Sandbox = "xxx", File = @"c:\test.txt", Path = @"\text.txt", Type = BlobType.Json });
                }
            }
        }

        static async Task<int> Main(string[] args)
        {
            int exitCode = 0;
            string invokedVerb = string.Empty;
            BaseOptions baseOptions = null;
            try
            {
                // Only assign the option and verb here, as the commandlineParser doesn't support async callback yet.
                var result = Parser.Default.ParseArguments<QuotaOptions, BlobMetadataOptions, DeleteOptions, DownloadOptions, UploadOptions >(args)
                    .WithParsed(options =>
                    {
                        var verbAttribute =
                            Attribute.GetCustomAttribute(options.GetType(), typeof(VerbAttribute)) as VerbAttribute;
                        invokedVerb = verbAttribute?.Name;
                        baseOptions = options as BaseOptions;
                    })
                    .WithNotParsed(err => exitCode = -1);

                DevAccount account = Auth.LoadLastSignedInUser();
                if (account == null)
                {
                    Console.Error.WriteLine("Didn't found dev sign in info, please use \"XblDevAccount.exe signin\" to initiate.");
                    return -1;
                }

                Console.WriteLine($"Using Dev account {account.Name} from {account.AccountSource}");

                if (invokedVerb == "quota" && baseOptions is QuotaOptions quotaOptions)
                {
                    exitCode = await OnGetQuota(quotaOptions);
                }
                else if (invokedVerb == "list" && baseOptions is BlobMetadataOptions blobMetadataOptions)
                {
                    exitCode = await OnGetBlobMetadata(blobMetadataOptions);
                }
                else if (invokedVerb == "delete" && baseOptions is DeleteOptions deleteOptions)
                {
                    exitCode = await OnDelete(deleteOptions);
                }
                else if (invokedVerb == "download" && baseOptions is DownloadOptions downloadOptions)
                {
                    exitCode = await OnDownload(downloadOptions);
                }
                else if (invokedVerb == "upload" && baseOptions is UploadOptions uploadOptions)
                {
                    exitCode = await OnUpload(uploadOptions);
                }
                else
                {
                    Console.Error.WriteLine("Parsing parameters error.");
                    exitCode = -1;
                }
            }
            catch (XboxLiveException ex)
            {
                Console.WriteLine($"Error: GlobalStorage {invokedVerb} failed.");
                if (ex.Response != null)
                {
                    switch (ex.Response.StatusCode)
                    {
                        case HttpStatusCode.Unauthorized:
                            Console.WriteLine(
                                $"Unable to authorize the account with XboxLive service with scid : {baseOptions?.ServiceConfigurationId} and sandbox : {baseOptions?.Sandbox}, please contact your administrator.");
                            break;

                        case HttpStatusCode.Forbidden:
                            Console.WriteLine(
                                "Your account doesn't have access to perform the operation, please contact your administrator.");
                            break;

                        default:
                            Console.WriteLine(ex.Message);
                            break;
                    }
                }
                else
                {
                    Console.WriteLine(ex.Message);
                }
                return -1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: unexpected error found.");
                Console.Error.WriteLine(ex.Message);
                exitCode = -1;
            }

            return exitCode;
        }

        private static async Task<int> OnGetQuota(QuotaOptions options)
        {
            Console.WriteLine($"Getting global storage quota for scid : {options.ServiceConfigurationId}, sandbox {options.Sandbox}");
            GlobalStorageQuotaInfo result =
                await TitleStorage.GetGlobalStorageQuotaAsync(options.ServiceConfigurationId, options.Sandbox);
            Console.WriteLine($"Total bytes : {result.QuotaBytes}, used bytes {result.UsedBytes}");

            return 0;
        }

        private static async Task<int> OnGetBlobMetadata(BlobMetadataOptions options)
        {
            Console.WriteLine(
                $"Getting global storage blob list for scid : {options.ServiceConfigurationId}, sandbox {options.Sandbox}, path {options.Path}");
            TitleStorageBlobMetadataResult result = await TitleStorage.GetGlobalStorageBlobMetaData(
                options.ServiceConfigurationId, options.Sandbox, options.Path, options.MaxItems, options.SkipItems);

            Console.WriteLine(
                $"Total {result.TotalItems} items found, Displaying item {options.SkipItems} to {options.SkipItems + result.Items.Count()}");
            foreach (var metadata in result.Items)
            {
                Console.WriteLine($"\t{metadata.Path}, \t{metadata.Type}, \t{metadata.Size}");
            }

            return 0;
        }

        private static async Task<int> OnDelete(DeleteOptions options)
        {
            Console.WriteLine(
                $"Deleting global storage blob list for scid : {options.ServiceConfigurationId}, sandbox {options.Sandbox}, path {options.Path}, type {options.Type}");

            await TitleStorage.DeleteGlobalStorageBlob(options.ServiceConfigurationId, options.Sandbox,
                options.Path, (TitleStorageBlobType)options.Type);

            Console.WriteLine("Global storage blob deleted");

            return 0;
        }

        private static async Task<int> OnUpload(UploadOptions options)
        {
            Console.WriteLine(
                $"Uploading global storage blob list for scid : {options.ServiceConfigurationId}, sandbox {options.Sandbox}, path {options.Path}, type {options.Type}");

            byte[] blobData = File.ReadAllBytes(options.File);

            await TitleStorage.UploadGlobalStorageBlob(options.ServiceConfigurationId, options.Sandbox, options.Path,
                (TitleStorageBlobType) options.Type, blobData);

            Console.WriteLine("Global storage blob uploaded.");

            return 0;
        }

        private static async Task<int> OnDownload(DownloadOptions options)
        {
            //Check if file exist if no ForceOverWrite present. 
            if (!options.ForceOverwrite && File.Exists(options.OutputFile))
            {
                Console.Error.WriteLine($"OutFile {options.OutputFile} already exist, pass in ForceOverwrite if you would like to overwrite");
                return -1;
            }

            Console.WriteLine(
                $"Download global storage blob list for scid : {options.ServiceConfigurationId}, sandbox {options.Sandbox}, path {options.Path}, type {options.Type}");

            byte[] blobData = await TitleStorage.DownloadGlobalStorageBlob(
                options.ServiceConfigurationId, options.Sandbox, options.Path, (TitleStorageBlobType)options.Type);

            File.WriteAllBytes(options.OutputFile, blobData);

            Console.WriteLine($"Global storage blob saved as {options.OutputFile}.");

            return 0;
        }
    }
}
