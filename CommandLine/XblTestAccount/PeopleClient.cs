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
    /// The changes the people command can make to the signed in account's lists.
    /// </summary>
    internal enum PeopleAction
    {
        /// <summary>
        /// Follow people.
        /// </summary>
        Follow,

        /// <summary>
        /// Stop following people.
        /// </summary>
        Unfollow,

        /// <summary>
        /// Make people mutual friends of the account.
        /// </summary>
        Friend,

        /// <summary>
        /// Mark followed people as favorites.
        /// </summary>
        Favorite,

        /// <summary>
        /// Clear the favorite mark from followed people.
        /// </summary>
        Unfavorite,

        /// <summary>
        /// Add people to the block list.
        /// </summary>
        Block,

        /// <summary>
        /// Remove people from the block list.
        /// </summary>
        Unblock,

        /// <summary>
        /// Add people to the mute list.
        /// </summary>
        Mute,

        /// <summary>
        /// Remove people from the mute list.
        /// </summary>
        Unmute,
    }

    /// <summary>
    /// Reads and changes who the signed in test account follows, blocks and mutes. These are the
    /// same calls that the Follow/Block/Mute and People tabs of XblTestAccountGui make.
    /// </summary>
    internal static class PeopleClient
    {
        /// <summary>
        /// The most people the social service lets an account follow in a 12 hour period.
        /// </summary>
        internal const int FollowLimit = 500;

        private const string DefaultContractVersion = "2";
        private const string FollowingEndpoint = "https://social.xboxlive.com/users/me/people";
        private const string BlockedEndpointFormat = "https://privacy.xboxlive.com/users/xuid({0})/people/avoid";
        private const string MutedEndpointFormat = "https://privacy.xboxlive.com/users/xuid({0})/people/mute";

        private static readonly Dictionary<PeopleAction, ActionEndpoint> Endpoints = new Dictionary<PeopleAction, ActionEndpoint>
        {
            [PeopleAction.Friend] = Social("https://social.xboxlive.com/bulk/users/me/people/makefriends/v2", "3", 2000),
            [PeopleAction.Follow] = Social("https://social.xboxlive.com/users/me/people/xuids?method=add", DefaultContractVersion, 35000),
            [PeopleAction.Unfollow] = Social("https://social.xboxlive.com/users/me/people/xuids?method=remove", DefaultContractVersion, 0),
            [PeopleAction.Favorite] = Social("https://social.xboxlive.com/users/me/people/favorites/xuids?method=add", DefaultContractVersion, 0),
            [PeopleAction.Unfavorite] = Social("https://social.xboxlive.com/users/me/people/favorites/xuids?method=remove", DefaultContractVersion, 0),
            [PeopleAction.Block] = Privacy("https://privacy.xboxlive.com/users/me/people/never", "Add"),
            [PeopleAction.Unblock] = Privacy("https://privacy.xboxlive.com/users/me/people/never", "Remove"),
            [PeopleAction.Mute] = Privacy("https://privacy.xboxlive.com/users/me/people/mute", "Add"),
            [PeopleAction.Unmute] = Privacy("https://privacy.xboxlive.com/users/me/people/mute", "Remove"),
        };

        /// <summary>
        /// Gets the most people the service accepts in one request for an action.
        /// </summary>
        internal static int GetBatchSize(PeopleAction action)
        {
            return Endpoints[action].BatchSize;
        }

        /// <summary>
        /// Gets how long to wait between requests for an action, to stay inside the service's rate limit.
        /// </summary>
        internal static int GetBatchDelayMilliseconds(PeopleAction action)
        {
            return Endpoints[action].DelayMilliseconds;
        }

        /// <summary>
        /// Applies an action to one batch of people.
        /// </summary>
        /// <param name="sandbox">The sandbox to authenticate against.</param>
        /// <param name="action">The change to make.</param>
        /// <param name="xuids">The people to change, no more than <see cref="GetBatchSize"/> of them.</param>
        /// <returns>The XUIDs the service reported it could not update.</returns>
        internal static async Task<IList<string>> ApplyAsync(string sandbox, PeopleAction action, IList<string> xuids)
        {
            ActionEndpoint endpoint = Endpoints[action];

            JObject payload = endpoint.PrivacyOperation == null
                ? new JObject { ["xuids"] = new JArray(xuids.ToArray()) }
                : new JObject
                {
                    ["operation"] = endpoint.PrivacyOperation,
                    ["users"] = new JArray(xuids.Select(xuid => new JObject { ["xuid"] = xuid })),
                };

            string response = await UserServiceRequest.SendAsync(
                sandbox, HttpMethod.Post, endpoint.Uri, endpoint.ContractVersion, payload.ToString(Formatting.None));

            return ParseFailures(response);
        }

        /// <summary>
        /// Reads the people the account follows.
        /// </summary>
        /// <param name="sandbox">The sandbox to authenticate against.</param>
        /// <returns>The followed people, in the order the service reports them.</returns>
        internal static async Task<IList<FollowedPerson>> GetFollowingAsync(string sandbox)
        {
            string response = await UserServiceRequest.SendAsync(sandbox, HttpMethod.Get, FollowingEndpoint, DefaultContractVersion, null);

            var people = new List<FollowedPerson>();
            if (!string.IsNullOrWhiteSpace(response) && JToken.Parse(response) is JObject parsed && parsed["people"] is JArray entries)
            {
                foreach (JToken entry in entries)
                {
                    string xuid = (string)entry["xuid"];
                    if (!string.IsNullOrEmpty(xuid))
                    {
                        people.Add(new FollowedPerson
                        {
                            Xuid = xuid,
                            IsFavorite = (bool?)entry["isFavorite"] ?? false,
                            IsFollowingCaller = (bool?)entry["isFollowingCaller"] ?? false,
                        });
                    }
                }
            }

            return people;
        }

        /// <summary>
        /// Reads the people the account has blocked.
        /// </summary>
        internal static Task<IList<string>> GetBlockedAsync(string sandbox, string xuid)
        {
            return GetPrivacyListAsync(sandbox, BlockedEndpointFormat, xuid);
        }

        /// <summary>
        /// Reads the people the account has muted.
        /// </summary>
        internal static Task<IList<string>> GetMutedAsync(string sandbox, string xuid)
        {
            return GetPrivacyListAsync(sandbox, MutedEndpointFormat, xuid);
        }

        private static async Task<IList<string>> GetPrivacyListAsync(string sandbox, string endpointFormat, string xuid)
        {
            string uri = string.Format(CultureInfo.InvariantCulture, endpointFormat, Uri.EscapeDataString(xuid));
            string response = await UserServiceRequest.SendAsync(sandbox, HttpMethod.Get, uri, DefaultContractVersion, null);

            var people = new List<string>();
            if (!string.IsNullOrWhiteSpace(response) && JToken.Parse(response) is JObject parsed && parsed["users"] is JArray entries)
            {
                people.AddRange(entries.Select(entry => (string)entry["xuid"]).Where(id => !string.IsNullOrEmpty(id)));
            }

            return people;
        }

        /// <summary>
        /// Reads the people a change did not apply to. The privacy calls answer with no body, and
        /// the social calls list any failures under "failedToUpdate", either as bare XUIDs or as
        /// objects carrying one.
        /// </summary>
        private static IList<string> ParseFailures(string response)
        {
            var failed = new List<string>();

            if (string.IsNullOrWhiteSpace(response))
            {
                return failed;
            }

            JToken parsed;
            try
            {
                parsed = JToken.Parse(response);
            }
            catch (JsonReaderException)
            {
                return failed;
            }

            if (parsed is JObject body && body["failedToUpdate"] is JArray entries)
            {
                foreach (JToken entry in entries)
                {
                    string xuid = entry is JObject person ? (string)person["xuid"] : entry.ToString();
                    if (!string.IsNullOrEmpty(xuid))
                    {
                        failed.Add(xuid);
                    }
                }
            }

            return failed;
        }

        private static ActionEndpoint Social(string uri, string contractVersion, int delayMilliseconds)
        {
            return new ActionEndpoint
            {
                Uri = uri,
                ContractVersion = contractVersion,
                BatchSize = 50,
                DelayMilliseconds = delayMilliseconds,
            };
        }

        private static ActionEndpoint Privacy(string uri, string operation)
        {
            return new ActionEndpoint
            {
                Uri = uri,
                ContractVersion = DefaultContractVersion,
                BatchSize = 100,
                PrivacyOperation = operation,
            };
        }

        /// <summary>
        /// Where and how one action is sent.
        /// </summary>
        private class ActionEndpoint
        {
            public string Uri { get; set; }

            public string ContractVersion { get; set; }

            public int BatchSize { get; set; }

            public int DelayMilliseconds { get; set; }

            /// <summary>
            /// Gets or sets the operation of a privacy list call, or null for a social call.
            /// </summary>
            public string PrivacyOperation { get; set; }
        }
    }
}
