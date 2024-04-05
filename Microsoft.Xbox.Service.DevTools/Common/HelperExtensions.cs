// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.Common
{
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Security;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    internal static class HelperExtensions
    {
        public static async Task<T> DeserializeJsonAsync<T>(this HttpContent content) 
        {
            using (Stream stream = await content.ReadAsStreamAsync())
            {
                using (StreamReader streamReader = new StreamReader(stream))
                {
                    using (JsonReader jsonReader = new JsonTextReader(streamReader))
                    {
                        JsonSerializer serializer = JsonSerializer.Create();
                        return serializer.Deserialize<T>(jsonReader);
                    }
                }
            }
        }

        public static string ToJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static byte[] ToArray(this Stream stream)
        {
            if (stream is MemoryStream)
            {
                return ((MemoryStream)stream).ToArray();
            }

            using (MemoryStream ms = new MemoryStream())
            {
                stream.Position = 0;
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public static Stream Copy(this Stream stream)
        {
            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;
            return ms;
        }

        public static SecureString ToSecureString(this string str)
        {
            return new NetworkCredential(string.Empty, str).SecurePassword;
        }

        public static string FromSecureString(this SecureString secureString)
        {
            return new NetworkCredential(string.Empty, secureString).Password;
        }
    }
}
