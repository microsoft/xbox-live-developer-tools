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
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Reads and writes privilege restrictions on the signed in test account through the
    /// Xbox Live parental service. This is the same call that XblTestAccountGui makes.
    /// </summary>
    internal static class PrivilegeClient
    {
        private const string ContractVersion = "1";

        // The service only honours Actor=Self, so the token owner is always the account being
        // changed. Other actor values are rejected with HTTP 400.
        private const string EndpointFormat = "https://parental.xboxlive.com/users/{0}/privileges?Actor=Self";

        /// <summary>
        /// Reads the privileges currently restricted on the account.
        /// </summary>
        /// <param name="sandbox">The sandbox to authenticate against.</param>
        /// <param name="xuid">The XUID of the signed in test account.</param>
        /// <returns>The restricted privilege ids.</returns>
        internal static async Task<IList<int>> GetRestrictionsAsync(string sandbox, string xuid)
        {
            string response = await UserServiceRequest.SendAsync(sandbox, HttpMethod.Get, BuildUri(xuid), ContractVersion, null);
            return ParseRestrictions(response);
        }

        /// <summary>
        /// Restricts or unrestricts privileges on the account.
        /// </summary>
        /// <param name="sandbox">The sandbox to authenticate against.</param>
        /// <param name="xuid">The XUID of the signed in test account.</param>
        /// <param name="privileges">The privilege ids to change.</param>
        /// <param name="restrict">True to restrict the privileges, false to clear the restriction.</param>
        /// <returns>The restricted privilege ids after the change.</returns>
        internal static async Task<IList<int>> SetRestrictionsAsync(string sandbox, string xuid, IEnumerable<int> privileges, bool restrict)
        {
            string operation = restrict ? "SetPrivilegeRestriction" : "ClearPrivilegeRestriction";
            var payload = new JObject
            {
                [operation] = new JArray(privileges.ToArray()),
            };

            string response = await UserServiceRequest.SendAsync(
                sandbox, HttpMethod.Post, BuildUri(xuid), ContractVersion, payload.ToString(Newtonsoft.Json.Formatting.None));

            return ParseRestrictions(response);
        }

        private static string BuildUri(string xuid)
        {
            return string.Format(CultureInfo.InvariantCulture, EndpointFormat, Uri.EscapeDataString(xuid));
        }

        private static IList<int> ParseRestrictions(string response)
        {
            var restrictions = new List<int>();

            if (string.IsNullOrWhiteSpace(response))
            {
                return restrictions;
            }

            // The service reports the current restriction list under the same property name that
            // sets it, whichever operation was sent.
            if (JToken.Parse(response) is JObject parsed && parsed["SetPrivilegeRestriction"] is JArray values)
            {
                restrictions.AddRange(values.Select(value => (int)value));
            }

            restrictions.Sort();
            return restrictions;
        }
    }
}
