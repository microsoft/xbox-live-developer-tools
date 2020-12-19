// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblDevAccount
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using CommandLine;
    using CommandLine.Text;
    using Microsoft.Xbox.Services.DevTools.Authentication;

    internal class Program
    {
        private enum AccountSourceOption
        {
            WindowsDevCenter = DevAccountSource.WindowsDevCenter,
        }

        private static async Task<int> Main(string[] args)
        {
            int exitCode = 0;
            try
            {
                string invokedVerb = string.Empty;
                SignInOptions signInOptions = null;

                // Only assign the option and verb here, as the commandlineParser doesn't support async callback yet.
                var result = Parser.Default.ParseArguments<SignInOptions, SignOutOptions, ShowOptions>(args)
                    .WithParsed<SignInOptions>(options =>
                    {
                        invokedVerb = "signin";
                        signInOptions = options;
                    })
                    .WithParsed<SignOutOptions>(options => exitCode = OnSignOut())
                    .WithParsed<ShowOptions>(options => exitCode = OnShow())
                    .WithNotParsed(err => exitCode = -1);

                if (invokedVerb == "signin" && signInOptions != null)
                {
                    exitCode = await OnSignIn(signInOptions);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: unexpected error found.");
                Console.WriteLine(ex.Message);
                exitCode = -1;
            }

            return exitCode;
        }

        private static async Task<int> OnSignIn(SignInOptions signInOptions)
        {
            try
            {
                var devAccount = await ToolAuthentication.SignInAsync((DevAccountSource)signInOptions.AccountSource, signInOptions.UserName, signInOptions.Tenant);
                Console.WriteLine($"Developer account {devAccount.Name} has successfully signed in.");
                DisplayDevAccount(devAccount, "\t");
                return 0;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Error: signin failed");
                if (ex.Message.Contains(Convert.ToString((int)HttpStatusCode.Unauthorized)))
                {
                    Console.WriteLine("Unable to authorize this account with Xbox Live. Please check your account.");
                }
                else
                {
                    Console.WriteLine(ex.Message);
                }

                return -1;
            }
        }

        private static void DisplayDevAccount(DevAccount devAccount, string indent)
        {
            Console.WriteLine($"{indent}ID : {devAccount.Id}");
            Console.WriteLine($"{indent}Publisher ID : {devAccount.AccountId}");
            Console.WriteLine($"{indent}AccountType : {devAccount.AccountType}");
            Console.WriteLine($"{indent}AccountMoniker : {devAccount.AccountMoniker}");
            Console.WriteLine($"{indent}AccountSource : {devAccount.AccountSource}");
        }

        private static int OnSignOut()
        {
            DevAccount account = ToolAuthentication.LoadLastSignedInUser();
            if (account != null)
            {
                ToolAuthentication.SignOut();
                Console.WriteLine($"Developer account {account.Name} from {account.AccountSource} has successfully signed out.");
                return 0;
            }
            else
            {
                Console.WriteLine($"No signed in account found.");
                return -1;
            }
        }

        private static int  OnShow()
        {
            DevAccount account = ToolAuthentication.LoadLastSignedInUser();
            if (account != null)
            {
                Console.WriteLine($"Developer account {account.Name} from {account.AccountSource} is currently signed in.");
                DisplayDevAccount(account, "\t");
                return 0;
            }
            else
            {
                Console.WriteLine($"No signed in account found.");
                return -1;
            }
        }

        [Verb("signin", HelpText = "Sign in an Xbox Live developer account.")]
        private class SignInOptions
        {
            public SignInOptions()
            {
                this.AccountSource = AccountSourceOption.WindowsDevCenter;
            }

            public AccountSourceOption AccountSource { get; set; }

            [Option('u', "name", Required = true,
                HelpText = "The user name of the account.")]
            public string UserName { get; set; }

            [Option('t', "tenant", Required = false,
                HelpText = "The AAD tenant of the account. This will default to \"common\".")]
            public string Tenant { get; set; }

            [Usage]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example(
                        "Sign in an Xbox Live developer account.",
                        new SignInOptions
                        {
                            AccountSource = AccountSourceOption.WindowsDevCenter,
                            UserName = "xxxx@xxx.com"
                        });
                }
            }
        }

        [Verb("signout", HelpText = "Sign out the signed in developer account.")]
        private class SignOutOptions
        {
            [Usage]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example("Sign out the Xbox Live developer account.", new SignOutOptions());
                }
            }
        }

        [Verb("show", HelpText = "Display current signed in user.")]
        private class ShowOptions
        {
            [Usage]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example("Display current signed in user.", new ShowOptions());
                }
            }
        }
    }
}
