// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblTestAccount
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using CommandLine;
    using Microsoft.Xbox.Services.DevTools.Authentication;
    using Newtonsoft.Json;

    internal class Program
    {
        private const string ToolName = "XblTestAccount";
        private const string UnknownPrivilegeName = "(unknown)";
        private const string HelpHelp = "Display this help screen.";
        private const string VersionHelp = "Display version information.";
        private const string JsonHelp = "Optional. Write the output as parsable json instead of a table.";
        private const string RestrictedState = "Restricted";

        /// <summary>
        /// Parses the options of a single command.
        /// </summary>
        /// <remarks>
        /// The help and version screens are rendered by this tool rather than by the parser, so
        /// that the base help offers "--help" and "--version" as options instead of listing "help"
        /// and "version" as verbs, and so that a command such as "privilege" can document the sub
        /// commands it takes. The parser has no notion of a nested verb, so each command takes its
        /// own sub command off the front of the arguments and parses only what is left.
        /// </remarks>
        private static readonly Parser CommandParser = new Parser(settings =>
        {
            settings.AutoHelp = false;
            settings.AutoVersion = false;
            settings.HelpWriter = null;
        });

        private static async Task<int> Main(string[] args)
        {
            try
            {
                return await Run(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Dispatches on the command, which is always the first argument.
        /// </summary>
        /// <param name="args">The command line as given.</param>
        /// <returns>The exit code of the command.</returns>
        private static async Task<int> Run(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                WriteRootHelp();
                return 0;
            }

            string command = args[0];
            string[] rest = args.Skip(1).ToArray();

            if (IsHelpFlag(command))
            {
                WriteRootHelp();
                return 0;
            }

            if (IsVersionFlag(command))
            {
                WriteVersion();
                return 0;
            }

            switch (command.ToLowerInvariant())
            {
                case "signin":
                    if (WantsHelp(rest))
                    {
                        return WriteSignInHelp();
                    }

                    return TryParse(rest, out SignInOptions signIn) ? await OnSignIn(signIn) : UsageError(WriteSignInHelp);

                case "signout":
                    if (WantsHelp(rest))
                    {
                        return WriteSignOutHelp();
                    }

                    return TryParse(rest, out SignOutOptions _) ? OnSignOut() : UsageError(WriteSignOutHelp);

                case "show":
                    if (WantsHelp(rest))
                    {
                        return WriteShowHelp();
                    }

                    return TryParse(rest, out ShowOptions show) ? OnShow(show) : UsageError(WriteShowHelp);

                case "privilege":
                    return await OnPrivilegeCommand(rest);

                case "privacy":
                    return await OnPrivacyCommand(rest);

                case "people":
                    return await OnPeopleCommand(rest);

                default:
                    Console.Error.WriteLine($"Error: unknown command \"{command}\".");
                    WriteRootHelp();
                    return -1;
            }
        }

        /// <summary>
        /// Dispatches the sub command of the privilege command.
        /// </summary>
        /// <param name="args">The arguments after the privilege command.</param>
        /// <returns>The exit code of the sub command.</returns>
        private static async Task<int> OnPrivilegeCommand(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Error: the privilege command requires an action.");
                WritePrivilegeHelp();
                return -1;
            }

            string action = args[0];
            string[] rest = args.Skip(1).ToArray();

            if (IsHelpFlag(action))
            {
                WritePrivilegeHelp();
                return 0;
            }

            switch (action.ToLowerInvariant())
            {
                case "show":
                    if (WantsHelp(rest))
                    {
                        return WritePrivilegeShowHelp();
                    }

                    return TryParse(rest, out PrivilegeShowOptions show)
                        ? await OnPrivilegeShow(show)
                        : UsageError(WritePrivilegeShowHelp);

                case "block":
                case "allow":
                    if (WantsHelp(rest))
                    {
                        return WritePrivilegeChangeHelp(action.ToLowerInvariant());
                    }

                    return TryParse(rest, out PrivilegeChangeOptions change)
                        ? await OnPrivilegeChange(change, action.ToLowerInvariant())
                        : UsageError(() => WritePrivilegeChangeHelp(action.ToLowerInvariant()));

                default:
                    Console.Error.WriteLine($"Error: unknown privilege action \"{action}\". Expected block, allow or show.");
                    WritePrivilegeHelp();
                    return -1;
            }
        }

        /// <summary>
        /// Dispatches the sub command of the privacy command.
        /// </summary>
        /// <param name="args">The arguments after the privacy command.</param>
        /// <returns>The exit code of the sub command.</returns>
        private static async Task<int> OnPrivacyCommand(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Error: the privacy command requires an action.");
                WritePrivacyHelp();
                return -1;
            }

            string action = args[0];
            string[] rest = args.Skip(1).ToArray();

            if (IsHelpFlag(action))
            {
                WritePrivacyHelp();
                return 0;
            }

            switch (action.ToLowerInvariant())
            {
                case "show":
                    if (WantsHelp(rest))
                    {
                        return WritePrivacyShowHelp();
                    }

                    return TryParse(rest, out PrivacyShowOptions show)
                        ? await OnPrivacyShow(show)
                        : UsageError(WritePrivacyShowHelp);

                case "set":
                    if (WantsHelp(rest))
                    {
                        return WritePrivacySetHelp();
                    }

                    return TryParse(rest, out PrivacySetOptions set)
                        ? await OnPrivacySet(set)
                        : UsageError(WritePrivacySetHelp);

                default:
                    Console.Error.WriteLine($"Error: unknown privacy action \"{action}\". Expected set or show.");
                    WritePrivacyHelp();
                    return -1;
            }
        }

        /// <summary>
        /// Dispatches the sub command of the people command.
        /// </summary>
        /// <param name="args">The arguments after the people command.</param>
        /// <returns>The exit code of the sub command.</returns>
        private static async Task<int> OnPeopleCommand(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Error: the people command requires an action.");
                WritePeopleHelp();
                return -1;
            }

            string action = args[0];
            string[] rest = args.Skip(1).ToArray();

            if (IsHelpFlag(action))
            {
                WritePeopleHelp();
                return 0;
            }

            if (string.Equals(action, "show", StringComparison.OrdinalIgnoreCase))
            {
                if (WantsHelp(rest))
                {
                    return WritePeopleShowHelp();
                }

                return TryParse(rest, out PeopleShowOptions show)
                    ? await OnPeopleShow(show)
                    : UsageError(WritePeopleShowHelp);
            }

            if (TryParseEnumName(action, out PeopleAction change))
            {
                if (WantsHelp(rest))
                {
                    return WritePeopleChangeHelp(change);
                }

                return TryParse(rest, out PeopleChangeOptions options)
                    ? await OnPeopleChange(options, change)
                    : UsageError(() => WritePeopleChangeHelp(change));
            }

            string expected = string.Join(", ", Enum.GetNames(typeof(PeopleAction)).Select(name => name.ToLowerInvariant()));
            Console.Error.WriteLine($"Error: unknown people action \"{action}\". Expected show, {expected}.");
            WritePeopleHelp();
            return -1;
        }

        private static async Task<int> OnSignIn(SignInOptions signInOptions)
        {
            try
            {
                TestAccount testAccount = await ToolAuthentication.SignInTestAccountAsync(
                    signInOptions.UserName, signInOptions.Sandbox, signInOptions.Force);

                Console.WriteLine($"Test account {testAccount.UserName} has successfully signed in to sandbox {testAccount.Sandbox}.");
                DisplayTestAccount(testAccount, "\t");
                Console.WriteLine();
                Console.WriteLine($"Run \"{ToolName} --help\" to see what you can do with this account.");
                return 0;
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine("Error: test account signin failed");
                if (ex.Message.Contains(Convert.ToString((int)HttpStatusCode.Unauthorized)))
                {
                    Console.Error.WriteLine("Unable to authorize this account with Xbox Live. Please check your account.");
                }
                else
                {
                    Console.Error.WriteLine(ex.Message);
                }

                return -1;
            }
        }

        private static int OnSignOut()
        {
            TestAccount testAccount = ToolAuthentication.LoadLastSignedInTestAccount();
            if (testAccount == null)
            {
                Console.Error.WriteLine("No signed in test account found.");
                return -1;
            }

            ToolAuthentication.SignOutTestAccount();
            Console.WriteLine($"Test account {testAccount.UserName} has successfully signed out.");
            return 0;
        }

        /// <summary>
        /// Reports who the signed in account is. The privileges and the privacy settings are
        /// reported by their own commands, so that each command has one job and one shape.
        /// </summary>
        /// <param name="options">The output options of the show command.</param>
        /// <returns>Zero when the account was reported.</returns>
        private static int OnShow(ShowOptions options)
        {
            TestAccount testAccount = LoadSignedInTestAccount();
            if (testAccount == null)
            {
                return -1;
            }

            if (options.Json)
            {
                WriteJson(new
                {
                    userName = testAccount.UserName,
                    gamertag = testAccount.Gamertag,
                    xuid = testAccount.Xuid,
                    sandbox = testAccount.Sandbox,
                    ageGroup = testAccount.AgeGroup.ToString(),
                });

                return 0;
            }

            Console.WriteLine($"Test account {testAccount.UserName} is currently signed in.");
            DisplayTestAccount(testAccount, "\t");
            return 0;
        }

        /// <summary>
        /// Reports every privilege this tool knows of, together with the state the signed in
        /// account holds it in and where it is controlled from. One listing answers both questions,
        /// rather than splitting the names into one command and the states into another.
        /// </summary>
        /// <param name="options">The output options and the sandbox to work against.</param>
        /// <returns>Zero when the privileges were reported.</returns>
        private static async Task<int> OnPrivilegeShow(PrivilegeShowOptions options)
        {
            TestAccount testAccount = LoadSignedInTestAccount();
            if (testAccount == null)
            {
                return -1;
            }

            if (!TryResolveSandbox(testAccount, out string sandbox))
            {
                return -1;
            }

            // Privileges are claims on the token, so the cached copy reports them as they were at
            // sign in. Refreshing mints a new token, which is how a change made since then, either
            // by this tool or elsewhere, becomes visible.
            if (options.Refresh)
            {
                try
                {
                    testAccount = await ToolAuthentication.GetTestAccountSilentlyAsync(sandbox, true);
                }
                catch (Exception ex)
                {
                    return ReportTokenFailure(new TestAccountTokenException(sandbox, ex));
                }
            }

            List<PrivilegeState> states = BuildPrivilegeStates(testAccount);

            if (options.Blocked)
            {
                states = states.Where(state => state.State == RestrictedState).ToList();
            }

            if (options.Json)
            {
                WriteJson(states.Select(state => new
                {
                    id = state.Id,
                    name = state.Known ? state.Name : null,
                    state = state.State,
                    editable = state.Editable,
                    privacySetting = state.PrivacySetting,
                }));

                return 0;
            }

            string heading = options.Blocked ? "Restricted privileges" : "Privileges";
            Console.WriteLine($"{heading} for {testAccount.Gamertag} ({testAccount.Xuid}):");

            if (states.Count == 0)
            {
                Console.WriteLine("    (none)");
            }
            else
            {
                // Pad to the widest name and state so the notes line up in a column, rather than
                // relying on tab stops that the varying name lengths push out of alignment.
                int nameWidth = states.Max(state => state.Name.Length);
                int stateWidth = states.Max(state => state.State.Length);
                foreach (PrivilegeState state in states)
                {
                    string note = DescribePrivilegeSource(state.Id);
                    Console.WriteLine($"    {FormatId(state.Id)}  {state.Name.PadRight(nameWidth)}  {state.State.PadRight(stateWidth)}  {note}".TrimEnd());
                }
            }

            if (!options.Refresh)
            {
                Console.WriteLine();
                Console.WriteLine("Privileges are read from the cached token. Add --refresh to report them as they are now.");
            }

            return 0;
        }

        /// <summary>
        /// Blocks or allows the given privileges on the signed in account.
        /// </summary>
        /// <param name="options">The privilege numbers and the sandbox to work against.</param>
        /// <param name="action">Either "block" or "allow".</param>
        /// <returns>Zero when the change was made.</returns>
        private static async Task<int> OnPrivilegeChange(PrivilegeChangeOptions options, string action)
        {
            bool block = action == "block";
            List<string> given = options.Privileges?.ToList() ?? new List<string>();

            if (given.Count == 0)
            {
                Console.Error.WriteLine($"Error: the {action} action requires at least one privilege number, for example \"{ToolName} privilege {action} 185\".");
                Console.Error.WriteLine($"Run \"{ToolName} privilege show\" to see the numbers.");
                return UsageError(() => WritePrivilegeChangeHelp(action));
            }

            var privileges = new List<int>();
            foreach (string value in given)
            {
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int privilege))
                {
                    Console.Error.WriteLine($"Error: \"{value}\" is not a privilege number. Expected a number such as 185.");
                    Console.Error.WriteLine($"Run \"{ToolName} privilege show\" to see the numbers.");
                    return -1;
                }

                privileges.Add(privilege);
            }

            if (!ValidateEditable(privileges, action))
            {
                return -1;
            }

            TestAccount testAccount = LoadSignedInTestAccount();
            if (testAccount == null)
            {
                return -1;
            }

            if (!TryResolveSandbox(testAccount, out string sandbox))
            {
                return -1;
            }

            // The parental service only accepts Actor=Self, so the account being changed is always
            // the one that owns the token.
            string xuid = testAccount.Xuid;

            try
            {
                string gerund = block ? "Restricting" : "Unrestricting";
                Console.WriteLine($"{gerund} {DescribePrivileges(privileges)} on {testAccount.Gamertag} ({xuid}).");

                await PrivilegeClient.SetRestrictionsAsync(sandbox, xuid, privileges, block);

                Console.WriteLine($"Done. Run \"{ToolName} privilege show --refresh -b\" for the privileges now restricted.");
                return 0;
            }
            catch (TestAccountTokenException ex)
            {
                return ReportTokenFailure(ex);
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine("Error: the privilege call failed.");
                Console.Error.WriteLine(ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Reports every privacy setting the service exposes, with its current value.
        /// </summary>
        /// <param name="options">The output options and the sandbox to work against.</param>
        /// <returns>Zero when the settings were reported.</returns>
        private static async Task<int> OnPrivacyShow(PrivacyShowOptions options)
        {
            TestAccount testAccount = LoadSignedInTestAccount();
            if (testAccount == null)
            {
                return -1;
            }

            if (!TryResolveSandbox(testAccount, out string sandbox))
            {
                return -1;
            }

            try
            {
                IDictionary<string, string> settings = await PrivacyClient.GetSettingsAsync(sandbox, testAccount.Xuid);

                if (options.Json)
                {
                    WriteJson(settings.Select(entry => new
                    {
                        setting = entry.Key,
                        value = entry.Value,
                        privilege = PrivilegeNames.TryGetPrivilegeForSetting(entry.Key, out int privilege)
                            ? (int?)privilege
                            : null,
                    }));

                    return 0;
                }

                Console.WriteLine($"Privacy settings for {testAccount.Gamertag} ({testAccount.Xuid}):");
                DisplayPrivacySettings(settings);
                return 0;
            }
            catch (TestAccountTokenException ex)
            {
                return ReportTokenFailure(ex);
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine("Error: the privacy call failed.");
                Console.Error.WriteLine(ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Changes one privacy setting on the signed in account.
        /// </summary>
        /// <param name="options">The setting, the value and the sandbox to work against.</param>
        /// <returns>Zero when the change was made.</returns>
        private static async Task<int> OnPrivacySet(PrivacySetOptions options)
        {
            // The arguments are checked before the account is loaded and the service is called, so
            // that a usage mistake is reported without a round trip.
            if (options.Surplus != null && options.Surplus.Any())
            {
                Console.Error.WriteLine($"Error: unexpected argument \"{options.Surplus.First()}\". A setting name or value containing a space must be quoted.");
                return -1;
            }

            if (string.IsNullOrWhiteSpace(options.Setting) || string.IsNullOrWhiteSpace(options.Value))
            {
                Console.Error.WriteLine($"Error: the set action requires a setting and a value, for example \"{ToolName} privacy set CommunicateDuringCrossNetworkPlay Blocked\".");
                Console.Error.WriteLine($"Run \"{ToolName} privacy show\" to see the settings.");
                return UsageError(WritePrivacySetHelp);
            }

            if (!TryParseEnumName(NormalizeValue(options.Value), out PrivacyValue value))
            {
                Console.Error.WriteLine($"Error: unknown value \"{options.Value}\". Expected {string.Join(", ", Enum.GetNames(typeof(PrivacyValue)))}.");
                return -1;
            }

            TestAccount testAccount = LoadSignedInTestAccount();
            if (testAccount == null)
            {
                return -1;
            }

            if (!TryResolveSandbox(testAccount, out string sandbox))
            {
                return -1;
            }

            // The write goes through /users/me, so the account changed is always the token owner.
            string xuid = testAccount.Xuid;

            try
            {
                IDictionary<string, string> settings = await PrivacyClient.GetSettingsAsync(sandbox, xuid);

                // The service is the authority on which settings exist, so the name is checked
                // against the set it just reported rather than a list held by this tool.
                string setting = PrivacyClient.ResolveSettingName(settings, options.Setting);
                if (setting == null)
                {
                    Console.Error.WriteLine($"Error: the service does not expose a privacy setting named \"{options.Setting}\".");
                    Console.Error.WriteLine($"The settings available on this account are: {string.Join(", ", settings.Keys)}.");
                    return -1;
                }

                Console.WriteLine($"Setting {setting} to {value} on {testAccount.Gamertag} ({xuid}).");
                settings = await PrivacyClient.SetSettingAsync(sandbox, xuid, setting, value);

                settings.TryGetValue(setting, out string applied);
                if (string.Equals(applied, value.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"{setting} is now {applied}.");
                }
                else
                {
                    Console.Error.WriteLine($"Warning: the service accepted the change but still reports {setting} as {applied ?? "missing"}.");
                    Console.Error.WriteLine($"It can take a moment to become visible. Run \"{ToolName} privacy show\" again to confirm.");
                }

                if (PrivilegeNames.TryGetPrivilegeForSetting(setting, out int privilege))
                {
                    Console.WriteLine($"This setting controls privilege {PrivilegeNames.Describe(privilege)}. Run \"{ToolName} privilege show --refresh\" to see it take effect.");
                }

                return 0;
            }
            catch (TestAccountTokenException ex)
            {
                return ReportTokenFailure(ex);
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine("Error: the privacy call failed.");
                Console.Error.WriteLine(ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Reports who the signed in account follows, blocks and mutes.
        /// </summary>
        /// <param name="options">The lists to report and the output options.</param>
        /// <returns>Zero when the lists were reported.</returns>
        private static async Task<int> OnPeopleShow(PeopleShowOptions options)
        {
            TestAccount testAccount = LoadSignedInTestAccount();
            if (testAccount == null)
            {
                return -1;
            }

            if (!TryResolveSandbox(testAccount, out string sandbox))
            {
                return -1;
            }

            // With no list named, every list is reported.
            bool all = !options.Following && !options.Blocked && !options.Muted;

            try
            {
                IList<FollowedPerson> following = all || options.Following ? await PeopleClient.GetFollowingAsync(sandbox) : null;
                IList<string> blocked = all || options.Blocked ? await PeopleClient.GetBlockedAsync(sandbox, testAccount.Xuid) : null;
                IList<string> muted = all || options.Muted ? await PeopleClient.GetMutedAsync(sandbox, testAccount.Xuid) : null;

                if (options.Json)
                {
                    var payload = new Dictionary<string, object>();
                    if (following != null)
                    {
                        payload["following"] = following.Select(person => new
                        {
                            xuid = person.Xuid,
                            isFavorite = person.IsFavorite,
                            followsBack = person.IsFollowingCaller,
                        });
                    }

                    if (blocked != null)
                    {
                        payload["blocked"] = blocked;
                    }

                    if (muted != null)
                    {
                        payload["muted"] = muted;
                    }

                    WriteJson(payload);
                    return 0;
                }

                Console.WriteLine($"People lists for {testAccount.Gamertag} ({testAccount.Xuid}):");

                if (following != null)
                {
                    DisplayPeople("Following", following.Select(person => Entry(person.Xuid, DescribeFollowedPerson(person))).ToList());
                }

                if (blocked != null)
                {
                    DisplayPeople("Blocked", blocked.Select(xuid => Entry(xuid, string.Empty)).ToList());
                }

                if (muted != null)
                {
                    DisplayPeople("Muted", muted.Select(xuid => Entry(xuid, string.Empty)).ToList());
                }

                return 0;
            }
            catch (TestAccountTokenException ex)
            {
                return ReportTokenFailure(ex);
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine("Error: the people call failed.");
                Console.Error.WriteLine(ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Follows, friends, favorites, blocks or mutes people, or undoes one of those, on the
        /// signed in account. Large lists are sent in batches, paced to the service's rate limit.
        /// </summary>
        /// <param name="options">The people to change.</param>
        /// <param name="action">The change to make.</param>
        /// <returns>Zero when every person was changed.</returns>
        private static async Task<int> OnPeopleChange(PeopleChangeOptions options, PeopleAction action)
        {
            string verb = action.ToString().ToLowerInvariant();

            if (!TryReadXuids(options, out List<string> xuids))
            {
                return -1;
            }

            if (xuids.Count == 0)
            {
                Console.Error.WriteLine($"Error: the {verb} action requires at least one XUID or a file of them, for example \"{ToolName} people {verb} 2814634367189023\".");
                return UsageError(() => WritePeopleChangeHelp(action));
            }

            TestAccount testAccount = LoadSignedInTestAccount();
            if (testAccount == null)
            {
                return -1;
            }

            if (!TryResolveSandbox(testAccount, out string sandbox))
            {
                return -1;
            }

            if (XuidList.TryParse(testAccount.Xuid, out string self) && xuids.Remove(self))
            {
                Console.WriteLine($"Skipping {testAccount.Gamertag} ({self}), which is the signed in account itself.");
                if (xuids.Count == 0)
                {
                    Console.Error.WriteLine("Error: no other XUIDs were given.");
                    return -1;
                }
            }

            var skipped = new List<string>();
            if (action == PeopleAction.Follow || action == PeopleAction.Friend)
            {
                // The service removes the block when following or friending someone, even if the call fails.
                try
                {
                    var blocked = new HashSet<string>(await PeopleClient.GetBlockedAsync(sandbox, testAccount.Xuid), StringComparer.Ordinal);
                    skipped = xuids.Where(blocked.Contains).ToList();
                }
                catch (TestAccountTokenException ex)
                {
                    return ReportTokenFailure(ex);
                }
                catch (HttpRequestException ex)
                {
                    Console.Error.WriteLine("Error: the blocked list could not be read, so nobody was changed.");
                    Console.Error.WriteLine(ex.Message);
                    return -1;
                }

                if (skipped.Count > 0)
                {
                    xuids.RemoveAll(skipped.Contains);
                    Console.WriteLine($"Skipping {CountPeople(skipped.Count)} the account has blocked, since {DescribeGerund(action).ToLowerInvariant()} them would remove the block. Run \"{ToolName} people unblock\" first to {verb} them:");
                    foreach (string xuid in skipped)
                    {
                        Console.WriteLine("    " + xuid);
                    }

                    if (xuids.Count == 0)
                    {
                        Console.Error.WriteLine($"Error: nobody is left to {verb}.");
                        return -1;
                    }
                }
            }

            if (action == PeopleAction.Follow && xuids.Count > PeopleClient.FollowLimit)
            {
                Console.Error.WriteLine($"Error: {xuids.Count} XUIDs were given, but the service lets an account follow no more than {PeopleClient.FollowLimit} people in 12 hours.");
                Console.Error.WriteLine("Split the list and follow the rest later.");
                return -1;
            }

            int batchSize = PeopleClient.GetBatchSize(action);
            int delay = PeopleClient.GetBatchDelayMilliseconds(action);
            int batches = (xuids.Count + batchSize - 1) / batchSize;

            Console.WriteLine($"{DescribeGerund(action)} {CountPeople(xuids.Count)} as {testAccount.Gamertag} ({testAccount.Xuid}).");
            if (batches > 1 && delay > 0)
            {
                Console.WriteLine($"Sending {batches} batches of up to {batchSize}, {delay / 1000} seconds apart to stay inside the rate limit. Press Ctrl+C to stop.");
            }

            var failed = new List<string>();
            int done = 0;

            // Not disposed, so that a Ctrl+C handler still running after the loop cannot cancel a disposed source.
            var cancel = new CancellationTokenSource();

            // The first Ctrl+C stops between batches so that the work done can be reported; a
            // second one ends the process as usual.
            ConsoleCancelEventHandler onCancel = (sender, e) =>
            {
                if (!cancel.IsCancellationRequested)
                {
                    e.Cancel = true;
                    Console.Error.WriteLine("Stopping after the current batch...");
                    cancel.Cancel();
                }
            };

            Console.CancelKeyPress += onCancel;
            try
            {
                for (int offset = 0; offset < xuids.Count && !cancel.IsCancellationRequested; offset += batchSize)
                {
                    if (offset > 0 && delay > 0)
                    {
                        Console.WriteLine($"Waiting {delay / 1000} seconds for the rate limit...");
                        try
                        {
                            await Task.Delay(delay, cancel.Token);
                        }
                        catch (TaskCanceledException)
                        {
                            break;
                        }
                    }

                    List<string> batch = xuids.Skip(offset).Take(batchSize).ToList();
                    if (batches > 1)
                    {
                        Console.WriteLine($"Batch {(offset / batchSize) + 1} of {batches}: {CountPeople(batch.Count)}.");
                    }

                    try
                    {
                        failed.AddRange(await PeopleClient.ApplyAsync(sandbox, action, batch));
                    }
                    catch (TaskCanceledException ex)
                    {
                        // HttpClient reports its timeout as a cancellation.
                        throw new HttpRequestException("The request timed out.", ex);
                    }

                    done += batch.Count;
                }
            }
            catch (TestAccountTokenException ex)
            {
                int exitCode = ReportTokenFailure(ex);
                ReportPeopleFailures(verb, failed);
                ReportPeopleProgress(done - failed.Count, xuids.Count);
                return exitCode;
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"Error: the {verb} call failed.");
                Console.Error.WriteLine(ex.Message);
                ReportPeopleFailures(verb, failed);
                ReportPeopleProgress(done - failed.Count, xuids.Count);

                if (action == PeopleAction.Friend && ex.Message.Contains("HTTP 403"))
                {
                    Console.Error.WriteLine($"The service rejects a whole friend batch that includes someone the account has blocked, and removes those blocks. Run \"{ToolName} people show --blocked\" to check.");
                }
                else if ((action == PeopleAction.Follow || action == PeopleAction.Friend) && ex.Message.Contains("HTTP 429"))
                {
                    Console.Error.WriteLine($"The service allows 100 follows per 30 seconds and {PeopleClient.FollowLimit} per 12 hours.");
                }

                return -1;
            }
            finally
            {
                Console.CancelKeyPress -= onCancel;
            }

            bool cancelled = cancel.IsCancellationRequested && done < xuids.Count;

            ReportPeopleFailures(verb, failed);

            if (cancelled)
            {
                Console.Error.WriteLine($"Stopped after {done} of {CountPeople(xuids.Count)}.");
            }

            if (failed.Count > 0 || cancelled)
            {
                return -1;
            }

            Console.WriteLine($"Done. Run \"{ToolName} people show {DescribeListOption(action)}\" to see the list.");
            if (DescribeListOption(action) != "--following")
            {
                Console.WriteLine("It can take a moment for the change to show.");
            }

            return skipped.Count > 0 ? -1 : 0;
        }

        /// <summary>
        /// Gathers the XUIDs given on the command line and in any file, in order and without
        /// repeats, reporting the first value that is not a XUID.
        /// </summary>
        /// <param name="options">The people command options.</param>
        /// <param name="xuids">Receives the XUIDs.</param>
        /// <returns>True when every value was a XUID.</returns>
        private static bool TryReadXuids(PeopleChangeOptions options, out List<string> xuids)
        {
            xuids = new List<string>();
            var values = (options.Xuids ?? Enumerable.Empty<string>())
                .Select(value => Entry(value, "on the command line"))
                .ToList();

            if (!string.IsNullOrWhiteSpace(options.File))
            {
                IList<string> read;
                try
                {
                    read = XuidList.ReadFile(options.File);
                }
                catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException)
                {
                    Console.Error.WriteLine($"Error: could not read \"{options.File}\". {ex.Message}");
                    return false;
                }

                if (read.Count == 0)
                {
                    Console.Error.WriteLine($"Error: no XUIDs were found in \"{options.File}\".");
                    return false;
                }

                values.AddRange(read.Select(value => Entry(value, $"in \"{options.File}\"")));
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> value in values)
            {
                if (!XuidList.TryParse(value.Key, out string xuid))
                {
                    Console.Error.WriteLine($"Error: \"{value.Key}\" {value.Value} is not a XUID. Expected a number such as 2814634367189023.");
                    if (value.Value != "on the command line")
                    {
                        Console.Error.WriteLine("A file must be a plain list of XUIDs, or a CSV with a Xuid column.");
                    }

                    return false;
                }

                if (seen.Add(xuid))
                {
                    xuids.Add(xuid);
                }
            }

            return true;
        }

        private static string DescribeFollowedPerson(FollowedPerson person)
        {
            var notes = new List<string>();
            if (person.IsFavorite)
            {
                notes.Add("favorite");
            }

            if (person.IsFollowingCaller)
            {
                notes.Add("follows back");
            }

            return string.Join(", ", notes);
        }

        private static string DescribeGerund(PeopleAction action)
        {
            switch (action)
            {
                case PeopleAction.Favorite:
                    return "Favoriting";
                case PeopleAction.Unfavorite:
                    return "Unfavoriting";
                case PeopleAction.Mute:
                    return "Muting";
                case PeopleAction.Unmute:
                    return "Unmuting";
                default:
                    return action + "ing";
            }
        }

        /// <summary>
        /// Gets the people show option that reports the list an action changes.
        /// </summary>
        private static string DescribeListOption(PeopleAction action)
        {
            switch (action)
            {
                case PeopleAction.Block:
                case PeopleAction.Unblock:
                    return "--blocked";
                case PeopleAction.Mute:
                case PeopleAction.Unmute:
                    return "--muted";
                default:
                    return "--following";
            }
        }

        private static string CountPeople(int count)
        {
            return count == 1 ? "1 person" : $"{count} people";
        }

        private static void ReportPeopleFailures(string verb, IList<string> failed)
        {
            if (failed.Count == 0)
            {
                return;
            }

            Console.Error.WriteLine($"Error: the service could not {verb} {CountPeople(failed.Count)}:");
            foreach (string xuid in failed)
            {
                Console.Error.WriteLine("    " + xuid);
            }
        }

        private static void ReportPeopleProgress(int done, int total)
        {
            if (done > 0)
            {
                Console.Error.WriteLine($"{done} of {CountPeople(total)} were changed before the failure.");
            }
        }

        /// <summary>
        /// Builds the state of every privilege worth reporting: the ones this tool knows the name
        /// of, plus any the token carries that it does not.
        /// </summary>
        /// <param name="testAccount">The account whose token carries the privilege claims.</param>
        /// <returns>The privileges in ascending id order.</returns>
        private static List<PrivilegeState> BuildPrivilegeStates(TestAccount testAccount)
        {
            var restricted = new SortedSet<int>(ParsePrivilegeString(testAccount.RestrictedPrivilegeString));
            var granted = new SortedSet<int>(ParsePrivilegeString(testAccount.PrivilegeString));

            var ids = new SortedSet<int>(PrivilegeNames.All.Keys);
            ids.UnionWith(granted);
            ids.UnionWith(restricted);

            var states = new List<PrivilegeState>();
            foreach (int id in ids)
            {
                bool known = PrivilegeNames.TryGetName(id, out string name);

                // A privilege the token names in neither claim is simply not held by this account,
                // which is worth reporting as its own state rather than as a restriction.
                string state = restricted.Contains(id)
                    ? RestrictedState
                    : granted.Contains(id) ? "Granted" : "Not held";

                states.Add(new PrivilegeState
                {
                    Id = id,
                    Known = known,
                    Name = known ? name : UnknownPrivilegeName,
                    State = state,
                    Editable = PrivilegeNames.IsEditable(id),
                    PrivacySetting = PrivilegeNames.TryGetPrivacySetting(id, out string setting) ? setting : null,
                });
            }

            return states;
        }

        /// <summary>
        /// Refuses a change to a privilege the account may not edit, because the service rejects
        /// one with a bare HTTP 400 that carries no explanation.
        /// </summary>
        /// <param name="privileges">The privilege numbers the caller asked to change.</param>
        /// <param name="action">Either "block" or "allow".</param>
        /// <returns>True when every privilege can be changed by the account itself.</returns>
        private static bool ValidateEditable(IEnumerable<int> privileges, string action)
        {
            List<int> notEditable = privileges.Where(id => !PrivilegeNames.IsEditable(id)).ToList();
            if (notEditable.Count == 0)
            {
                return true;
            }

            Console.Error.WriteLine($"Error: {DescribePrivileges(notEditable)} cannot be changed by the account itself.");
            Console.Error.WriteLine($"Only {DescribePrivileges(PrivilegeNames.Editable)} can be blocked and allowed this way.");

            // Where the privilege is derived from a privacy setting, changing that setting is what
            // the caller actually wants, so point at it rather than just refusing.
            bool suggested = false;
            foreach (int id in notEditable)
            {
                if (PrivilegeNames.TryGetPrivacySetting(id, out string setting))
                {
                    string want = action == "block" ? nameof(PrivacyValue.Blocked) : nameof(PrivacyValue.Everyone);
                    Console.Error.WriteLine();
                    Console.Error.WriteLine($"{PrivilegeNames.Describe(id)} follows a privacy setting. To {action} it, run:");
                    Console.Error.WriteLine($"    {ToolName} privacy set {setting} {want}");
                    suggested = true;
                }
            }

            if (!suggested)
            {
                Console.Error.WriteLine("The remaining privileges are either fixed by the service or derived from another privilege.");
            }

            return false;
        }

        /// <summary>
        /// Loads the signed in test account, reporting the same guidance when there is none.
        /// </summary>
        private static TestAccount LoadSignedInTestAccount()
        {
            TestAccount testAccount = ToolAuthentication.LoadLastSignedInTestAccount();
            if (testAccount == null)
            {
                Console.Error.WriteLine($"No signed in test account found. Run \"{ToolName} signin\" first.");
            }

            return testAccount;
        }

        /// <summary>
        /// Reports that a user token could not be minted for the signed in test account. The
        /// sandbox is no longer a choice the caller makes, so the account or its cached credential
        /// is what needs attention.
        /// </summary>
        /// <param name="exception">The failure raised while minting the token.</param>
        /// <returns>The exit code for a failed command.</returns>
        private static int ReportTokenFailure(TestAccountTokenException exception)
        {
            Console.Error.WriteLine($"Error: could not obtain a token for the signed in test account in sandbox {exception.Sandbox}.");
            Console.Error.WriteLine("The cached credential may have expired, or the account may no longer be able to sign in to that sandbox.");
            Console.Error.WriteLine($"Run \"{ToolName} signin -u <name> -s <sandbox>\" to sign it in again.");

            string detail = exception.InnerException?.Message;
            if (!string.IsNullOrWhiteSpace(detail))
            {
                Console.Error.WriteLine(detail);
            }

            return -1;
        }

        /// <summary>
        /// Gets the sandbox to work against, which is always the one the account signed in to.
        /// </summary>
        /// <remarks>
        /// The parental and privacy services are keyed by XUID and neither endpoint takes a
        /// sandbox, so there is nothing for the caller to choose. The sandbox is still needed to
        /// mint the user token that authenticates the call.
        /// </remarks>
        /// <param name="testAccount">The signed in test account.</param>
        /// <param name="sandbox">Receives the sandbox to use.</param>
        /// <returns>True when a sandbox could be resolved.</returns>
        private static bool TryResolveSandbox(TestAccount testAccount, out string sandbox)
        {
            sandbox = testAccount.Sandbox;

            if (string.IsNullOrEmpty(sandbox))
            {
                Console.Error.WriteLine("Error: the signed in test account does not record a sandbox.");
                Console.Error.WriteLine($"Run \"{ToolName} signin\" again to sign it in.");
                return false;
            }

            return true;
        }

        private static string NormalizeValue(string value)
        {
            // Accept the shorthand a caller is likely to reach for as well as the service's own names.
            switch (value.Trim().ToLowerInvariant())
            {
                case "block":
                    return nameof(PrivacyValue.Blocked);
                case "friends":
                case "friendsonly":
                case "people":
                    return nameof(PrivacyValue.PeopleOnMyList);
                case "all":
                    return nameof(PrivacyValue.Everyone);
                default:
                    return value.Trim();
            }
        }

        /// <summary>
        /// Parses an enum by name only. Enum.TryParse would otherwise accept a numeric string such
        /// as "1" and select a member by its ordinal, silently turning a typo into a valid command.
        /// </summary>
        /// <typeparam name="T">The enum type to parse.</typeparam>
        /// <param name="value">The word supplied on the command line.</param>
        /// <param name="parsed">Receives the parsed value.</param>
        /// <returns>True when the value matched one of the enum names.</returns>
        private static bool TryParseEnumName<T>(string value, out T parsed)
            where T : struct
        {
            parsed = default(T);

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            foreach (T candidate in Enum.GetValues(typeof(T)))
            {
                if (string.Equals(candidate.ToString(), value.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    parsed = candidate;
                    return true;
                }
            }

            return false;
        }

        private static string DescribePrivileges(IEnumerable<int> privileges)
        {
            return string.Join(", ", privileges.Select(PrivilegeNames.Describe));
        }

        /// <summary>
        /// Renders a privilege id right aligned to the width of the ids the service uses, so that
        /// a shorter id still lines up with the rest of the column.
        /// </summary>
        private static string FormatId(int id)
        {
            return id.ToString().PadLeft(3);
        }

        /// <summary>
        /// Describes where a privilege is controlled from, for the listing and the show verb.
        /// </summary>
        /// <param name="id">The privilege id.</param>
        /// <returns>A note such as "(editable)", or an empty string when the service fixes it.</returns>
        private static string DescribePrivilegeSource(int id)
        {
            if (PrivilegeNames.IsEditable(id))
            {
                return "(editable)";
            }

            if (PrivilegeNames.TryGetPrivacySetting(id, out string setting))
            {
                return $"(set with: privacy set {setting})";
            }

            return string.Empty;
        }

        /// <summary>
        /// Reads the privilege ids out of a token claim, which holds them as a space separated list.
        /// </summary>
        private static IEnumerable<int> ParsePrivilegeString(string claim)
        {
            if (string.IsNullOrWhiteSpace(claim))
            {
                yield break;
            }

            foreach (string part in claim.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(part, out int id))
                {
                    yield return id;
                }
            }
        }

        private static void DisplayTestAccount(TestAccount testAccount, string indent)
        {
            Console.WriteLine($"{indent}Gamertag : {testAccount.Gamertag}");
            Console.WriteLine($"{indent}XUID : {testAccount.Xuid}");
            Console.WriteLine($"{indent}Sandbox : {testAccount.Sandbox}");
            Console.WriteLine($"{indent}Age Group : {testAccount.AgeGroup}");
        }

        /// <summary>
        /// Reports every privacy setting the service returned, noting which privilege each controls.
        /// </summary>
        /// <param name="settings">The settings as reported by the service.</param>
        private static void DisplayPrivacySettings(IDictionary<string, string> settings)
        {
            if (settings.Count == 0)
            {
                Console.WriteLine("    (none)");
                return;
            }

            int nameWidth = settings.Keys.Max(name => name.Length);
            int valueWidth = settings.Values.Max(value => value.Length);

            foreach (var entry in settings)
            {
                string note = PrivilegeNames.TryGetPrivilegeForSetting(entry.Key, out int privilege)
                    ? $"(controls privilege {privilege})"
                    : string.Empty;

                string value = note.Length > 0 ? entry.Value.PadRight(valueWidth) : entry.Value;
                Console.WriteLine($"    {entry.Key.PadRight(nameWidth)}  {value}  {note}".TrimEnd());
            }
        }

        /// <summary>
        /// Reports one people list, with any note for each person.
        /// </summary>
        /// <param name="heading">The name of the list.</param>
        /// <param name="people">The XUIDs in the list, each with a note that may be empty.</param>
        private static void DisplayPeople(string heading, IList<KeyValuePair<string, string>> people)
        {
            Console.WriteLine();
            Console.WriteLine($"{heading} ({people.Count}):");

            if (people.Count == 0)
            {
                Console.WriteLine("    (none)");
                return;
            }

            int xuidWidth = people.Max(person => person.Key.Length);

            foreach (KeyValuePair<string, string> person in people)
            {
                Console.WriteLine($"    {person.Key.PadRight(xuidWidth)}  {person.Value}".TrimEnd());
            }
        }

        private static void WriteJson(object payload)
        {
            Console.WriteLine(JsonConvert.SerializeObject(payload, Formatting.Indented));
        }

        /// <summary>
        /// Parses the options of one command, reporting anything the parser rejected.
        /// </summary>
        /// <typeparam name="T">The option class of the command.</typeparam>
        /// <param name="args">The arguments left after the command and any sub command.</param>
        /// <param name="options">Receives the parsed options.</param>
        /// <returns>True when the arguments parsed.</returns>
        private static bool TryParse<T>(string[] args, out T options)
            where T : new()
        {
            T parsed = default(T);
            bool succeeded = false;

            CommandParser.ParseArguments<T>(args)
                .WithParsed(result =>
                {
                    parsed = result;
                    succeeded = true;
                })
                .WithNotParsed(ReportParseErrors);

            options = parsed;
            return succeeded;
        }

        /// <summary>
        /// Reports why the arguments of a command were rejected.
        /// </summary>
        /// <param name="errors">The errors the parser raised.</param>
        private static void ReportParseErrors(IEnumerable<Error> errors)
        {
            foreach (Error error in errors)
            {
                switch (error)
                {
                    case UnknownOptionError unknown:
                        Console.Error.WriteLine($"Error: unknown option {FormatUnknownOption(unknown.Token)}.");
                        break;
                    case MissingRequiredOptionError missing:
                        Console.Error.WriteLine($"Error: required option {FormatOptionName(missing.NameInfo)} is missing.");
                        break;
                    case MissingValueOptionError missingValue:
                        Console.Error.WriteLine($"Error: option {FormatOptionName(missingValue.NameInfo)} needs a value.");
                        break;
                    case BadFormatConversionError badFormat:
                        Console.Error.WriteLine($"Error: option {FormatOptionName(badFormat.NameInfo)} was given a value of the wrong kind.");
                        break;
                    default:
                        Console.Error.WriteLine("Error: the arguments could not be understood.");
                        break;
                }
            }
        }

        /// <summary>
        /// Formats an option the reader typed but that does not exist. The parser hands back the
        /// token with its dashes removed, so they are put back to match what was typed.
        /// </summary>
        /// <param name="token">The option token as the parser reports it, without dashes.</param>
        /// <returns>The option written the way it is typed.</returns>
        private static string FormatUnknownOption(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return "that was given";
            }

            return token.Length == 1 ? "-" + token : "--" + token;
        }

        /// <summary>
        /// Formats an option for a message the way it is typed, so that the reader can copy it
        /// straight on to the command line. The parser reports it as "u, name", without the dashes.
        /// </summary>
        /// <param name="name">The names the parser holds for the option.</param>
        /// <returns>The option written as "-u, --name".</returns>
        private static string FormatOptionName(NameInfo name)
        {
            var names = new List<string>();

            if (!string.IsNullOrEmpty(name.ShortName))
            {
                names.Add("-" + name.ShortName);
            }

            if (!string.IsNullOrEmpty(name.LongName))
            {
                names.Add("--" + name.LongName);
            }

            return names.Count == 0 ? "the argument" : string.Join(", ", names);
        }

        /// <summary>
        /// Reports that a command was called wrongly by showing its help, so that the reader can
        /// see what the command takes without having to ask for it in a second run.
        /// </summary>
        /// <param name="writeHelp">Writes the help screen of the command that was called wrongly.</param>
        /// <returns>The exit code for a usage error.</returns>
        private static int UsageError(Func<int> writeHelp)
        {
            Console.Error.WriteLine();
            writeHelp();
            return -1;
        }

        private static bool IsHelpFlag(string argument)
        {
            return string.Equals(argument, "--help", StringComparison.OrdinalIgnoreCase)
                || string.Equals(argument, "-h", StringComparison.OrdinalIgnoreCase)
                || string.Equals(argument, "-?", StringComparison.Ordinal)
                || string.Equals(argument, "/?", StringComparison.Ordinal);
        }

        private static bool IsVersionFlag(string argument)
        {
            return string.Equals(argument, "--version", StringComparison.OrdinalIgnoreCase);
        }

        private static bool WantsHelp(IEnumerable<string> args)
        {
            return args.Any(IsHelpFlag);
        }

        private static void WriteVersion()
        {
            Console.WriteLine($"{ToolName} {Assembly.GetExecutingAssembly().GetName().Version}");
        }

        private static void WriteHeading()
        {
            WriteVersion();
            Console.WriteLine("Copyright (c) Microsoft Corporation. All rights reserved.");
            Console.WriteLine();
        }

        /// <summary>
        /// Writes a help screen as a list of names and descriptions, lined up in a column and
        /// wrapped to the console width, with a blank line between entries.
        /// </summary>
        /// <param name="entries">The names and their descriptions, in the order to write them.</param>
        private static void WriteEntries(IEnumerable<KeyValuePair<string, string>> entries)
        {
            List<KeyValuePair<string, string>> list = entries.ToList();
            if (list.Count == 0)
            {
                return;
            }

            int nameWidth = list.Max(entry => entry.Key.Length);
            string indent = new string(' ', nameWidth + 4);

            foreach (KeyValuePair<string, string> entry in list)
            {
                bool first = true;
                foreach (string text in Wrap(entry.Value, ConsoleWidth() - indent.Length))
                {
                    string line = first ? $"  {entry.Key.PadRight(nameWidth)}  {text}" : indent + text;
                    Console.WriteLine(line.TrimEnd());
                    first = false;
                }

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Breaks a description into lines that fit the space left beside the name column.
        /// </summary>
        /// <param name="text">The description to break up.</param>
        /// <param name="width">The number of characters available on a line.</param>
        /// <returns>The description as one or more lines.</returns>
        private static IEnumerable<string> Wrap(string text, int width)
        {
            if (string.IsNullOrEmpty(text) || width < 20)
            {
                yield return text ?? string.Empty;
                yield break;
            }

            var line = new StringBuilder();
            foreach (string word in text.Split(' '))
            {
                if (line.Length > 0 && line.Length + 1 + word.Length > width)
                {
                    yield return line.ToString();
                    line.Clear();
                }

                if (line.Length > 0)
                {
                    line.Append(' ');
                }

                line.Append(word);
            }

            if (line.Length > 0)
            {
                yield return line.ToString();
            }
        }

        /// <summary>
        /// Gets the width to wrap help at. A redirected console reports no width, so a conventional
        /// one is used instead.
        /// </summary>
        /// <returns>The number of characters available on a line.</returns>
        private static int ConsoleWidth()
        {
            try
            {
                return Console.WindowWidth > 40 ? Console.WindowWidth - 1 : 79;
            }
            catch (System.IO.IOException)
            {
                return 79;
            }
        }

        private static KeyValuePair<string, string> Entry(string name, string description)
        {
            return new KeyValuePair<string, string>(name, description);
        }

        private static void WriteRootHelp()
        {
            WriteHeading();
            Console.WriteLine($"Usage: {ToolName} <command> [options]");
            Console.WriteLine();
            WriteEntries(new[]
            {
                Entry("--help", HelpHelp),
                Entry("--version", VersionHelp),
                Entry("signin", "sign in a test account and cache the credential"),
                Entry("signout", "sign out the signed in test account"),
                Entry("show", "show the signed in test account"),
                Entry("privilege", "show, block or allow privileges on the signed in test account"),
                Entry("privacy", "show or set privacy settings on the signed in test account"),
                Entry("people", "show or change who the signed in test account follows, blocks and mutes"),
            });
        }

        private static int WriteSignInHelp()
        {
            WriteHeading();
            Console.WriteLine($"Usage: {ToolName} signin -u <name> -s <sandbox> [options]");
            Console.WriteLine();
            WriteEntries(new[]
            {
                Entry("-u, --name", "Required. The user name (email address) of the test account."),
                Entry("-s, --sandbox", "Required. The sandbox to sign the test account in to."),
                Entry("-f, --force", "Optional. Ignore any cached credential and always show the sign in UI."),
                Entry("--help", HelpHelp),
            });

            return 0;
        }

        private static int WriteSignOutHelp()
        {
            WriteHeading();
            Console.WriteLine($"Usage: {ToolName} signout");
            Console.WriteLine();
            WriteEntries(new[]
            {
                Entry("--help", HelpHelp),
            });

            return 0;
        }

        private static int WriteShowHelp()
        {
            WriteHeading();
            Console.WriteLine($"Usage: {ToolName} show [options]");
            Console.WriteLine();
            WriteEntries(new[]
            {
                Entry("-j, --json", JsonHelp),
                Entry("--help", HelpHelp),
            });

            return 0;
        }

        private static void WritePrivilegeHelp()
        {
            WriteHeading();
            Console.WriteLine($"Usage: {ToolName} privilege <block|allow|show> [options]");
            Console.WriteLine();
            WriteEntries(new[]
            {
                Entry("--help", HelpHelp),
                Entry("block", "block a privilege"),
                Entry("allow", "allow a privilege"),
                Entry("show (-j)", "show privileges, pass -j to output as parsable json, -b for only the restricted ones"),
            });
        }

        private static int WritePrivilegeChangeHelp(string action)
        {
            WriteHeading();
            Console.WriteLine($"Usage: {ToolName} privilege {action} <privilegenumber> [privilegenumber...] [options]");
            Console.WriteLine();
            WriteEntries(new[]
            {
                Entry("privilegenumber", "i.e. 254"),
                Entry("--help", HelpHelp),
            });

            return 0;
        }

        private static int WritePrivilegeShowHelp()
        {
            WriteHeading();
            Console.WriteLine($"Usage: {ToolName} privilege show [options]");
            Console.WriteLine();
            WriteEntries(new[]
            {
                Entry("-j, --json", JsonHelp),
                Entry("-b, --blocked", "Optional. Report only the privileges that are restricted."),
                Entry("-r, --refresh", "Optional. Mint a new token so that the privileges are reported as they are now, rather than as they were at sign in."),
                Entry("--help", HelpHelp),
            });

            string[] notes =
            {
                "Only the privileges marked editable can be blocked and allowed by the account itself.",
                "A privilege marked \"set with\" follows a privacy setting, so change that setting instead.",
                "The rest are fixed by the service, for a reason such as the age group of the account.",
            };

            foreach (string note in notes)
            {
                foreach (string line in Wrap(note, ConsoleWidth()))
                {
                    Console.WriteLine(line);
                }
            }

            return 0;
        }

        private static void WritePrivacyHelp()
        {
            WriteHeading();
            Console.WriteLine($"Usage: {ToolName} privacy <set|show> [options]");
            Console.WriteLine();
            WriteEntries(new[]
            {
                Entry("--help", HelpHelp),
                Entry("set", "set a privacy setting"),
                Entry("show (-j)", "show privacy settings, pass -j to output as parsable json"),
            });
        }

        private static int WritePrivacySetHelp()
        {
            WriteHeading();
            Console.WriteLine($"Usage: {ToolName} privacy set <setting> <value> [options]");
            Console.WriteLine();
            WriteEntries(new[]
            {
                Entry("setting", "i.e. CommunicateUsingTextAndVoice"),
                Entry("value", $"i.e. Blocked. One of {string.Join(", ", Enum.GetNames(typeof(PrivacyValue)))}."),
                Entry("--help", HelpHelp),
            });

            return 0;
        }

        private static int WritePrivacyShowHelp()
        {
            WriteHeading();
            Console.WriteLine($"Usage: {ToolName} privacy show [options]");
            Console.WriteLine();
            WriteEntries(new[]
            {
                Entry("-j, --json", JsonHelp),
                Entry("--help", HelpHelp),
            });

            return 0;
        }

        private static void WritePeopleHelp()
        {
            WriteHeading();
            Console.WriteLine($"Usage: {ToolName} people <action> [options]");
            Console.WriteLine();

            var entries = new List<KeyValuePair<string, string>>
            {
                Entry("--help", HelpHelp),
                Entry("show (-j)", "show who the account follows, blocks and mutes, pass -j to output as parsable json"),
            };

            foreach (PeopleAction action in Enum.GetValues(typeof(PeopleAction)))
            {
                entries.Add(Entry(action.ToString().ToLowerInvariant(), DescribePeopleAction(action)));
            }

            WriteEntries(entries);
        }

        private static int WritePeopleShowHelp()
        {
            WriteHeading();
            Console.WriteLine($"Usage: {ToolName} people show [options]");
            Console.WriteLine();
            WriteEntries(new[]
            {
                Entry("--following", "Optional. Report only the people the account follows."),
                Entry("-b, --blocked", "Optional. Report only the people the account has blocked."),
                Entry("-m, --muted", "Optional. Report only the people the account has muted."),
                Entry("-j, --json", JsonHelp),
                Entry("--help", HelpHelp),
            });

            foreach (string line in Wrap("With none of --following, --blocked or --muted, every list is reported.", ConsoleWidth()))
            {
                Console.WriteLine(line);
            }

            return 0;
        }

        private static int WritePeopleChangeHelp(PeopleAction action)
        {
            string verb = action.ToString().ToLowerInvariant();

            WriteHeading();
            Console.WriteLine($"Usage: {ToolName} people {verb} <xuid> [xuid...] [options]");
            Console.WriteLine($"       {ToolName} people {verb} -f <file> [options]");
            Console.WriteLine();
            WriteEntries(new[]
            {
                Entry("xuid", "i.e. 2814634367189023"),
                Entry("-f, --file", "Optional. A file of XUIDs: a plain list separated by spaces, commas or new lines, or a CSV export of test accounts from Partner Center, read by its Xuid column."),
                Entry("--help", HelpHelp),
            });

            var notes = new List<string>();
            int delay = PeopleClient.GetBatchDelayMilliseconds(action);
            if (delay > 0)
            {
                notes.Add($"Up to {PeopleClient.GetBatchSize(action)} people are sent per request, {delay / 1000} seconds apart to stay inside the service's rate limit. Press Ctrl+C to stop between requests.");
            }

            if (action == PeopleAction.Follow)
            {
                notes.Add($"The service lets an account follow no more than {PeopleClient.FollowLimit} people in 12 hours.");
            }

            if (action == PeopleAction.Follow || action == PeopleAction.Friend)
            {
                notes.Add($"People the account has blocked are skipped, since {verb}ing them would remove the block. Unblock them first to {verb} them.");
            }

            foreach (string note in notes)
            {
                foreach (string line in Wrap(note, ConsoleWidth()))
                {
                    Console.WriteLine(line);
                }
            }

            return 0;
        }

        private static string DescribePeopleAction(PeopleAction action)
        {
            switch (action)
            {
                case PeopleAction.Follow:
                    return "follow people";
                case PeopleAction.Unfollow:
                    return "stop following people";
                case PeopleAction.Friend:
                    return "make people mutual friends, so that each follows the other";
                case PeopleAction.Favorite:
                    return "mark people the account follows as favorites";
                case PeopleAction.Unfavorite:
                    return "clear the favorite mark from people";
                case PeopleAction.Block:
                    return "block people";
                case PeopleAction.Unblock:
                    return "unblock people";
                case PeopleAction.Mute:
                    return "mute people";
                case PeopleAction.Unmute:
                    return "unmute people";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// The state of one privilege on the signed in account.
        /// </summary>
        private class PrivilegeState
        {
            /// <summary>
            /// Gets or sets the privilege id.
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether this tool knows the name of the privilege.
            /// </summary>
            public bool Known { get; set; }

            /// <summary>
            /// Gets or sets the friendly name, or a stand in when the privilege is not known.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the state the account holds the privilege in.
            /// </summary>
            public string State { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the account may change the privilege itself.
            /// </summary>
            public bool Editable { get; set; }

            /// <summary>
            /// Gets or sets the privacy setting that controls the privilege, where one does.
            /// </summary>
            public string PrivacySetting { get; set; }
        }

        /// <summary>
        /// The options of the signin command.
        /// </summary>
        private class SignInOptions
        {
            /// <summary>
            /// Gets or sets the user name of the test account.
            /// </summary>
            [Option('u', "name", Required = true,
                HelpText = "The user name (email address) of the test account.")]
            public string UserName { get; set; }

            /// <summary>
            /// Gets or sets the sandbox to sign the test account in to.
            /// </summary>
            [Option('s', "sandbox", Required = true,
                HelpText = "The sandbox to sign the test account in to.")]
            public string Sandbox { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to ignore any cached credential.
            /// </summary>
            [Option('f', "force", Required = false,
                HelpText = "Ignore any cached credential and always show the sign in UI.")]
            public bool Force { get; set; }
        }

        /// <summary>
        /// The options of the signout command, which takes none.
        /// </summary>
        private class SignOutOptions
        {
        }

        /// <summary>
        /// The options of the show command.
        /// </summary>
        private class ShowOptions
        {
            /// <summary>
            /// Gets or sets a value indicating whether to write the output as json.
            /// </summary>
            [Option('j', "json", Required = false, HelpText = JsonHelp)]
            public bool Json { get; set; }
        }

        /// <summary>
        /// The options of the privilege show command.
        /// </summary>
        private class PrivilegeShowOptions
        {
            /// <summary>
            /// Gets or sets a value indicating whether to write the output as json.
            /// </summary>
            [Option('j', "json", Required = false, HelpText = JsonHelp)]
            public bool Json { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to report only the restricted privileges.
            /// </summary>
            [Option('b', "blocked", Required = false,
                HelpText = "Optional. Report only the privileges that are restricted.")]
            public bool Blocked { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to mint a new token first.
            /// </summary>
            [Option('r', "refresh", Required = false,
                HelpText = "Mint a new token so that the privileges are reported as they are now.")]
            public bool Refresh { get; set; }
        }

        /// <summary>
        /// The options of the privilege block and privilege allow commands.
        /// </summary>
        private class PrivilegeChangeOptions
        {
            /// <summary>
            /// Gets or sets the privilege numbers to change.
            /// </summary>
            /// <remarks>
            /// Not marked required, so that an empty list reaches the command and is refused with
            /// guidance on where to find the numbers, rather than by the parser with a bare note
            /// that a nameless value is missing. Taken as text rather than as numbers for the same
            /// reason: the parser has no name for a positional value, so it can only report that
            /// something somewhere was of the wrong kind, whereas the command can name the value.
            /// </remarks>
            [Value(0, MetaName = "privilegenumber", Required = false,
                HelpText = "The privilege numbers to change, for example 185 254.")]
            public IEnumerable<string> Privileges { get; set; }
        }

        /// <summary>
        /// The options of the privacy show command.
        /// </summary>
        private class PrivacyShowOptions
        {
            /// <summary>
            /// Gets or sets a value indicating whether to write the output as json.
            /// </summary>
            [Option('j', "json", Required = false, HelpText = JsonHelp)]
            public bool Json { get; set; }
        }

        /// <summary>
        /// The options of the privacy set command.
        /// </summary>
        private class PrivacySetOptions
        {
            /// <summary>
            /// Gets or sets the privacy setting to change.
            /// </summary>
            /// <remarks>
            /// Not marked required, so that a missing setting reaches the command and is refused
            /// with an example, rather than by the parser with a bare note that a nameless value
            /// is missing.
            /// </remarks>
            [Value(0, MetaName = "setting", Required = false,
                HelpText = "The privacy setting to change, as named by \"privacy show\".")]
            public string Setting { get; set; }

            /// <summary>
            /// Gets or sets the value to set.
            /// </summary>
            [Value(1, MetaName = "value", Required = false,
                HelpText = "The value to set: Everyone, PeopleOnMyList or Blocked.")]
            public string Value { get; set; }

            /// <summary>
            /// Gets or sets any positional arguments past the ones this command takes.
            /// </summary>
            /// <remarks>
            /// CommandLineParser silently drops any positional argument past the last one declared,
            /// so a trailing sequence is declared to catch the surplus and refuse the command rather
            /// than acting on part of it.
            /// </remarks>
            [Value(2, Hidden = true)]
            public IEnumerable<string> Surplus { get; set; }
        }

        /// <summary>
        /// The options of the people show command.
        /// </summary>
        private class PeopleShowOptions
        {
            /// <summary>
            /// Gets or sets a value indicating whether to report the people the account follows.
            /// </summary>
            [Option("following", Required = false,
                HelpText = "Optional. Report only the people the account follows.")]
            public bool Following { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to report the people the account has blocked.
            /// </summary>
            [Option('b', "blocked", Required = false,
                HelpText = "Optional. Report only the people the account has blocked.")]
            public bool Blocked { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to report the people the account has muted.
            /// </summary>
            [Option('m', "muted", Required = false,
                HelpText = "Optional. Report only the people the account has muted.")]
            public bool Muted { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to write the output as json.
            /// </summary>
            [Option('j', "json", Required = false, HelpText = JsonHelp)]
            public bool Json { get; set; }
        }

        /// <summary>
        /// The options of the people commands that change a list.
        /// </summary>
        private class PeopleChangeOptions
        {
            /// <summary>
            /// Gets or sets the XUIDs given on the command line.
            /// </summary>
            /// <remarks>
            /// Taken as text and not required, for the same reasons as the privilege numbers: so
            /// that the command can name a bad value and explain an empty list.
            /// </remarks>
            [Value(0, MetaName = "xuid", Required = false,
                HelpText = "The XUIDs of the people to change.")]
            public IEnumerable<string> Xuids { get; set; }

            /// <summary>
            /// Gets or sets a file of XUIDs.
            /// </summary>
            [Option('f', "file", Required = false,
                HelpText = "A file of XUIDs, either a plain list or a Partner Center CSV export.")]
            public string File { get; set; }
        }
    }
}
