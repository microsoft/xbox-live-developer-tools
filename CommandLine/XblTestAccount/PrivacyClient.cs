// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblTestAccount
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The values the Xbox Live privacy service accepts for a privacy setting.
    /// </summary>
    internal enum PrivacyValue
    {
        /// <summary>
        /// Anyone is allowed.
        /// </summary>
        Everyone,

        /// <summary>
        /// Only people on the account's friends list are allowed.
        /// </summary>
        PeopleOnMyList,

        /// <summary>
        /// Nobody is allowed.
        /// </summary>
        Blocked,
    }

    /// <summary>
    /// Reads and writes the privacy settings of the signed in test account. This is the same call
    /// that the Privacy tab of XblTestAccountGui makes.
    /// </summary>
    internal static class PrivacyClient
    {
        private const string ContractVersion = "4";
        private const string ReadEndpointFormat = "https://privacy.xboxlive.com/users/xuid({0})/privacy/settings";

        // The write goes through /users/me, so the account being changed is always the token owner.
        private const string WriteEndpoint = "https://privacy.xboxlive.com/users/me/privacy/settings";

        // The write is not immediately visible to the read, so the read back is repeated briefly.
        private const int ReadBackAttempts = 4;
        private const int ReadBackDelayMilliseconds = 750;

        /// <summary>
        /// Reads every privacy setting on the account.
        /// </summary>
        /// <param name="sandbox">The sandbox to authenticate against.</param>
        /// <param name="xuid">The XUID of the signed in test account.</param>
        /// <returns>The setting names and their values, keyed by setting name.</returns>
        internal static async Task<IDictionary<string, string>> GetSettingsAsync(string sandbox, string xuid)
        {
            string uri = string.Format(CultureInfo.InvariantCulture, ReadEndpointFormat, Uri.EscapeDataString(xuid));
            string response = await UserServiceRequest.SendAsync(sandbox, HttpMethod.Get, uri, ContractVersion, null);
            return ParseSettings(response);
        }

        /// <summary>
        /// Sets one privacy setting on the account.
        /// </summary>
        /// <param name="sandbox">The sandbox to authenticate against.</param>
        /// <param name="xuid">The XUID of the signed in test account.</param>
        /// <param name="setting">The setting name the service expects.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>The setting names and their values after the change.</returns>
        internal static async Task<IDictionary<string, string>> SetSettingAsync(string sandbox, string xuid, string setting, PrivacyValue value)
        {
            var payload = new JObject
            {
                ["settings"] = new JArray(
                    new JObject
                    {
                        ["setting"] = setting,
                        ["value"] = value.ToString(),
                    }),
            };

            await UserServiceRequest.SendAsync(
                sandbox, HttpMethod.Put, WriteEndpoint, ContractVersion, payload.ToString(Formatting.None));

            // The write does not report the resulting state, and a read issued straight afterwards
            // can still return the previous value, so the read back is repeated until the new value
            // appears or the attempts run out. The caller reports a value that never converged.
            IDictionary<string, string> settings = null;
            for (int attempt = 0; attempt < ReadBackAttempts; attempt++)
            {
                if (attempt > 0)
                {
                    await Task.Delay(ReadBackDelayMilliseconds);
                }

                settings = await GetSettingsAsync(sandbox, xuid);

                if (settings.TryGetValue(setting, out string current)
                    && string.Equals(current, value.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }

            return settings;
        }

        /// <summary>
        /// Matches a setting name against the set the service reports.
        /// </summary>
        /// <remarks>
        /// The service is the only authority on which settings exist, and it exposes more of them
        /// than this tool could usefully hard code, so a name is resolved against a live read
        /// rather than a table held here. Matching is case insensitive, and the name the service
        /// uses is returned so that the write carries the casing it expects.
        /// </remarks>
        /// <param name="settings">The settings as reported by the service.</param>
        /// <param name="name">The setting name supplied on the command line.</param>
        /// <returns>The name the service uses, or null when the service has no such setting.</returns>
        internal static string ResolveSettingName(IDictionary<string, string> settings, string name)
        {
            if (settings == null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            string trimmed = name.Trim();
            return settings.Keys.FirstOrDefault(known => string.Equals(known, trimmed, StringComparison.OrdinalIgnoreCase));
        }

        private static IDictionary<string, string> ParseSettings(string response)
        {
            var settings = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(response))
            {
                return settings;
            }

            if (JToken.Parse(response) is JObject parsed && parsed["settings"] is JArray entries)
            {
                foreach (JToken entry in entries)
                {
                    string name = (string)entry["setting"];
                    if (!string.IsNullOrEmpty(name))
                    {
                        settings[name] = (string)entry["value"] ?? string.Empty;
                    }
                }
            }

            return settings;
        }
    }
}
