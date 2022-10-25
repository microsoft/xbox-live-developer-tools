// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblPlayerDataReset
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using CommandLine;
    using CommandLine.Text;
    using Microsoft.Xbox.Services.DevTools.Authentication;
    using Microsoft.Xbox.Services.DevTools.PlayerReset;

    internal class Program
    {
        internal const int MaxBatchSize = 10;
        internal const string DefaultDelimitter = ",";
        internal const string PartnerCenterDelim = "PC";
        internal const string PartnerEmaillDelim = "PCE";
        internal const string PartnerXuidDelim = "PCX";

        private static async Task<int> Main(string[] args)
        {
            try
            {
                ResetOptions options = null;
                var parserResult = Parser.Default.ParseArguments<ResetOptions>(args)
                    .WithParsed<ResetOptions>(parsedOptions => options = parsedOptions);
                if (parserResult.Tag == ParserResultType.NotParsed)
                {
                    return -1;
                }

                return await OnReset(options);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: player data reset failed:");
                Console.WriteLine(ex.Message);
                return -1;
            }
        }

        private static void PrintProviderDetails(List<JobProviderStatus> providers)
        {
            foreach (var provider in providers)
            {
                if (provider.Status == ResetProviderStatus.CompletedSuccess)
                {
                    Console.WriteLine($"\t{provider.Provider}, Status: {provider.Status} " +
                                      (provider.Status == ResetProviderStatus.CompletedSuccess? $"Error: {provider.ErrorMessage}" : string.Empty));
                }
            }
        }

        private static async Task<int> OnReset(ResetOptions options)
        {
            int result = 0;

            if (options == null)
            {
                Console.WriteLine("Unknown parameter error");
                return -1;
            }

            Console.WriteLine($"Resetting player data for SCID {options.ServiceConfigurationId} in sandbox {options.Sandbox}");

            if (!string.IsNullOrEmpty(options.TestAccount))
            {
                string delimitter = options.Delimitter == PartnerCenterDelim ? PartnerEmaillDelim : options.Delimitter;
                List<string> testAccountNames = ExtractIds(options.TestAccount, delimitter);

                // Sign into each account individually
                foreach (string testAccountName in testAccountNames)
                {
                    TestAccount ta = await ToolAuthentication.SignInTestAccountAsync(testAccountName, options.Sandbox);

                    // If we have a failure, output the account and stop the process
                    if (ta == null)
                    {
                        Console.Error.WriteLine($"Failed to log in to test account {testAccountName}.");
                        return -1;
                    }

                    Console.WriteLine($"Using Test account {testAccountName} ({ta.Gamertag}) with xuid {ta.Xuid}");

                    // Accounts to need be processed sequentially when not using a dev account; send one at a time
                    int batchResult = await RunResetBatch(options, new List<string> { ta.Xuid });
                    result = batchResult != 0 ? batchResult : result;
                }
            }
            else if (!string.IsNullOrEmpty(options.XboxUserId))
            {
                string delimitter = options.Delimitter == PartnerCenterDelim ? PartnerXuidDelim : options.Delimitter;
                List<string> xuids = ExtractIds(options.XboxUserId, delimitter);

                DevAccount account = ToolAuthentication.LoadLastSignedInUser();
                if (account == null)
                {
                    Console.Error.WriteLine("Resetting by XUID requires a signed in Partner Center account. Please use \"XblDevAccount.exe signin\" to log in.");
                    return -1;
                }

                // Process the accounts in batches
                for (int i = 0; i < xuids.Count; i += MaxBatchSize)
                {
                    List<string> xuidBatch = xuids.Where((x, index) =>
                        index >= i && index < i + MaxBatchSize).ToList();

                    // TODO: Should we display the gamertags here too?
                    Console.WriteLine($"Using Dev account {account.Name} from {account.AccountSource}");
                    Console.WriteLine($"Processing batch of {xuidBatch.Count} account(s).");

                    // Accounts can be processed in parallel when using a dev account; send several at once
                    int batchResult = await RunResetBatch(options, xuidBatch);
                    result = batchResult != 0 ? batchResult : result;
                }
            }
            else if (!string.IsNullOrEmpty(options.FileName))
            {
                List<string> xuids = new List<string>();
                try
                {
                    // Create an instance of StreamReader to read from a file.
                    // The using statement also closes the StreamReader.
                    using (StreamReader sr = new StreamReader(File.OpenRead(@options.FileName)))
                    {
                        // TODO: Add format option
                        string line;
                        // Read and display lines from the file until the end of
                        // the file is reached.
                        line = sr.ReadLine(); // Skip the first line of headers
                        while ((line = sr.ReadLine()) != null)
                        {
                            xuids.Add(line.Split(',')[0]);
                        }
                    }
                }
                catch (Exception e)
                {
                    // Let the user know what went wrong.
                    Console.WriteLine("The file could not be read:");
                    Console.WriteLine(e.Message);
                }
            }

            return result;
        }

        private static List<string> ExtractIds(string input, string delimitter)
        {
            List<string> ids = new List<string>();

            // Check if the input is a file
            bool isFile = input.Contains('.') && !input.Contains('@');

            if (isFile)
            {
                // If so, extract the contents into a string
                try
                {
                    // Create an instance of StreamReader to read from a file.
                    // The using statement also closes the StreamReader.
                    using (StreamReader sr = new StreamReader(File.OpenRead(@input)))
                    {
                        // TODO: Add format option
                        input = string.Empty;
                        string line;
                        // Read and display lines from the file until the end of
                        // the file is reached.
                        if (delimitter == PartnerEmaillDelim || delimitter == PartnerXuidDelim)
                            sr.ReadLine(); // Skip the first line of headers
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (delimitter == PartnerEmaillDelim)
                                input += line.Split(',')[1] + ",";
                            else if (delimitter == PartnerXuidDelim)
                                input += line.Split(',')[0] + ",";
                        }
                    }
                }
                catch (Exception e)
                {
                    // Let the user know what went wrong.
                    Console.WriteLine("The file could not be read:");
                    Console.WriteLine(e.Message);
                }
            }

            // Then process

            char[] delim = delimitter == PartnerEmaillDelim || delimitter == PartnerXuidDelim ? new char[] { ',' } : new char[] { delimitter[0] };
            // Extract the account names from the input
            ids = input.Split(delim, StringSplitOptions.RemoveEmptyEntries).ToList();

            return ids;
        }

        private static async Task<int> RunResetBatch(ResetOptions options, List<string> xuidBatch)
        {
            try
            {
                UserResetResult result = await PlayerReset.ResetPlayerDataAsync(
                    options.ServiceConfigurationId,
                    options.Sandbox, xuidBatch);

                switch (result.OverallResult)
                {
                    case ResetOverallResult.Succeeded:
                        Console.WriteLine("Player data has been reset successfully.");
                        return 0;
                    case ResetOverallResult.CompletedError:
                        Console.WriteLine("An error occurred while resetting player data:");
                        if (!string.IsNullOrEmpty(result.HttpErrorMessage))
                        {
                            Console.WriteLine($"\t{result.HttpErrorMessage}");
                        }

                        PrintProviderDetails(result.ProviderStatus);
                        return -1;
                    case ResetOverallResult.Timeout:
                        Console.WriteLine("Player data reset has timed out:");
                        PrintProviderDetails(result.ProviderStatus);
                        return -1;
                    default:
                        Console.WriteLine("An unknown error occurred while resetting player data.");
                        return -1;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Error: player data reset failed");

                if (ex.Message.Contains(Convert.ToString((int)HttpStatusCode.Unauthorized)))
                {
                    Console.WriteLine(
                        $"Unable to authorize the account with Xbox Live and scid : {options.ServiceConfigurationId} and sandbox : {options.Sandbox}, please contact your administrator.");
                }
                else if (ex.Message.Contains(Convert.ToString((int)HttpStatusCode.Forbidden)))
                {
                    Console.WriteLine(
                        "Your account doesn't have access to perform the operation, please contact your administrator.");
                }
                else
                {
                    Console.WriteLine(ex.Message);
                }

                return -1;
            }
        }

        internal class ResetOptions
        {
            [Option('c', "scid", Required = true,
                HelpText = "The service configuration ID (SCID) of the title for player data resetting")]
            public string ServiceConfigurationId { get; set; }

            [Option('s', "sandbox", Required = true,
                HelpText = "The target sandbox for player resetting")]
            public string Sandbox { get; set; }

            [Option('x', "xuid", Required = false, SetName = "xuid",
                // TODO: Owner? Admin? Who is allowed to run this?
                HelpText = "A list of Xbox Live User IDs (XUID) of the players to be reset. Requires login of xuid owner.")]
            public string XboxUserId { get; set; }

            [Option('u', "user", Required = false, SetName = "testacct",
                HelpText = "A list of email addresses of the test accounts to be reset. Requires password input per account.")]
            public string TestAccount { get; set; }

            // TODO: Write better help text
            [Option('f', "file", Required = false,
                HelpText = "A file with account information.")]
            public string FileName { get; set; }

            // TODO: Use better help text
            [Option('d', "delimitter", Required = false, Default = ",",
                HelpText = "Delimitter, can be a character or 'PartnerCenter'")]
            public string Delimitter { get; set; } //TODO: Set default

            [Usage(ApplicationAlias = "XblPlayerDataReset")]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    // TODO: Update usage example
                    yield return new Example("Reset a player for given scid and sandbox", new ResetOptions { ServiceConfigurationId = "xxx", Sandbox = "xxx", XboxUserId = "xxx", TestAccount = "xxx@xboxtest.com" });
                }
            }
        }
    }
}
