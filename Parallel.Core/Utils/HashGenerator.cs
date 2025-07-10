// Copyright 2025 Kyle Ebbinga

using System.Security.Cryptography;
using System.Text;

namespace Parallel.Core.Utils
{
    /// <summary>
    /// Provides functionality for generating random hashes. This class cannot be inherited.
    /// </summary>
    public static class HashGenerator
    {
        /// <summary>
        /// Generates a random hash.
        /// </summary>
        /// <param name="length"></param>
        /// <param name="lowercase"></param>
        /// <returns></returns>
        public static string GenerateHash(int length, bool lowercase = false)
        {
            return RandomNumberGenerator.GetHexString(length, lowercase);
        }

        /// <summary>
        /// Computes a SHA1 hash from a string.
        /// </summary>
        /// <param name="value">The string to hash.</param>
        /// <returns>A <see cref="SHA1"/> hash as a string.</returns>
        public static string CreateSHA1(string value)
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            return Convert.ToHexString(SHA1.HashData(Encoding.ASCII.GetBytes(value))).ToLower();
        }

        /// <summary>
        /// Computes a SHA256 hash from a string.
        /// </summary>
        /// <param name="value">The string to hash.</param>
        /// <returns>A <see cref="SHA256"/> hash as a string.</returns>
        public static string CreateSHA256(string value)
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLower();
        }
    }
}