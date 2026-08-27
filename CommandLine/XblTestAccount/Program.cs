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
        private enum PrivilegeAction
        {
            /// <summary>
            /// Report the privileges currently restricted on the account.
            /// </summary>
            Get,

            /// <summary>
            /// Restrict the given privileges.
            /// </summary>
            Block,

            /// <summary>
            /// Clear the restriction on the given privileges.
            /// </summary>
            Allow,
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
                var result = Parser.Default.ParseArguments<SignInOptions, SignOutOptions, ShowOptions, PrivilegeOptions, ListPrivilegesOptions, PrivacyOptions, ListPrivacySettingsOptions>(args)
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
                    .WithParsed<ListPrivilegesOptions>(options => exitCode = OnListPrivileges())
                    .WithParsed<PrivacyOptions>(options =>
                    {
                        invokedVerb = "privacy";
                        privacyOptions = options;
                    })
                    .WithParsed<ListPrivacySettingsOptions>(options => exitCode = OnListPrivacySettings())
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
                Console.WriteLine("Error: " + ex.Message);
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
                return 0;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Error: test account signin failed");
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

        private static int OnSignOut()
        {
            TestAccount testAccount = ToolAuthentication.LoadLastSignedInTestAccount();
            if (testAccount == null)
            {
                Console.WriteLine("No signed in test account found.");
                return -1;
            }

            ToolAuthentication.SignOutTestAccount();
            Console.WriteLine($"Test account {testAccount.UserName} has successfully signed out.");
            return 0;
        }

        private static async Task<int> OnShow(ShowOptions options)
        {
            TestAccount testAccount = ToolAuthentication.LoadLastSignedInTestAccount();
            if (testAccount == null)
            {
                Console.WriteLine("No signed in test account found.");
                return -1;
            }

            Console.WriteLine($"Test account {testAccount.UserName} is currently signed in.");

            // The cached copy reports the state as it was at sign in, so refreshing is how a
            // privilege or privacy change becomes visible.
            if (options.Refresh)
            {
                string sandbox = string.IsNullOrEmpty(options.Sandbox) ? testAccount.Sandbox : options.Sandbox;
                if (string.IsNullOrEmpty(sandbox))
                {
                    Console.WriteLine("Error: no sandbox was given and the signed in test account does not record one.");
                    return -1;
                }

                try
                {
                    testAccount = await ToolAuthentication.GetTestAccountSilentlyAsync(sandbox, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: could not refresh the test account claims.");
                    Console.WriteLine(ex.Message);
                    return -1;
                }
            }

            DisplayTestAccount(testAccount, "\t");
            return 0;
        }

        private static int OnListPrivileges()
        {
            // Pad to the widest name so the notes line up in a column, rather than relying on tab
            // stops that the varying name lengths push out of alignment.
            int nameWidth = PrivilegeNames.All.Values.Max(name => name.Length);

            Console.WriteLine("Known privilege ids:");
            foreach (var privilege in PrivilegeNames.All.OrderBy(entry => entry.Key))
            {
                string note = string.Empty;
                if (PrivilegeNames.IsEditable(privilege.Key))
                {
                    note = "(editable)";
                }
                else if (PrivilegeNames.TryGetPrivacyEquivalent(privilege.Key, out string alias))
                {
                    note = $"(set with: privacy -n {alias})";
                }

                string name = note.Length > 0 ? privilege.Value.PadRight(nameWidth) : privilege.Value;
                Console.WriteLine($"    {privilege.Key}  {name}  {note}".TrimEnd());
            }

            Console.WriteLine();
            Console.WriteLine("Only the privileges marked editable can be blocked and allowed by the account itself.");
            Console.WriteLine("A privilege marked \"set with\" follows a privacy setting, so change that setting instead.");
            Console.WriteLine("The rest are reported by the get action but are fixed by the service.");
            return 0;
        }

        private static async Task<int> OnPrivilege(PrivilegeOptions options)
        {
            TestAccount testAccount = ToolAuthentication.LoadLastSignedInTestAccount();
            if (testAccount == null)
            {
                Console.WriteLine("No signed in test account found. Run \"XblTestAccount signin\" first.");
                return -1;
            }

            string sandbox = string.IsNullOrEmpty(options.Sandbox) ? testAccount.Sandbox : options.Sandbox;
            if (string.IsNullOrEmpty(sandbox))
            {
                Console.WriteLine("Error: no sandbox was given and the signed in test account does not record one.");
                return -1;
            }

            // The parental service only accepts Actor=Self, so the account being changed is always
            // the one that owns the token.
            string xuid = testAccount.Xuid;

            // The action is a positional word rather than an enum option: CommandLineParser 2.2.1
            // matches enum values case sensitively and reports a bare "bad format" error on a
            // mismatch, so it is parsed here where a useful message can be given.
            if (!TryParseAction(options.Action, out PrivilegeAction action))
            {
                Console.WriteLine($"Error: unknown action \"{options.Action}\". Expected get, block or allow.");
                return -1;
            }

            bool isMutation = action != PrivilegeAction.Get;
            List<int> privileges = options.Privileges?.ToList() ?? new List<int>();

            if (isMutation && privileges.Count == 0)
            {
                Console.WriteLine($"Error: the {ActionWord(action)} action requires at least one privilege, for example \"XblTestAccount privilege {ActionWord(action)} 185\".");
                return -1;
            }

            if (!isMutation && privileges.Count > 0)
            {
                Console.WriteLine("Error: the get action does not take a privilege list.");
                return -1;
            }

            // The service rejects a privilege the account may not edit with a bare HTTP 400 that
            // carries no explanation, so the request is refused here with one instead.
            if (isMutation)
            {
                List<int> notEditable = privileges.Where(id => !PrivilegeNames.IsEditable(id)).ToList();
                if (notEditable.Count > 0)
                {
                    Console.WriteLine($"Error: {DescribePrivileges(notEditable)} cannot be changed by the account itself.");
                    Console.WriteLine($"Only {DescribePrivileges(PrivilegeNames.Editable)} can be blocked and allowed this way.");

                    // Where the privilege is derived from a privacy setting, changing that setting
                    // is what the caller actually wants, so point at it rather than just refusing.
                    bool suggested = false;
                    foreach (int id in notEditable)
                    {
                        if (PrivilegeNames.TryGetPrivacyEquivalent(id, out string alias))
                        {
                            string want = action == PrivilegeAction.Block ? "Blocked" : "Everyone";
                            Console.WriteLine();
                            Console.WriteLine($"{PrivilegeNames.Describe(id)} follows a privacy setting. To {ActionWord(action)} it, run:");
                            Console.WriteLine($"    XblTestAccount privacy -n {alias} -v {want}");
                            suggested = true;
                        }
                    }

                    if (!suggested)
                    {
                        Console.WriteLine("The remaining privileges are either fixed by the service or derived from another privilege.");
                    }

                    return -1;
                }
            }

            try
            {
                IList<int> restricted;
                if (isMutation)
                {
                    string verb = action == PrivilegeAction.Block ? "Restricting" : "Unrestricting";
                    Console.WriteLine($"{verb} {DescribePrivileges(privileges)} on {testAccount.Gamertag} ({xuid}) in sandbox {sandbox}.");
                    restricted = await PrivilegeClient.SetRestrictionsAsync(sandbox, xuid, privileges, action == PrivilegeAction.Block);
                }
                else
                {
                    restricted = await PrivilegeClient.GetRestrictionsAsync(sandbox, xuid);
                }

                // The parental service only reports what it restricted itself. A privilege the
                // service derived from a privacy setting is enforced through the token claims and
                // never appears there, so the token is read as well to report the effective set.
                var parental = new HashSet<int>(restricted);
                var effective = new SortedSet<int>(restricted);
                bool tokenRead = false;

                if (!isMutation)
                {
                    try
                    {
                        TestAccount current = await ToolAuthentication.GetTestAccountSilentlyAsync(sandbox, true);
                        foreach (int id in ParsePrivilegeString(current.RestrictedPrivilegeString))
                        {
                            effective.Add(id);
                        }

                        tokenRead = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: could not read the token claims, reporting the parental service only. {ex.Message}");
                    }
                }

                Console.WriteLine($"Restricted privileges for {testAccount.Gamertag} ({xuid}):");
                if (effective.Count == 0)
                {
                    Console.WriteLine("    (none)");
                }
                else
                {
                    // Pad to the widest name in this listing so the source annotations line up.
                    int nameWidth = effective
                        .Select(id => PrivilegeNames.TryGetName(id, out string known) ? known : "(unknown)")
                        .Max(name => name.Length);

                    foreach (int privilege in effective)
                    {
                        string name = PrivilegeNames.TryGetName(privilege, out string known) ? known : "(unknown)";
                        string source = string.Empty;

                        if (tokenRead)
                        {
                            if (parental.Contains(privilege))
                            {
                                source = "set by the parental service";
                            }
                            else if (PrivilegeNames.TryGetPrivacyEquivalent(privilege, out string alias))
                            {
                                source = $"follows privacy setting {alias}";
                            }
                        }

                        string paddedName = source.Length > 0 ? name.PadRight(nameWidth) : name;
                        Console.WriteLine($"    {privilege}  {paddedName}  {source}".TrimEnd());
                    }
                }

                if (isMutation)
                {
                    Console.WriteLine();
                    Console.WriteLine("This lists what the parental service restricted. Run \"XblTestAccount privilege\" to see the effective set.");
                }

                return 0;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Error: the privilege call failed.");
                Console.WriteLine(ex.Message);
                return -1;
            }
        }

        private static int OnListPrivacySettings()
        {
            // Map each setting to the privilege it controls, so that someone looking for a
            // privilege can find the setting that drives it and the other way round.
            var privilegeForSetting = PrivilegeNames.PrivacyControlled
                .ToDictionary(entry => PrivacyNames.Resolve(entry.Value), entry => entry.Key, StringComparer.OrdinalIgnoreCase);

            Console.WriteLine("Known privacy settings:");
            foreach (var setting in PrivacyNames.All.OrderBy(entry => entry.Key))
            {
                // Show the aliases against the setting they stand for, so that the name to type is
                // visible without cross referencing a separate list.
                string[] aliases = PrivacyNames.Aliases
                    .Where(entry => string.Equals(entry.Value, setting.Key, StringComparison.OrdinalIgnoreCase))
                    .Select(entry => entry.Key)
                    .OrderBy(alias => alias)
                    .ToArray();

                string drives = privilegeForSetting.TryGetValue(setting.Key, out int privilege)
                    ? $"  (controls privilege {privilege})"
                    : string.Empty;

                Console.WriteLine($"    {setting.Key}{drives}");
                if (aliases.Length > 0)
                {
                    Console.WriteLine($"        alias: {string.Join(", ", aliases)}");
                }

                Console.WriteLine($"        {setting.Value}");
            }

            Console.WriteLine();
            Console.WriteLine($"Values: {string.Join(", ", Enum.GetNames(typeof(PrivacyValue)))}");
            return 0;
        }

        private static async Task<int> OnPrivacy(PrivacyOptions options)
        {
            TestAccount testAccount = ToolAuthentication.LoadLastSignedInTestAccount();
            if (testAccount == null)
            {
                Console.WriteLine("No signed in test account found. Run \"XblTestAccount signin\" first.");
                return -1;
            }

            string sandbox = string.IsNullOrEmpty(options.Sandbox) ? testAccount.Sandbox : options.Sandbox;
            if (string.IsNullOrEmpty(sandbox))
            {
                Console.WriteLine("Error: no sandbox was given and the signed in test account does not record one.");
                return -1;
            }

            // The write goes through /users/me, so the account changed is always the token owner.
            string xuid = testAccount.Xuid;
            bool hasSetting = !string.IsNullOrEmpty(options.Setting);
            bool hasValue = !string.IsNullOrEmpty(options.Value);

            // A name on its own narrows the report to that setting. A value on its own has nothing
            // to apply to, so only that combination is rejected.
            if (hasValue && !hasSetting)
            {
                Console.WriteLine("Error: -v needs the setting to change, for example -n cross-network -v Blocked.");
                return -1;
            }

            bool isMutation = hasSetting && hasValue;

            string setting = null;
            PrivacyValue? value = null;

            if (hasSetting)
            {
                setting = PrivacyNames.Resolve(options.Setting);
                if (setting == null)
                {
                    Console.WriteLine($"Error: unknown privacy setting \"{options.Setting}\". Run \"XblTestAccount list-privacy-settings\" to see the known settings.");
                    return -1;
                }
            }

            if (isMutation)
            {
                if (!TryParseEnumName(NormalizeValue(options.Value), out PrivacyValue parsed))
                {
                    Console.WriteLine($"Error: unknown value \"{options.Value}\". Expected {string.Join(", ", Enum.GetNames(typeof(PrivacyValue)))}.");
                    return -1;
                }

                value = parsed;
            }

            try
            {
                IDictionary<string, string> settings;
                if (isMutation)
                {
                    // The service exposes more settings than this tool documents, so the name is
                    // checked against the set the service actually reports rather than a fixed list.
                    IDictionary<string, string> current = await PrivacyClient.GetSettingsAsync(sandbox, xuid);
                    if (!current.ContainsKey(setting))
                    {
                        Console.WriteLine($"Error: the service does not expose a privacy setting named \"{options.Setting}\".");
                        Console.WriteLine($"The settings available on this account are: {string.Join(", ", current.Keys)}.");
                        return -1;
                    }

                    Console.WriteLine($"Setting {setting} to {value.Value} on {testAccount.Gamertag} ({xuid}) in sandbox {sandbox}.");
                    settings = await PrivacyClient.SetSettingAsync(sandbox, xuid, setting, value.Value);
                }
                else
                {
                    settings = await PrivacyClient.GetSettingsAsync(sandbox, xuid);

                    if (hasSetting)
                    {
                        if (!settings.ContainsKey(setting))
                        {
                            Console.WriteLine($"Error: the service does not expose a privacy setting named \"{options.Setting}\".");
                            Console.WriteLine($"The settings available on this account are: {string.Join(", ", settings.Keys)}.");
                            return -1;
                        }

                        settings = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            [setting] = settings[setting],
                        };
                    }
                }

                Console.WriteLine($"Privacy settings for {testAccount.Gamertag} ({xuid}):");
                if (settings.Count == 0)
                {
                    Console.WriteLine("    (none)");
                }
                else
                {
                    // Pad to the widest name so the values line up in a column.
                    int nameWidth = settings.Keys.Max(name => name.Length);
                    foreach (var entry in settings)
                    {
                        Console.WriteLine($"    {entry.Key.PadRight(nameWidth)}  {entry.Value}");
                    }
                }

                if (isMutation
                    && (!settings.TryGetValue(setting, out string applied)
                        || !string.Equals(applied, value.Value.ToString(), StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine();
                    Console.WriteLine($"Warning: the service accepted the change but still reports {setting} as {applied ?? "missing"}.");
                    Console.WriteLine("It can take a moment to become visible. Run \"XblTestAccount privacy\" again to confirm.");
                }

                return 0;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Error: the privacy call failed.");
                Console.WriteLine(ex.Message);
                return -1;
            }
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
        /// Parses the action word of the privilege verb, defaulting to Get when none was given.
        /// </summary>
        /// <param name="value">The action word supplied on the command line.</param>
        /// <param name="action">Receives the parsed action.</param>
        /// <returns>True when the action was recognised.</returns>
        private static bool TryParseAction(string value, out PrivilegeAction action)
        {
            action = PrivilegeAction.Get;
            return string.IsNullOrWhiteSpace(value) || TryParseEnumName(value, out action);
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
            Console.WriteLine($"{indent}Privileges : {testAccount.PrivilegeString}");
            Console.WriteLine($"{indent}Restricted Privileges : {testAccount.RestrictedPrivilegeString}");
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

        [Verb("show", HelpText = "Display the currently signed in Xbox Live test account.")]
        private class ShowOptions
        {
            [Option('r', "refresh", Required = false,
                HelpText = "Mint a new token so that the privilege claims are reported as they are now, rather than as they were at sign in.")]
            public bool Refresh { get; set; }

            [Option('s', "sandbox", Required = false,
                HelpText = "The sandbox to refresh against. Defaults to the sandbox the account signed in to.")]
            public string Sandbox { get; set; }

            [Usage]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example("Display the currently signed in Xbox Live test account.", new ShowOptions());
                    yield return new Example("Display it with the privilege claims as they are now.", new ShowOptions { Refresh = true });
                }
            }
        }

        [Verb("privilege", HelpText = "Report, block or allow privileges on the signed in Xbox Live test account.")]
        private class PrivilegeOptions
        {
            [Value(0, MetaName = "action", Required = false,
                HelpText = "The action to take: get, block or allow. Defaults to get.")]
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
                        "Report the privileges currently restricted on the test account",
                        new PrivilegeOptions());

                    yield return new Example(
                        "Restrict cross network play",
                        new PrivilegeOptions { Action = "block", Privileges = new[] { 185 } });

                    yield return new Example(
                        "Clear the restriction on cross network play",
                        new PrivilegeOptions { Action = "allow", Privileges = new[] { 185 } });
                }
            }
        }

        [Verb("list-privileges", HelpText = "List the known Xbox Live privilege ids and their names.")]
        private class ListPrivilegesOptions
        {
            [Usage]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example("List the known Xbox Live privilege ids and their names.", new ListPrivilegesOptions());
                }
            }
        }

        [Verb("privacy", HelpText = "Report or change the privacy settings of the signed in Xbox Live test account.")]
        private class PrivacyOptions
        {
            [Option('n', "name", Required = false,
                HelpText = "The privacy setting to report or change, either its full name or a short alias such as cross-network. On its own it narrows the report to that setting.")]
            public string Setting { get; set; }

            [Option('v', "value", Required = false,
                HelpText = "The value to set: Everyone, PeopleOnMyList or Blocked.")]
            public string Value { get; set; }

            [Option('s', "sandbox", Required = false,
                HelpText = "The sandbox to use. Defaults to the sandbox the test account signed in to.")]
            public string Sandbox { get; set; }

            [Usage]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example(
                        "Report the privacy settings of the test account",
                        new PrivacyOptions());

                    yield return new Example(
                        "Report one privacy setting",
                        new PrivacyOptions { Setting = "comms" });

                    yield return new Example(
                        "Block communicating outside of Xbox with voice and text",
                        new PrivacyOptions { Setting = "cross-network", Value = "Blocked" });

                    yield return new Example(
                        "Block communicating on Xbox with voice and text (privilege 252)",
                        new PrivacyOptions { Setting = "comms", Value = "Blocked" });
                }
            }
        }

        [Verb("list-privacy-settings", HelpText = "List the known Xbox Live privacy settings, their aliases and their values.")]
        private class ListPrivacySettingsOptions
        {
            [Usage]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    yield return new Example("List the known Xbox Live privacy settings.", new ListPrivacySettingsOptions());
                }
            }
        }
    }
}
