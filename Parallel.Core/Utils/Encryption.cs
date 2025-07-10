// Copyright 2025 Kyle Ebbinga

using System.Text;

namespace Parallel.Core.Utils
{
    /// <summary>
    /// Provides functionality for encryption. This class cannot be inherited.
    /// </summary>
    public static class Encryption
    {
        /// <summary>
        /// Converts a string of UTF-8 characters to a base64 string.
        /// </summary>
        /// <param name="value">The string to be encoded.</param>
        /// <returns>A base64 encoded string.</returns>
        public static string Encode(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            byte[] data = Encoding.UTF8.GetBytes(value);
            string encoded = Convert.ToBase64String(data);
            return encoded;
        }

        /// <summary>
        /// Converts a base64 string to a string of UTF-8 characters.
        /// </summary>
        /// <param name="value">The base64 string to decode.</param>
        public static string Decode(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            byte[] data = Convert.FromBase64String(value);
            string decoded = Encoding.UTF8.GetString(data);
            return decoded;
        }
    }
}