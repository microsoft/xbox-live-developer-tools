// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblTestAccount
{
    using System.Collections.Generic;

    /// <summary>
    /// Friendly names for the Xbox Live privilege ids handled by the parental service.
    /// </summary>
    internal static class PrivilegeNames
    {
        private static readonly Dictionary<int, string> NameMapping = new Dictionary<int, string>
        {
            { 185, "Cross Network Play" },
            { 189, "Non-interactive Sessions" },
            { 197, "View Friends List" },
            { 198, "Game DVR" },
            { 199, "Share Kinect Content" },
            { 203, "Join Parties" },
            { 205, "Comms In-game Voice" },
            { 206, "Comms Voice (Skype)" },
            { 207, "Cloud Gaming Manage Session" },
            { 208, "Cloud Gaming Join Session" },
            { 209, "Cloud Saved Games" },
            { 214, "Premium Content" },
            { 217, "Internet Browser" },
            { 219, "Subscription Content" },
            { 220, "Social Network Sharing" },
            { 224, "Premium Video" },
            { 235, "Video Communications" },
            { 245, "Purchase Content" },
            { 247, "User Generated Content" },
            { 249, "Profile Viewing" },
            { 252, "Comms (text and voice)" },
            { 254, "Multiplayer" },
            { 255, "Add Friend" },
        };

        // The parental service only accepts Actor=Self, and under that actor it only allows these
        // two privileges to be restricted. Every other id is rejected: the service answers HTTP 400
        // for a privilege it will not let the account edit, and HTTP 409 for one it derives from
        // another privilege, such as 189, which it restricts by itself when 254 is restricted.
        private static readonly HashSet<int> EditableIds = new HashSet<int> { 185, 254 };

        // Some privileges are derived by the service from a privacy setting rather than being set
        // directly, so restricting the privacy setting is what restricts the privilege. Both of
        // these were confirmed against the service: blocking the privacy setting moved the privilege
        // out of the account's granted privileges and into its restricted ones.
        private static readonly Dictionary<int, string> PrivacyEquivalent = new Dictionary<int, string>
        {
            { 247, "ugc" },
            { 252, "comms" },
        };

        /// <summary>
        /// Gets the known privilege ids and their friendly names.
        /// </summary>
        internal static IReadOnlyDictionary<int, string> All => NameMapping;

        /// <summary>
        /// Gets the privilege ids that the account is allowed to restrict for itself.
        /// </summary>
        internal static IEnumerable<int> Editable => EditableIds;

        /// <summary>
        /// Gets the privileges that follow a privacy setting, keyed by privilege id.
        /// </summary>
        internal static IReadOnlyDictionary<int, string> PrivacyControlled => PrivacyEquivalent;

        /// <summary>
        /// Gets the privacy setting alias that controls a privilege, where one is known.
        /// </summary>
        /// <param name="id">The privilege id.</param>
        /// <param name="alias">Receives the privacy setting alias that controls the privilege.</param>
        /// <returns>True when the privilege is controlled by a privacy setting.</returns>
        internal static bool TryGetPrivacyEquivalent(int id, out string alias)
        {
            return PrivacyEquivalent.TryGetValue(id, out alias);
        }

        /// <summary>
        /// Gets a value indicating whether a privilege can be restricted by the account itself.
        /// </summary>
        /// <param name="id">The privilege id.</param>
        /// <returns>True when the privilege can be blocked and allowed by this tool.</returns>
        internal static bool IsEditable(int id)
        {
            return EditableIds.Contains(id);
        }

        /// <summary>
        /// Gets the friendly name of a privilege.
        /// </summary>
        /// <param name="id">The privilege id.</param>
        /// <param name="name">Receives the friendly name when the privilege is known.</param>
        /// <returns>True when the privilege is known.</returns>
        internal static bool TryGetName(int id, out string name)
        {
            return NameMapping.TryGetValue(id, out name);
        }

        /// <summary>
        /// Renders a privilege as its id followed by its friendly name, where one is known.
        /// </summary>
        /// <param name="id">The privilege id.</param>
        /// <returns>A display string such as "185 (Cross Network Play)".</returns>
        internal static string Describe(int id)
        {
            return TryGetName(id, out string name) ? $"{id} ({name})" : id.ToString();
        }
    }
}
