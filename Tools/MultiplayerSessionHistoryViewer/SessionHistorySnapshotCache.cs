// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SessionHistoryViewer
{
    using System;
    using System.Collections.Concurrent;
    using System.Security.Cryptography;
    using System.Text;

    internal class SessionHistorySnapshotCache
    {
        private ConcurrentDictionary<string, string> cache = new ConcurrentDictionary<string, string>();

        private HashAlgorithm algorithm;

        public SessionHistorySnapshotCache()
        {
            this.algorithm = SHA256.Create();
        }

        public string GetHashString(string sessionName, string branch, long changeNumber)
        {
            if (sessionName == null)
            {
                throw new ArgumentNullException("sessionName");
            }

            if (branch == null)
            {
                throw new ArgumentNullException("branch");
            }

            string key = string.Format("{0}{1}{2}", sessionName, branch, changeNumber);

            byte[] bytes = this.algorithm.ComputeHash(Encoding.UTF8.GetBytes(key));

            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }

        public bool TryGetSnapshot(string key, out string snapshot)
        {
            snapshot = null;

            if (this.cache.ContainsKey(key))
            {
                snapshot = this.cache[key];
                return true;
            }

            return false;
        }

        public void AddSnapshotToCache(string key, string snapshot)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            this.cache[key] = snapshot ?? throw new ArgumentNullException("snapshot");
        }
    }
}
