//-----------------------------------------------------------------------
// <copyright file="SessionHistorySnapshotCache.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
//     Internal use only.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text;

namespace SessionHistoryViewer
{
    internal class SessionHistorySnapshotCache
    {
        ConcurrentDictionary<string, string> cache = new ConcurrentDictionary<string, string>();

        internal HashAlgorithm algorithm;
        public SessionHistorySnapshotCache()
        {
            algorithm = SHA256.Create();
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

            byte[] bytes =algorithm.ComputeHash(Encoding.UTF8.GetBytes(key));

            StringBuilder sb = new StringBuilder();
            foreach(byte b in bytes)
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }

        public bool TryGetSnapshot(string key, out string snapshot)
        {
            snapshot = null;

            if (cache.ContainsKey(key))
            {
                snapshot = cache[key];
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

            if (snapshot == null)
            {
                throw new ArgumentNullException("snapshot");
            }

            cache[key] =snapshot;
        }
    }
}
