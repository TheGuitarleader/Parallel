// Copyright 2025 Kyle Ebbinga

using System.Net;

namespace Parallel.Core.Utils
{
    /// <summary>
    /// Converts a data type to another data type. This class cannot be inherited.
    /// </summary>
    public static class Converter
    {
        /// <summary>
        /// Converts a int32 into a boolean.
        /// </summary>
        /// <param name="value">The int32 value.</param>
        /// <returns>If 1 true, false otherwise.</returns>
        public static bool ToBool(double value)
        {
            return value.Equals(1);
        }

        /// <summary>
        /// Converts a boolean into an int32.
        /// </summary>
        /// <param name="value">The int32 value.</param>
        /// <returns>If 1 true, false otherwise.</returns>
        public static int ToInt32(bool value)
        {
            return value ? 1 : 0;
        }

        /// <summary>
        /// Converts two double values into a percent.
        /// </summary>
        /// <param name="part"></param>
        /// <param name="whole"></param>
        public static double ToPercent(double part, double whole)
        {
            return part * 100 / whole;
        }
    }
}