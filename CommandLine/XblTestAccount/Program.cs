// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblTestAccount
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using CommandLine;
    using CommandLine.Text;
    using Microsoft.Xbox.Services.DevTools.Authentication;

    internal class Program
    {
        private const string UnknownPrivilegeName = "(unknown)";

        /// <summary>
        /// The actions the privilege verb takes.
        /// </summary>
        private enum PrivilegeAction
        {
            /// <summary>
            /// List every privilege id this tool knows the name of.
            /// </summary>
            ListAll,

            /// <summary>
            /// Restrict the given privileges.
            /// </summary>
            Block,

            /// <summary>
            /// Clear the restriction on the given privileges.
            /// </summary>
            Allow,
        }

        /// <summary>
        /// The actions the privacy verb takes.
        /// </summary>
        private enum PrivacyAction
        {
            /// <summary>
            /// List every privacy setting the service exposes, with its current value.
            /// </summary>
            ListAll,

            /// <summary>
            /// Change one privacy setting.
            /// </summary>
            Set,
        }

        private static async Task<int> Main(string[] args)
        {
            int exitCode = 0;
            try
            {
                string invokedVerb = string.Empty;
                SignInOptions signInOptions = null;
                ShowOptions showOptions = null;
                PrivilegeOptions privilegeOptions = null;
                PrivacyOptions privacyOptions = null;

                // Only assign the option and verb here, as the commandlineParser doesn't support async callback yet.
                var result = Parser.Default.ParseArguments<SignInOptions, SignOutOptions, ShowOptions, PrivilegeOptions, PrivacyOptions>(args)
                    .WithParsed<SignInOptions>(options =>
                    {
                        invokedVerb = "signin";
                        signInOptions = options;
                    })
                    .WithParsed<SignOutOptions>(options => exitCode = OnSignOut())
                    .WithParsed<ShowOptions>(options =>
                    {
                        invokedVerb = "show";
                        showOptions = options;
                    })
                    .WithParsed<PrivilegeOptions>(options =>
                    {
                        invokedVerb = "privilege";
                        privilegeOptions = options;
                    })
                    .WithParsed<PrivacyOptions>(options =>
                    {
                        invokedVerb = "privacy";
                        privacyOptions = options;
                    })
                    .WithNotParsed(errors => exitCode = IsHelpOrVersionRequest(errors) ? 0 : -1);

                if (invokedVerb == "signin" && signInOptions != null)
                {
                    exitCode = await OnSignIn(signInOptions);
                }
                else if (invokedVerb == "show" && showOptions != null)
                {
                    exitCode = await OnShow(showOptions);
                }
                else if (invokedVerb == "privilege" && privilegeOptions != null)
                {
                    exitCode = await OnPrivilege(privilegeOptions);
                }
                else if (invokedVerb == "privacy" && privacyOptions != null)
                {
                    exitCode = await OnPrivacy(privacyOptions);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                exitCode = -1;
            }

            return exitCode;
        }

        /// <summary>
        /// Gets a value indicating whether the parser stopped because help or version was asked
        /// for, which is a successful run rather than a usage error.
        /// </summary>
        private static bool IsHelpOrVersionRequest(IEnumerable<Error> errors)
        {
            return errors != null
                && errors.Any()
                && errors.All(error => error.Tag == ErrorType.HelpRequestedError
                    || error.Tag == ErrorType.HelpVerbRequestedError
                    || error.Tag == ErrorType.VersionRequestedError);
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
                Console.WriteLine("Run \"XblTestAccount show\" for its privileges and privacy settings.");
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
        /// Reports everything known about the signed in account: who it is, the state of every
        /// privilege it holds, and the value of every privacy setting the service exposes.
        /// </summary>
        private static async Task<int> OnShow(ShowOptions options)
        {
            TestAccount testAccount = LoadSignedInTestAccount();
            if (testAccount == null)
            {
                return -1;
            }

            if (!TryResolveSandbox(testAccount, options.Sandbox, out string sandbox))
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
                    Console.Error.WriteLine("Error: could not refresh the test account claims.");
                    Console.Error.WriteLine(ex.Message);
                    return -1;
                }
            }

            Console.WriteLine($"Test account {testAccount.UserName} is currently signed in.");
            DisplayTestAccount(testAccount, "\t");

            Console.WriteLine();
            DisplayPrivileges(testAccount);

            Console.WriteLine();

            // The privacy settings are not carried on the token, so they cost a service call. A
            // failure here is reported but does not fail the verb, as the account itself was shown.
            try
            {
                IDictionary<string, string> settings = await PrivacyClient.GetSettingsAsync(sandbox, testAccount.Xuid);
                DisplayPrivacySettings(settings);
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine("Warning: could not read the privacy settings.");
                Console.Error.WriteLine(ex.Message);
            }

            if (!options.Refresh)
            {
                Console.WriteLine();
                Console.WriteLine("Privileges are read from the cached token. Add --refresh to report them as they are now.");
            }

            return 0;
        }

        private static async Task<int> OnPrivilege(PrivilegeOptions options)
        {
            // The action is a positional word rather than an enum option: CommandLineParser 2.2.1
            // matches enum values case sensitively and reports a bare "bad format" error on a
            // mismatch, so it is parsed here where a useful message can be given.
            if (!TryParseEnumName(options.Action, out PrivilegeAction action))
            {
                Console.Error.WriteLine($"Error: unknown action \"{options.Action}\". Expected {DescribeActions<PrivilegeAction>()}.");
                return -1;
            }

            List<int> privileges = options.Privileges?.ToList() ?? new List<int>();

            if (action == PrivilegeAction.ListAll)
            {
                if (privileges.Count > 0)
                {
                    Console.Error.WriteLine("Error: the listall action does not take a privilege list.");
                    return -1;
                }

                return ListPrivileges();
            }

            if (privileges.Count == 0)
            {
                Console.Error.WriteLine($"Error: the {ActionWord(action)} action requires at least one privilege id, for example \"XblTestAccount privilege {ActionWord(action)} 185\".");
                Console.Error.WriteLine("Run \"XblTestAccount privilege listall\" to see the ids.");
                return -1;
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

            if (!TryResolveSandbox(testAccount, options.Sandbox, out string sandbox))
            {
                return -1;
            }

            // The parental service only accepts Actor=Self, so the account being changed is always
            // the one that owns the token.
            string xuid = testAccount.Xuid;

            try
            {
                string verb = action == PrivilegeAction.Block ? "Restricting" : "Unrestricting";
                Console.WriteLine($"{verb} {DescribePrivileges(privileges)} on {testAccount.Gamertag} ({xuid}) in sandbox {sandbox}.");

                IList<int> restricted = await PrivilegeClient.SetRestrictionsAsync(
                    sandbox, xuid, privileges, action == PrivilegeAction.Block);

                Console.WriteLine($"Privileges now restricted by the parental service for {testAccount.Gamertag} ({xuid}):");
                if (restricted.Count == 0)
                {
                    Console.WriteLine("    (none)");
                }
                else
                {
                    int nameWidth = restricted.Max(id => PrivilegeNames.GetName(id, UnknownPrivilegeName).Length);
                    foreach (int privilege in restricted.OrderBy(id => id))
                    {
                        Console.WriteLine($"    {FormatId(privilege)}  {PrivilegeNames.GetName(privilege, UnknownPrivilegeName).PadRight(nameWidth)}");
                    }
                }

                Console.WriteLine();
                Console.WriteLine("This is only what the parental service holds. Run \"XblTestAccount show --refresh\" for the effective set.");
                return 0;
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine("Error: the privilege call failed.");
                Console.Error.WriteLine(ex.Message);
                return -1;
            }
        }

        private static async Task<int> OnPrivacy(PrivacyOptions options)
        {
            if (!TryParseEnumName(options.Action, out PrivacyAction action))
            {
                Console.Error.WriteLine($"Error: unknown action \"{options.Action}\". Expected {DescribeActions<PrivacyAction>()}.");
                return -1;
            }

            // The arguments are checked before the account is loaded and the service is called, so
            // that a usage mistake is reported without a round trip.
            if (options.Surplus != null && options.Surplus.Any())
            {
                Console.Error.WriteLine($"Error: unexpected argument \"{options.Surplus.First()}\". A setting name or value containing a space must be quoted.");
                return -1;
            }

            PrivacyValue value = default(PrivacyValue);
            if (action == PrivacyAction.ListAll)
            {
                if (!string.IsNullOrWhiteSpace(options.Setting) || !string.IsNullOrWhiteSpace(options.Value))
                {
                    Console.Error.WriteLine("Error: the listall action does not take a setting or a value.");
                    return -1;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(options.Setting) || string.IsNullOrWhiteSpace(options.Value))
                {
                    Console.Error.WriteLine("Error: the set action requires a setting and a value, for example \"XblTestAccount privacy set CommunicateDuringCrossNetworkPlay Blocked\".");
                    Console.Error.WriteLine("Run \"XblTestAccount privacy listall\" to see the settings.");
                    return -1;
                }

                if (!TryParseEnumName(NormalizeValue(options.Value), out value))
                {
                    Console.Error.WriteLine($"Error: unknown value \"{options.Value}\". Expected {string.Join(", ", Enum.GetNames(typeof(PrivacyValue)))}.");
                    return -1;
                }
            }

            TestAccount testAccount = LoadSignedInTestAccount();
            if (testAccount == null)
            {
                return -1;
            }

            if (!TryResolveSandbox(testAccount, options.Sandbox, out string sandbox))
            {
                return -1;
            }

            // The write goes through /users/me, so the account changed is always the token owner.
            string xuid = testAccount.Xuid;

            try
            {
                IDictionary<string, string> settings = await PrivacyClient.GetSettingsAsync(sandbox, xuid);

                if (action == PrivacyAction.ListAll)
                {
                    Console.WriteLine($"Privacy settings for {testAccount.Gamertag} ({xuid}):");
                    DisplayPrivacySettings(settings, string.Empty);
                    return 0;
                }

                // The service is the authority on which settings exist, so the name is checked
                // against the set it just reported rather than a list held by this tool.
                string setting = PrivacyClient.ResolveSettingName(settings, options.Setting);
                if (setting == null)
                {
                    Console.Error.WriteLine($"Error: the service does not expose a privacy setting named \"{options.Setting}\".");
                    Console.Error.WriteLine($"The settings available on this account are: {string.Join(", ", settings.Keys)}.");
                    return -1;
                }

                Console.WriteLine($"Setting {setting} to {value} on {testAccount.Gamertag} ({xuid}) in sandbox {sandbox}.");
                settings = await PrivacyClient.SetSettingAsync(sandbox, xuid, setting, value);

                settings.TryGetValue(setting, out string applied);
                if (string.Equals(applied, value.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"{setting} is now {applied}.");
                }
                else
                {
                    Console.Error.WriteLine($"Warning: the service accepted the change but still reports {setting} as {applied ?? "missing"}.");
                    Console.Error.WriteLine("It can take a moment to become visible. Run \"XblTestAccount privacy listall\" again to confirm.");
                }

                if (PrivilegeNames.TryGetPrivilegeForSetting(setting, out int privilege))
                {
                    Console.WriteLine($"This setting controls privilege {PrivilegeNames.Describe(privilege)}. Run \"XblTestAccount show --refresh\" to see it take effect.");
                }

                return 0;
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine("Error: the privacy call failed.");
                Console.Error.WriteLine(ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Lists every privilege id this tool knows the name of, marking which can be changed
        /// directly and which follow a privacy setting.
        /// </summary>
        private static int ListPrivileges()
        {
            // Pad to the widest name so the notes line up in a column, rather than relying on tab
            // stops that the varying name lengths push out of alignment.
            int nameWidth = PrivilegeNames.All.Values.Max(name => name.Length);

            Console.WriteLine("Known privilege ids:");
            foreach (var privilege in PrivilegeNames.All.OrderBy(entry => entry.Key))
            {
                string note = DescribePrivilegeSource(privilege.Key);
                string name = note.Length > 0 ? privilege.Value.PadRight(nameWidth) : privilege.Value;
                Console.WriteLine($"    {FormatId(privilege.Key)}  {name}  {note}".TrimEnd());
            }

            Console.WriteLine();
            Console.WriteLine("Only the privileges marked editable can be blocked and allowed by the account itself.");
            Console.WriteLine("A privilege marked \"set with\" follows a privacy setting, so change that setting instead.");
            Console.WriteLine("The rest are fixed by the service, for a reason such as the age group of the account.");
            Console.WriteLine();
            Console.WriteLine("Run \"XblTestAccount show\" to see which of these the signed in account holds.");
            return 0;
        }

        /// <summary>
        /// Refuses a change to a privilege the account may not edit, because the service rejects
        /// one with a bare HTTP 400 that carries no explanation.
        /// </summary>
        /// <param name="privileges">The privilege ids the caller asked to change.</param>
        /// <param name="action">The action the caller asked for.</param>
        /// <returns>True when every privilege can be changed by the account itself.</returns>
        private static bool ValidateEditable(IEnumerable<int> privileges, PrivilegeAction action)
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
                    string want = action == PrivilegeAction.Block ? nameof(PrivacyValue.Blocked) : nameof(PrivacyValue.Everyone);
                    Console.Error.WriteLine();
                    Console.Error.WriteLine($"{PrivilegeNames.Describe(id)} follows a privacy setting. To {ActionWord(action)} it, run:");
                    Console.Error.WriteLine($"    XblTestAccount privacy set {setting} {want}");
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
                Console.Error.WriteLine("No signed in test account found. Run \"XblTestAccount signin\" first.");
            }

            return testAccount;
        }

        /// <summary>
        /// Resolves the sandbox to work against, falling back to the one the account signed in to.
        /// </summary>
        /// <param name="testAccount">The signed in test account.</param>
        /// <param name="requested">The sandbox given on the command line, if any.</param>
        /// <param name="sandbox">Receives the sandbox to use.</param>
        /// <returns>True when a sandbox could be resolved.</returns>
        private static bool TryResolveSandbox(TestAccount testAccount, string requested, out string sandbox)
        {
            sandbox = string.IsNullOrEmpty(requested) ? testAccount.Sandbox : requested;

            if (string.IsNullOrEmpty(sandbox))
            {
                Console.Error.WriteLine("Error: no sandbox was given and the signed in test account does not record one.");
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

        /// <summary>
        /// Renders the actions of a verb as the lowercase words used on the command line.
        /// </summary>
        /// <typeparam name="T">The action enum of the verb.</typeparam>
        /// <returns>A display string such as "listall, block or allow".</returns>
        private static string DescribeActions<T>()
            where T : struct
        {
            string[] names = Enum.GetNames(typeof(T)).Select(name => name.ToLowerInvariant()).ToArray();
            return names.Length < 2
                ? string.Join(string.Empty, names)
                : $"{string.Join(", ", names.Take(names.Length - 1))} or {names.Last()}";
        }

        /// <summary>
        /// Renders an action as the lowercase word used on the command line.
        /// </summary>
        private static string ActionWord(PrivilegeAction action)
        {
            return action.ToString().ToLowerInvariant();
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
        /// Reports every privilege named on the token, as its id, its name, whether it is granted
        /// or restricted, and where it is controlled from.
        /// </summary>
        private static void DisplayPrivileges(TestAccount testAccount)
        {
            var restricted = new SortedSet<int>(ParsePrivilegeString(testAccount.RestrictedPrivilegeString));
            var all = new SortedSet<int>(ParsePrivilegeString(testAccount.PrivilegeString));
            all.UnionWith(restricted);

            Console.WriteLine("Privileges:");
            if (all.Count == 0)
            {
                Console.WriteLine("    (none reported on the token)");
                return;
            }

            // Pad to the widest name and state so the notes line up in a column.
            int nameWidth = all.Max(id => PrivilegeNames.GetName(id, UnknownPrivilegeName).Length);
            int stateWidth = restricted.Count == 0 ? "Granted".Length : "Restricted".Length;

            foreach (int id in all)
            {
                string state = restricted.Contains(id) ? "Restricted" : "Granted";
                string note = DescribePrivilegeSource(id);
                Console.WriteLine($"    {FormatId(id)}  {PrivilegeNames.GetName(id, UnknownPrivilegeName).PadRight(nameWidth)}  {state.PadRight(stateWidth)}  {note}".TrimEnd());
            }
        }

        /// <summary>
        /// Reports every privacy setting the service returned, noting which privilege each controls.
        /// </summary>
        /// <param name="settings">The settings as reported by the service.</param>
        /// <param name="heading">The heading to print, or an empty string for none.</param>
        private static void DisplayPrivacySettings(IDictionary<string, string> settings, string heading = "Privacy settings:")
        {
            if (heading.Length > 0)
            {
                Console.WriteLine(heading);
            }

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

        [Verb("signin", HelpText = "Sign in an Xbox Live test account and cache the credential so that later runs need no UI.")]
        private class SignInOptions
        {
            [Option('u', "name", Required = true,
                HelpText = "The user name (email address) of the test account.")]
            public string UserName { get; set; }

            [Option('s', "sandbox", Required = true,
                HelpText = "The sandbox to sign the test account in to.")]
            public string Sandbox { get; set; }

            [Option('f', "force", Required = false,
                HelpText = "Ignore any cached credential and always show the sign in UI.")]
            public bool Force { get; set; }

            [Usage]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example(
                        "Sign in an Xbox Live test account and cache the credential",
                        new SignInOptions
                        {
                            UserName = "xxxx@xboxtest.com",
                            Sandbox = "XXXXXX.0"
                        });
                }
            }
        }

        [Verb("signout", HelpText = "Sign out the signed in Xbox Live test account.")]
        private class SignOutOptions
        {
            [Usage]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example("Sign out the signed in Xbox Live test account.", new SignOutOptions());
                }
            }
        }

        [Verb("show", HelpText = "Display the signed in Xbox Live test account, its privileges and its privacy settings.")]
        private class ShowOptions
        {
            [Option('r', "refresh", Required = false,
                HelpText = "Mint a new token so that the privilege claims are reported as they are now, rather than as they were at sign in.")]
            public bool Refresh { get; set; }

            [Option('s', "sandbox", Required = false,
                HelpText = "The sandbox to use. Defaults to the sandbox the test account signed in to.")]
            public string Sandbox { get; set; }

            [Usage]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example("Display the test account, its privileges and its privacy settings.", new ShowOptions());
                    yield return new Example("Display it with the privilege claims as they are now.", new ShowOptions { Refresh = true });
                }
            }
        }

        [Verb("privilege", HelpText = "List the known Xbox Live privileges, or block and allow them on the signed in test account.")]
        private class PrivilegeOptions
        {
            [Value(0, MetaName = "action", Required = true,
                HelpText = "The action to take: listall, block or allow.")]
            public string Action { get; set; }

            [Value(1, MetaName = "privileges", Required = false,
                HelpText = "The privilege ids to block or allow, for example 185 254.")]
            public IEnumerable<int> Privileges { get; set; }

            [Option('s', "sandbox", Required = false,
                HelpText = "The sandbox to use. Defaults to the sandbox the test account signed in to.")]
            public string Sandbox { get; set; }

            [Usage]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example(
                        "List the known privilege ids and their names",
                        new PrivilegeOptions { Action = "listall" });

                    yield return new Example(
                        "Restrict cross network play",
                        new PrivilegeOptions { Action = "block", Privileges = new[] { 185 } });

                    yield return new Example(
                        "Clear the restriction on cross network play",
                        new PrivilegeOptions { Action = "allow", Privileges = new[] { 185 } });
                }
            }
        }

        [Verb("privacy", HelpText = "List the privacy settings of the signed in Xbox Live test account, or change one of them.")]
        private class PrivacyOptions
        {
            [Value(0, MetaName = "action", Required = true,
                HelpText = "The action to take: listall or set.")]
            public string Action { get; set; }

            [Value(1, MetaName = "setting", Required = false,
                HelpText = "The privacy setting to change, as named by \"privacy listall\".")]
            public string Setting { get; set; }

            [Value(2, MetaName = "value", Required = false,
                HelpText = "The value to set: Everyone, PeopleOnMyList or Blocked.")]
            public string Value { get; set; }

            // CommandLineParser silently drops any positional argument past the last one declared,
            // so a trailing sequence is declared to catch the surplus and refuse the command rather
            // than acting on part of it.
            [Value(3, Hidden = true)]
            public IEnumerable<string> Surplus { get; set; }

            [Option('s', "sandbox", Required = false,
                HelpText = "The sandbox to use. Defaults to the sandbox the test account signed in to.")]
            public string Sandbox { get; set; }

            [Usage]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example(
                        "List every privacy setting the service exposes, with its current value",
                        new PrivacyOptions { Action = "listall" });

                    yield return new Example(
                        "Block communicating outside of Xbox with voice and text",
                        new PrivacyOptions { Action = "set", Setting = "CommunicateDuringCrossNetworkPlay", Value = "Blocked" });

                    yield return new Example(
                        "Block communicating on Xbox with voice and text (privilege 252)",
                        new PrivacyOptions { Action = "set", Setting = "CommunicateUsingTextAndVoice", Value = "Blocked" });
                }
            }
        }
    }
}
