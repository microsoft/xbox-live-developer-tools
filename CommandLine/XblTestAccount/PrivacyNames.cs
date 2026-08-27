// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XblTestAccount
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The privacy settings exposed by the Xbox Live privacy service, with the short aliases and
    /// descriptions used on the command line.
    /// </summary>
    internal static class PrivacyNames
    {
        private static readonly Dictionary<string, string> DescriptionMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "CommunicateDuringCrossNetworkPlay", "Decide whether to communicate using voice & text with people on gaming services outside of Xbox, such as PC and PlayStation." },
            { "AllowUserCreatedContentViewing", "Decide whose community creations you want to see. Blocking this may affect whether you can upload your own creations to games." },
            { "CommunicateUsingTextAndVoice", "Decide who on Xbox to communicate with using voice and text, and who sends you invitations to parties, games, or clubs." },
            { "SharePresence", "Decide who can see that you're online and which game or app you're using." },
            { "ShareActivityFeed", "Decide who can see what you post to your activity feed." },
        };

        private static readonly Dictionary<string, string> AliasMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "cross-network", "CommunicateDuringCrossNetworkPlay" },
            { "crossnetwork", "CommunicateDuringCrossNetworkPlay" },
            { "comms", "CommunicateUsingTextAndVoice" },
            { "ugc", "AllowUserCreatedContentViewing" },
            { "presence", "SharePresence" },
            { "activity-feed", "ShareActivityFeed" },
            { "activityfeed", "ShareActivityFeed" },
        };

        /// <summary>
        /// Gets the known privacy setting names and their descriptions.
        /// </summary>
        internal static IReadOnlyDictionary<string, string> All => DescriptionMapping;

        /// <summary>
        /// Gets the short aliases accepted in place of a full setting name.
        /// </summary>
        internal static IReadOnlyDictionary<string, string> Aliases => AliasMapping;

        /// <summary>
        /// Resolves a setting name or short alias to the name the service expects.
        /// </summary>
        /// <param name="nameOrAlias">The setting name or alias supplied on the command line.</param>
        /// <returns>
        /// The resolved setting name. A name that is not one of the documented settings is returned
        /// unchanged, because the service exposes more settings than are described here and the
        /// caller validates against the set the service actually reports.
        /// </returns>
        internal static string Resolve(string nameOrAlias)
        {
            if (string.IsNullOrWhiteSpace(nameOrAlias))
            {
                return null;
            }

            string trimmed = nameOrAlias.Trim();

            if (AliasMapping.TryGetValue(trimmed, out string aliased))
            {
                return aliased;
            }

            // Match a documented name case insensitively but return it with the casing the service
            // uses, otherwise pass the name through for the caller to check against the live set.
            return DescriptionMapping.Keys.FirstOrDefault(
                known => string.Equals(known, trimmed, StringComparison.OrdinalIgnoreCase))
                ?? trimmed;
        }
    }
}
