﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZenCart.Fishbowl.Extensions
{
    public static class StringExtensions
    {
        public static String Coalesce(params string[] values)
        {
            return values.FirstOrDefault(v => !String.IsNullOrWhiteSpace(v));
        }

        /// <summary>
        /// Truncates string so that it is no longer than the specified number of characters.
        /// </summary>
        /// <param name="str">String to truncate.</param>
        /// <param name="length">Maximum string length.</param>
        /// <returns>Original string or a truncated one if the original was too long.</returns>
        public static string Truncate(this string str, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", "Length must be >= 0");
            }

            if (str == null)
            {
                return null;
            }

            int maxLength = Math.Min(str.Length, length);
            return str.Substring(0, maxLength);
        }
    }
}