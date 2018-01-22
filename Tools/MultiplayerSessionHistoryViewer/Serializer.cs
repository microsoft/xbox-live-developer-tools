//-----------------------------------------------------------------------
// <copyright file="Serializer.cs" company="Microsoft">
//     Copyright (c) Microsoft. All rights reserved.
//     Internal use only.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SessionHistoryViewer
{
    public static class Serializer
    {
        /// <summary>
        /// Read a stream into a byte array.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>A byte array with the entire contents of the stream.</returns>
        public static byte[] ToByteArray(this Stream stream)
        {
            byte[] buffer = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        public static T DeserializeJson<T>(this byte[] data)
        {
            if (data == null || data.Length == 0)
                return default(T);

            try
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
                    return (T)ser.ReadObject(ms);
                }
            }
            catch (SerializationException)
            {
            }
            return default(T);
        }
        public static T DeserializeJson<T>(this string json)
        {
            return String.IsNullOrEmpty(json) ? default(T) : json.ToUTF8ByteArray().DeserializeJson<T>();
        }

        public static string SerializeToJsonString(this object instance)
        {
            using (Stream ms = SerializeJson(instance))
            {
                return ms.CreateString();
            }
        }


        public static byte[] SerializeToJsonByteArray(this object instance)
        {
            return SerializeToJsonString(instance).ToUTF8ByteArray();
        }

        /// <summary>
        /// Caller must dispose this Stream
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static Stream SerializeJson(object instance)
        {
            if (instance == null)
                return null;

            MemoryStream ms = new MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(instance.GetType());
            ser.WriteObject(ms, instance);
            ms.Position = 0;
            return ms;
        }

        public static string CreateString(this Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                return sr.ReadToEnd();
            }
        }

        /// <summary>
        /// Generate a ASCII encoded byte array for the string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] ToUTF8ByteArray(this string value)
        {
            return String.IsNullOrEmpty(value) ? new byte[0] : Encoding.UTF8.GetBytes(value);
        }
        /// <summary>
        /// Get a string from a ASCII byte array
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string FromUTF8ByteArray(this byte[] value)
        {
            return value == null || value.Length == 0 ? String.Empty : Encoding.UTF8.GetString(value);
        }
        /// <summary>
        /// Generate a Unicode encoded byte array for the string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] ToUnicodeByteArray(this string value)
        {
            return String.IsNullOrEmpty(value) ? new byte[0] : Encoding.Unicode.GetBytes(value);
        }
        /// <summary>
        /// Get a string from a Unicode byte array
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string FromUnicodeByteArray(this byte[] value)
        {
            return value == null || value.Length == 0 ? String.Empty : Encoding.Unicode.GetString(value);
        }
        public static string ToISO8601(this DateTime value)
        {
            return value.ToString("s", CultureInfo.InvariantCulture);
        }
        /// <summary>
        /// Suppress IDispose because caller is responsible for disposing of this Stream
        /// </summary>
        /// <param name="byteArray"></param>
        /// <returns></returns>
        public static Stream CreateMemoryStream(this byte[] byteArray)
        {
            if (byteArray == null)
                throw new ArgumentNullException("byteArray");
            MemoryStream ms = new MemoryStream();
            ms.Write(byteArray, 0, byteArray.Length);
            ms.Position = 0;
            return ms;
        }
        /// <summary>
        /// Convert a byte array to a Base64 string.
        /// </summary>
        /// <param name="value">Byte array instance</param>
        /// <returns>Base64 string representing the byte array.   Null or Empty byte array returns String.Empty</returns>
        public static string ToBase64String(this byte[] value)
        {
            return value == null || value.Length == 0 ? String.Empty : Convert.ToBase64String(value);
        }

        /// <summary>
        /// Convert a base 64 string to a byte array
        /// </summary>
        /// <param name="value">Base64 string instance</param>
        /// <returns>Byte array:  null, empty or invalid string returns an empty byte array</returns>
        public static byte[] FromBase64String(this string value)
        {
            byte[] result;
            try
            {
                result = String.IsNullOrEmpty(value) ? new byte[0] : Convert.FromBase64String(value);
            }
            catch (FormatException)
            {
                result = new byte[0];
            }

            return result;
        }
        /// <summary>
        /// Wrap Enum.TryParse into a function that returns a INullable value
        /// </summary>
        /// <typeparam name="T">Enum Type to parse to</typeparam>
        /// <param name="value">String Value to parse</param>
        /// <returns>INullable Enum value</returns>
        public static T? ParseEnumValue<T>(this string value)
            where T : struct
        {
            T? result = null;
            T enumValue = default(T);
            if (Enum.TryParse<T>(value, true, out enumValue))
            {
                result = enumValue;
            }

            return result;
        }
    }
}