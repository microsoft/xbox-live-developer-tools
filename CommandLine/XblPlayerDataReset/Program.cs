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

            DevAccount devAccount = null;

            if (!string.IsNullOrEmpty(options.File))
            {
                // Detect if file is of type partner center (check headers)
                // If so, extract the contents into a string
                try
                {
                    using (StreamReader sr = new StreamReader(File.OpenRead(@options.File)))
                    {
                        // Prepare to read values from the file
                        string input = string.Empty;
                        string delimiter = options.Delimiter;
                        bool partnerCenterFormat = false;
                        bool isEmail = false;

                        string line;
                        // Check the first line for Partner Center headers
                        string firstLine = sr.ReadLine();
                        if (firstLine.Contains("Xuid,Email,Gamertag,IsDeleted,AccountId,Etag,Subscriptions,Keywords"))
                        {
                            partnerCenterFormat = true;
                            Console.WriteLine("File has Partner Center format.");
                        }

                        if (partnerCenterFormat)
                        {
                            // Set delimiter to comma
                            options.Delimiter = ",";

                            // Attempt to sign into dev account
                            devAccount = ToolAuthentication.LoadLastSignedInUser();

                            // Fail, read emails
                            if (devAccount == null)
                            {
                                Console.WriteLine("Partner Center account not logged in. Using email sign in option.");
                                delimiter = PartnerEmaillDelim;
                                isEmail = true;
                            }

                            // Success, read xuids
                            else
                            {
                                Console.WriteLine("Successfully signed into Partner Center account. Using xuid sign in option.");
                                delimiter = PartnerXuidDelim;
                            }
                        }
                        else
                        {
                            // Detect if using email or xuid
                            if (firstLine.Contains("@"))
                                isEmail = true;
                            string type = isEmail ? "emails" : "xuids";
                            Console.WriteLine($"Reading '{options.Delimiter}' delimitted file of {type}.");
                            input += firstLine + delimiter;
                        }

                        // Read the file line by line using the appropriate delimiter
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (delimiter == PartnerEmaillDelim)
                                input += line.Split(',')[1] + ","; // Second value in line
                            else if (delimiter == PartnerXuidDelim)
                                input += line.Split(',')[0] + ","; // First value in line
                            else
                                input += line + delimiter; // Read the whole line
                        }

                        // Set input to be either email or xuid and run the rest of the process below
                        if (isEmail)
                            options.TestAccount = input;
                        else
                            options.XboxUserId = input;
                    }
                }
                catch (NullReferenceException ne)
                {
                    Console.Error.WriteLine("The file could not be read:");
                    Console.Error.WriteLine("The file appears to have no content.");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("The file could not be read:");
                    Console.Error.WriteLine(e.Message);
                }
            }

            if (!string.IsNullOrEmpty(options.TestAccount))
            {
                // Extract account emails
                List<string> testAccountNames = ExtractIds(options.TestAccount, options.Delimiter);

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
                // Extract XUIDs
                List<string> xuids = ExtractIds(options.XboxUserId, options.Delimiter);

                // If we haven't already, sign into dev account
                devAccount = devAccount ?? ToolAuthentication.LoadLastSignedInUser();
                if (devAccount == null)
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
                    Console.WriteLine($"Using Dev account {devAccount.Name} from {devAccount.AccountSource}");
                    Console.WriteLine($"Processing batch of {xuidBatch.Count} account(s).");

                    // Accounts can be processed in parallel when using a dev account; send several at once
                    int batchResult = await RunResetBatch(options, xuidBatch);
                    result = batchResult != 0 ? batchResult : result;
                }
            }

            return result;
        }

        private static List<string> ExtractIds(string input, string delimiter)
        {
            List<string> ids = new List<string>();

            // Set up the delimiter as char array
            char[] delim = new char[] { delimiter[0] };
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
                HelpText = "A list of Xbox Live User IDs (XUID) of the players to be reset. Requires login for xuid owner.")]
            public string XboxUserId { get; set; }

            [Option('u', "user", Required = false, SetName = "testacct",
                HelpText = "A list of email addresses of the test accounts to be reset. Requires login for each email.")]
            public string TestAccount { get; set; }

            [Option('f', "file", Required = false, SetName = "file",
                HelpText = "File location with account information. Can be a set of xuids, emails, or an exported Partner Center csv.")]
            public string File { get; set; }

            [Option('d', "delimiter", Required = false, Default = ",",
                HelpText = "Delimiter that separates accounts to reset. Defaults to \",\".")]
            public string Delimiter { get; set; }

            [Usage(ApplicationAlias = "XblPlayerDataReset")]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example("Reset a player for given scid and sandbox", new ResetOptions { ServiceConfigurationId = "xxx", Sandbox = "xxx", XboxUserId = "xxx", TestAccount = "xxx@xboxtest.com", File = "path/to/file", Delimiter = "," });
                }
            }
        }
    }
}
