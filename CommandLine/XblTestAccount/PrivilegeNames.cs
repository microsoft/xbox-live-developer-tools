// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblTestAccount
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Friendly names for the Xbox Live privilege ids that appear in an account's token claims and
    /// in the parental service.
    /// </summary>
    /// <remarks>
    /// The names follow the GDK XUserPrivilege enumeration and the Win32 KnownGamingPrivileges
    /// enumeration in gamingtcui.h, which between them cover the ids a modern account holds. The
    /// remainder are ids that appear in the token claim example in the GDK documentation for the
    /// server side handling of user privileges. An id outside this set is reported as its number,
    /// as the service mints new ones from time to time.
    /// </remarks>
    internal static class PrivilegeNames
    {
        private static readonly Dictionary<int, string> NameMapping = new Dictionary<int, string>
        {
            { 185, "Cross Network Play" },
            { 188, "Clubs" },
            { 189, "Non-interactive Sessions" },
            { 190, "Broadcast" },
            { 196, "Manage Profile Privacy" },
            { 197, "View Friends List" },
            { 198, "Game DVR" },
            { 199, "Share Kinect Content" },
            { 203, "Multiplayer Parties" },
            { 205, "Comms In-game Voice" },
            { 206, "Comms Voice (Skype)" },
            { 207, "Cloud Gaming Manage Session" },
            { 208, "Cloud Gaming Join Session" },
            { 209, "Cloud Saved Games" },
            { 211, "Share Content Outside Xbox" },
            { 212, "Unfiltered Programming" },
            { 214, "Premium Content" },
            { 217, "Internet Browser" },
            { 219, "Subscription Content" },
            { 220, "Social Network Sharing" },
            { 224, "Premium Video" },
            { 226, "Dedicated Server Multiplayer" },
            { 227, "Manage Payment Instruments" },
            { 228, "Switch Microsoft Account" },
            { 229, "Share Friends List (People On My List)" },
            { 230, "Share Friends List" },
            { 231, "Store App Access" },
            { 234, "Video Communications (People On My List)" },
            { 235, "Video Communications" },
            { 237, "Explicit Music Content" },
            { 240, "Cross Platform System Communication" },
            { 243, "Share Presence (People On My List)" },
            { 244, "Share Presence" },
            { 245, "Purchase Content" },
            { 246, "User Generated Content (People On My List)" },
            { 247, "User Generated Content" },
            { 248, "Profile Viewing (People On My List)" },
            { 249, "Profile Viewing" },
            { 251, "Comms (People On My List)" },
            { 252, "Comms (text and voice)" },
            { 254, "Multiplayer Sessions" },
            { 255, "Add Friend" },
        };

        // The parental service only accepts Actor=Self, and under that actor it only allows these
        // two privileges to be restricted. Every other id is rejected: the service answers HTTP 400
        // for a privilege it will not let the account edit, and HTTP 409 for one it derives from
        // another privilege, such as 189, which it restricts by itself when 254 is restricted.
        private static readonly HashSet<int> EditableIds = new HashSet<int> { 185, 254 };

        // Some privileges are derived by the service from a privacy setting rather than being set
        // directly, so changing the privacy setting is what changes the privilege. Each entry below
        // was confirmed against the service by moving the setting and re-reading the token: the
        // privilege changed state with it. A setting value other than Everyone is what restricts,
        // so PeopleOnMyList restricts just as Blocked does.
        private static readonly Dictionary<int, string> PrivacyEquivalent = new Dictionary<int, string>
        {
            { 234, "CommunicateUsingVideo" },
            { 247, "AllowUserCreatedContentViewing" },
            { 251, "CommunicateUsingTextAndVoice" },
            { 252, "CommunicateUsingTextAndVoice" },
        };

        // A setting can control more than one privilege, so the privilege to name when talking about
        // the setting is called out here rather than being picked out of the mapping above, where it
        // would depend on the order the entries happen to be stored in.
        private static readonly Dictionary<string, int> PrivacyEquivalentPrimary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "CommunicateUsingVideo", 234 },
            { "AllowUserCreatedContentViewing", 247 },
            { "CommunicateUsingTextAndVoice", 252 },
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
        /// Gets the privacy setting that controls a privilege, where one is known.
        /// </summary>
        /// <param name="id">The privilege id.</param>
        /// <param name="setting">Receives the name of the privacy setting that controls it.</param>
        /// <returns>True when the privilege is controlled by a privacy setting.</returns>
        internal static bool TryGetPrivacySetting(int id, out string setting)
        {
            return PrivacyEquivalent.TryGetValue(id, out setting);
        }

        /// <summary>
        /// Gets the privilege that a privacy setting is described by, where one is known. A setting
        /// can control several privileges, so this is the one that represents it.
        /// </summary>
        /// <param name="setting">The privacy setting name as reported by the service.</param>
        /// <param name="id">Receives the privilege id the setting controls.</param>
        /// <returns>True when the setting controls a known privilege.</returns>
        internal static bool TryGetPrivilegeForSetting(string setting, out int id)
        {
            if (setting != null && PrivacyEquivalentPrimary.TryGetValue(setting, out id))
            {
                return true;
            }

            id = 0;
            return false;
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
        /// Gets the friendly name of a privilege, or a stand in when the id is not one this tool
        /// knows. The service mints new ids from time to time, so an unknown id is reported rather
        /// than hidden.
        /// </summary>
        /// <param name="id">The privilege id.</param>
        /// <param name="fallback">The name to use when the privilege is not known.</param>
        /// <returns>The friendly name, or the fallback.</returns>
        internal static string GetName(int id, string fallback)
        {
            return TryGetName(id, out string name) ? name : fallback;
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
